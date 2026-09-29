using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Quetzal.Application.DTOs;
using Quetzal.Domain.Entidades;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Quetzal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        private JwtSecurityToken GerarToken(List<Claim> authClaims)
        {
            var chave = _configuration["Jwt:Chave"]
                ?? throw new InvalidOperationException("A chave JWT não foi configurada.");

            var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(chave));

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Emissor"],
                audience: _configuration["Jwt:Audiencia"],
                expires: DateTime.UtcNow.AddHours(8),
                claims: authClaims,
                signingCredentials: new SigningCredentials(
                    authSigningKey,
                    SecurityAlgorithms.HmacSha256));
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarUserDto dto)
        {
            var userExists = await _userManager.FindByEmailAsync(dto.Email);

            if (userExists is not null)
            {
                return BadRequest(
                    ApiResposta<object>.Falha("Já existe um usuário com este e-mail."));
            }

            // Cadastro público começa INATIVO.
            // O cliente ainda poderá fazer login e usar o site público,
            // mas não terá acesso à Minha área até a Admin ativá-lo.
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                NomeCompleto = dto.NomeCompleto,
                Telefone = dto.Telefone,
                Ativo = false,
                DataCadastro = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Senha);

            if (!result.Succeeded)
            {
                var erros = result.Errors
                    .Select(e => e.Description)
                    .ToList();

                return BadRequest(
                    ApiResposta<object>.FalhaValidacao(
                        erros,
                        "Erro ao criar usuário."));
            }

            // Todo cadastro público recebe somente a role Cliente.
            var resultadoRole = await _userManager.AddToRoleAsync(user, "Cliente");

            if (!resultadoRole.Succeeded)
            {
                var erros = string.Join(
                    "; ",
                    resultadoRole.Errors.Select(e => e.Description)
                );

                return BadRequest(new
                {
                    mensagem = $"Não foi possível atribuir o perfil Cliente: {erros}"
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResposta<object>.Ok(null!, "Usuário registrado com sucesso e aguardando ativação."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user is null)
            {
                return Unauthorized(
                    ApiResposta<LoginRespostaDto>.Falha("E-mail ou senha inválidos."));
            }

            // Usuário INATIVO também pode fazer login.
            // A restrição acontece na Minha área, não na autenticação.
            var senhaValida = await _userManager.CheckPasswordAsync(user, dto.Senha);

            if (!senhaValida)
            {
                return Unauthorized(
                    ApiResposta<LoginRespostaDto>.Falha("E-mail ou senha inválidos."));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.NomeCompleto),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("Ativo", user.Ativo.ToString())
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = GerarToken(authClaims);

            var resposta = new LoginRespostaDto
            {
                UsuarioId = user.Id,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiracao = token.ValidTo,
                NomeUsuario = user.NomeCompleto,
                Email = user.Email ?? string.Empty,
                Perfis = roles.ToList(),
                Ativo = user.Ativo
            };

            return Ok(
                ApiResposta<LoginRespostaDto>.Ok(
                    resposta,
                    "Login realizado com sucesso."));
        }
    }
}
