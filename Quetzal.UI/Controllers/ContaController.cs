using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quetzal.UI.Servicos;
using Quetzal.UI.ViewModels;
using System.Security.Claims;

namespace Quetzal.UI.Controllers
{
    public class ContaController : Controller
    {
        private readonly ApiCliente _apiCliente;
        private readonly IWebHostEnvironment _ambiente;

        public ContaController(ApiCliente apiCliente, IWebHostEnvironment ambiente)
        {
            _apiCliente = apiCliente;
            _ambiente = ambiente;
        }

        [HttpGet]
        public IActionResult Login(string? retornoUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var perfis = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var ativoClaim = User.FindFirst("Ativo")?.Value;
                var usuarioAtivo = bool.TryParse(ativoClaim, out var ativo) && ativo;
                return RedirecionarPorPerfil(perfis, usuarioAtivo);
            }
            return View(new LoginViewModel { RetornoUrl = retornoUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var loginDto = new LoginRequisicao { Email = viewModel.Email, Senha = viewModel.Senha };
            var resposta = await _apiCliente.PostAsync<LoginResposta, LoginRequisicao>("api/auth/login", loginDto);

            if (!resposta.Sucesso || resposta.Dados == null)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(viewModel);
            }

            await AutenticarUsuarioAsync(resposta.Dados, viewModel.LembrarMe);

            if (!string.IsNullOrEmpty(viewModel.RetornoUrl) && Url.IsLocalUrl(viewModel.RetornoUrl))
                return Redirect(viewModel.RetornoUrl);

            return RedirecionarPorPerfil(resposta.Dados.Perfis, resposta.Dados.Ativo);
        }

        private IActionResult RedirecionarPorPerfil(List<string> perfis, bool usuarioAtivo)
        {
            if (perfis.Contains("Admin") || perfis.Contains("Operador"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if ((perfis.Contains("Cliente") || perfis.Contains("Usuario")) && usuarioAtivo)
                return RedirectToAction("Index", "MeuProjeto", new { area = "Cliente" });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public IActionResult Registro()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View(new RegistroViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var registrarDto = new RegistrarRequisicao
            {
                NomeCompleto = viewModel.NomeCompleto,
                Email = viewModel.Email,
                Telefone = viewModel.Telefone,
                Senha = viewModel.Senha,
                ConfirmarSenha = viewModel.ConfirmarSenha
            };

            var resposta = await _apiCliente.PostAsync<UsuarioResposta, RegistrarRequisicao>("api/auth/registrar", registrarDto);

            if (!resposta.Sucesso)
            {
                if (resposta.Erros != null && resposta.Erros.Count > 0)
                    foreach (var erro in resposta.Erros) ModelState.AddModelError(string.Empty, erro);
                else
                    ModelState.AddModelError(string.Empty, resposta.Mensagem);
                return View(viewModel);
            }

            // Cadastro nasce inativo, mas o login continua permitido.
            var loginDto = new LoginRequisicao { Email = viewModel.Email, Senha = viewModel.Senha };
            var loginResposta = await _apiCliente.PostAsync<LoginResposta, LoginRequisicao>("api/auth/login", loginDto);

            if (loginResposta.Sucesso && loginResposta.Dados != null)
            {
                await AutenticarUsuarioAsync(loginResposta.Dados, false);
                TempData["MensagemSucesso"] = "Cadastro realizado. Seu acesso à Minha área ficará disponível após a ativação.";
                return RedirecionarPorPerfil(loginResposta.Dados.Perfis, loginResposta.Dados.Ativo);
            }

            TempData["MensagemSucesso"] = "Cadastro realizado com sucesso! Faça login para continuar.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sair()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("quetzal_token");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AcessoNegado() => View();

        private async Task AutenticarUsuarioAsync(LoginResposta dadosLogin, bool lembrarMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, dadosLogin.UsuarioId),
                new Claim(ClaimTypes.Name, dadosLogin.NomeUsuario),
                new Claim(ClaimTypes.Email, dadosLogin.Email),
                new Claim("Ativo", dadosLogin.Ativo.ToString())
            };

            foreach (var perfil in dadosLogin.Perfis)
                claims.Add(new Claim(ClaimTypes.Role, perfil));

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidade);
            var propriedades = new AuthenticationProperties { IsPersistent = lembrarMe, ExpiresUtc = dadosLogin.Expiracao };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, propriedades);

            Response.Cookies.Append("quetzal_token", dadosLogin.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_ambiente.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = dadosLogin.Expiracao
            });
        }

        public class LoginRequisicao { public string Email { get; set; } = string.Empty; public string Senha { get; set; } = string.Empty; }
        public class LoginResposta
        {
            public string UsuarioId { get; set; } = string.Empty;
            public bool Ativo { get; set; }
            public string Token { get; set; } = string.Empty;
            public DateTime Expiracao { get; set; }
            public string NomeUsuario { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public List<string> Perfis { get; set; } = new();
        }
        public class RegistrarRequisicao
        {
            public string NomeCompleto { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Telefone { get; set; } = string.Empty;
            public string Senha { get; set; } = string.Empty;
            public string ConfirmarSenha { get; set; } = string.Empty;
        }
        public class UsuarioResposta
        {
            public string Id { get; set; } = string.Empty;
            public string NomeCompleto { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Telefone { get; set; } = string.Empty;
            public bool Ativo { get; set; }
            public DateTime DataCadastro { get; set; }
            public List<string> Perfis { get; set; } = new();
        }
    }
}
