using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quetzal.UI.Servicos;
using Quetzal.UI.ViewModels;

namespace Quetzal.UI.Areas.Admin.Controllers
{
    // Gestão de contas: ativar/desativar controla o acesso à Minha área.
    // O login continua permitido mesmo quando o usuário está inativo.
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operador")]
    public class UsuariosController : Controller
    {
        private readonly ApiCliente _api;

        public UsuariosController(ApiCliente api)
        {
            _api = api;
        }

        // GET: /Admin/Usuarios
        public async Task<IActionResult> Index()
        {
            var resposta = await _api.GetAsync<List<UsuarioApiModelo>>("api/Usuarios");

            if (!resposta.Sucesso || resposta.Dados == null)
            {
                TempData["MensagemErro"] = resposta.Mensagem;
                return View(new List<UsuarioViewModel>());
            }

            var viewModel = resposta.Dados.Select(u => new UsuarioViewModel
            {
                Id = u.Id,
                NomeCompleto = u.NomeCompleto,
                Email = u.Email,
                Telefone = u.Telefone,
                Ativo = u.Ativo,
                DataCadastro = u.DataCadastro,
                Perfis = u.Perfis
            })
            .OrderByDescending(u => u.Ativo)
            .ThenBy(u => u.NomeCompleto, StringComparer.OrdinalIgnoreCase)
            .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desativar(string id)
        {
            var resposta = await _api.PutAsync<bool>($"api/Usuarios/{id}/desativar");

            TempData[resposta.Sucesso ? "MensagemSucesso" : "MensagemErro"] =
                resposta.Sucesso ? "Usuário desativado com sucesso." : resposta.Mensagem;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(string id)
        {
            var resposta = await _api.PutAsync<bool>($"api/Usuarios/{id}/ativar");

            TempData[resposta.Sucesso ? "MensagemSucesso" : "MensagemErro"] =
                resposta.Sucesso ? "Usuário ativado com sucesso." : resposta.Mensagem;

            return RedirectToAction(nameof(Index));
        }

        // -> corresponde a UsuarioDto na API
        public class UsuarioApiModelo
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