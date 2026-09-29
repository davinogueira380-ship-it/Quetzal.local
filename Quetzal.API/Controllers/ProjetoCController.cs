using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Quetzal.Application.DTOs;
using Quetzal.Application.Servicos.Interfaces;

namespace Quetzal.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjetoCController : ControllerBase
{
    private readonly IProjetoCServico _projetoCServico;

    public ProjetoCController(IProjetoCServico projetoCServico)
    {
        _projetoCServico = projetoCServico;
    }

    [HttpGet]
    public async Task<IActionResult> ObterAtivos()
    {
        var resposta = await _projetoCServico.ObterTodosAsync(incluirInativos: false);
        return Ok(resposta);
    }

    [HttpGet("meu")]
    [Authorize(Roles = "Cliente,Usuario")]
    public async Task<IActionResult> ObterMeu()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Unauthorized(
                ApiResposta<ProjetoCDto>.Falha("Usuário autenticado sem identificador."));
        }

        var resposta = await _projetoCServico.ObterMeuAsync(usuarioId);

        if (!resposta.Sucesso || resposta.Dados == null)
        {
            return NotFound(resposta);
        }

        return Ok(resposta);
    }

    [HttpGet("todas")]
    public async Task<IActionResult> ObterTodas()
    {
        var resposta = await _projetoCServico.ObterTodosAsync(incluirInativos: true);
        return Ok(resposta);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var resposta = await _projetoCServico.ObterPorIdAsync(id);
        if (!resposta.Sucesso) return NotFound(resposta);
        return Ok(resposta);
    }

    //[HttpGet("filtrar")]
    //public async Task<IActionResult> Filtrar([FromQuery] string? termo, [FromQuery] int? ambienteId)
    //{
    //    var resposta = await _projetoCServico.FiltrarPorAmbienteAsync(termo, ambienteId);
    //    return Ok(resposta);
    //}

    //[HttpGet("ambiente/{ambienteId}")]
    //public async Task<IActionResult> ObterPorAmbiente(int ambienteId)
    //{
    //    var resposta = await _projetoCServico.ObterPorAsync(ambienteId);
    //    return Ok(resposta);
    //}

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CriarProjetoCDto dto)
    {
        var resposta = await _projetoCServico.CadastrarAsync(dto);
        if (!resposta.Sucesso) return BadRequest(resposta);
        return StatusCode(201, resposta);
    }

    [HttpPut("{id}/atualizar")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarProjetoCDto dto)
    {
        var resposta = await _projetoCServico.AtualizarAsync(id, dto);
        if (!resposta.Sucesso) return BadRequest(resposta);
        return Ok(resposta);
    }

    [HttpDelete("{id}/desativar")]
    public async Task<IActionResult> Desativar(int id)
    {
        var resposta = await _projetoCServico.DesativarAsync(id);
        if (!resposta.Sucesso) return NotFound(resposta);
        return Ok(resposta);
    }

    [HttpPut("{id}/reativar")]
    public async Task<IActionResult> Reativar(int id)
    {
        var resposta = await _projetoCServico.ReativarAsync(id);
        if (!resposta.Sucesso) return NotFound(resposta);
        return Ok(resposta);
    }

    [HttpDelete("{id}/permanente")]
    public async Task<IActionResult> ExcluirPermanente(int id)
    {
        var resposta = await _projetoCServico.ExcluirPermanentementeAsync(id);
        if (!resposta.Sucesso) return NotFound(resposta);
        return Ok(resposta);
    }
}