using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Quetzal.UI.Infraestrutura;
using Quetzal.UI.Servicos;
using Quetzal.UI.ViewModels;

namespace Quetzal.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin,Operador")]
    public class ProjetoCController : Controller
    {
        private const string MensagemProjetoNoPortfolio =
            "Não é possível modificar um projeto enquanto ele estiver no portfólio público do site.";

        private readonly ApiCliente _api;
        private readonly ServicoUpload _upload;

        public ProjetoCController(ApiCliente api, ServicoUpload upload)
        {
            _api = api;
            _upload = upload;
        }

        // GET: /Admin/ProjetoC
        public async Task<IActionResult> Index()
        {
            var resposta = await _api.GetAsync<List<ProjetoCApiModelo>>("api/ProjetoC/todas");

            if (!resposta.Sucesso || resposta.Dados == null)
            {
                TempData["MensagemErro"] = resposta.Mensagem;
                return View(new List<ProjetoCViewModel>());
            }

            var viewModel = resposta.Dados.Select(p => new ProjetoCViewModel
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                ImagemUpload = p.Fotos.FirstOrDefault() ?? p.ImagemUpload,
                ClienteNome = string.IsNullOrWhiteSpace(p.UsuarioNome)
                    ? "(sem nome cadastrado)"
                    : p.UsuarioNome,
                Ativo = p.Ativo,
                DataCadastro = p.DataCadastro
            })
            .OrderByDescending(p => p.Ativo)
            .ThenBy(p => p.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();

            return View(viewModel);
        }

        // GET: /Admin/ProjetoC/Criar
        public async Task<IActionResult> Criar()
        {
            var viewModel = new ProjetoCEdicaoViewModel();

            await PreencherClientes(viewModel);

            return View("Editar", viewModel);
        }

        // POST: /Admin/ProjetoC/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(ProjetoCEdicaoViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PreencherClientes(viewModel);
                return View("Editar", viewModel);
            }

            var resultadoFotos = await SalvarFotosAsync(viewModel.FotosArquivos);

            if (!resultadoFotos.Sucesso)
            {
                ModelState.AddModelError(
                    nameof(viewModel.FotosArquivos),
                    resultadoFotos.Erro!);

                await PreencherClientes(viewModel);
                return View("Editar", viewModel);
            }

            var dto = new CriarProjetoCApiModelo
            {
                Nome = viewModel.Nome,
                Descricao = viewModel.Descricao,
                UsuarioId = viewModel.UsuarioId ?? string.Empty,
                Fotos = resultadoFotos.Fotos
            };

            var resposta = await _api.PostAsync<ProjetoCApiModelo, CriarProjetoCApiModelo>(
                "api/ProjetoC",
                dto);

            if (!resposta.Sucesso)
            {
                AdicionarErrosDaApi(resposta.Erros, resposta.Mensagem);
                await PreencherClientes(viewModel);
                return View("Editar", viewModel);
            }

            TempData["MensagemSucesso"] = "Projeto do cliente cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/ProjetoC/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var resposta = await _api.GetAsync<ProjetoCApiModelo>($"api/ProjetoC/{id}");

            if (!resposta.Sucesso || resposta.Dados == null)
            {
                TempData["MensagemErro"] = "Projeto não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var dados = resposta.Dados;

            var fotosExistentes = dados.FotosDetalhadas
                .OrderBy(f => f.Ordem)
                .Select(f => new ProjetoCFotoEdicaoViewModel
                {
                    Id = f.Id,
                    Foto = f.Foto,
                    Ordem = f.Ordem
                })
                .ToList();

            // Compatibilidade com projetos antigos que ainda possuem
            // somente ImagemUpload.
            if (!fotosExistentes.Any() &&
                !string.IsNullOrWhiteSpace(dados.ImagemUpload))
            {
                fotosExistentes.Add(new ProjetoCFotoEdicaoViewModel
                {
                    Id = 0,
                    Foto = dados.ImagemUpload,
                    Ordem = 1
                });
            }

            var viewModel = new ProjetoCEdicaoViewModel
            {
                Id = dados.Id,
                Nome = dados.Nome,
                Descricao = dados.Descricao,
                UsuarioId = dados.UsuarioId,

                ClienteNome = string.IsNullOrWhiteSpace(dados.UsuarioNome)
                            ? "(sem nome cadastrado)"
                            : dados.UsuarioNome,

                ImagemAtualUrl = dados.ImagemUpload,

                FotosExistentes = fotosExistentes,

                FotosExistentesSelecionadas = fotosExistentes
                         .Where(f => f.Id > 0)
                         .Select(f => f.Id)
                         .ToList()
            };

            await PreencherClientes(viewModel);

            return View(viewModel);
        }

        // POST: /Admin/ProjetoC/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
       int id,
           ProjetoCEdicaoViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PreencherClientes(viewModel);
                await PreencherFotosExistentes(viewModel, id);

                return View(viewModel);
            }

            // --------------------------------------------------------
            // SALVAR SOMENTE AS NOVAS FOTOS
            // --------------------------------------------------------

            var resultadoFotos =
                await SalvarFotosAsync(viewModel.FotosArquivos);

            if (!resultadoFotos.Sucesso)
            {
                ModelState.AddModelError(
                    nameof(viewModel.FotosArquivos),
                    resultadoFotos.Erro!);

                await PreencherClientes(viewModel);
                await PreencherFotosExistentes(viewModel, id);

                return View(viewModel);
            }

            // --------------------------------------------------------
            // MONTAR DTO
            // --------------------------------------------------------

            var dto = new AtualizarProjetoCApiModelo
            {
                Id = id,
                Nome = viewModel.Nome,
                Descricao = viewModel.Descricao,
                UsuarioId = viewModel.UsuarioId ?? string.Empty,

                // IDs das fotos que já existem e devem permanecer.
                FotosExistentesIds =
                    viewModel.FotosExistentesSelecionadas?
                        .Distinct()
                        .ToList()
                    ?? new List<int>(),

                // Somente as fotos novas adicionadas nesta edição.
                NovasFotos =
                    resultadoFotos.Fotos
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .Distinct()
                        .ToList()
            };

            // --------------------------------------------------------
            // ENVIAR PARA API
            // --------------------------------------------------------

            var resposta =
                await _api.PutAsync<
                    ProjetoCApiModelo,
                    AtualizarProjetoCApiModelo>(
                        $"api/ProjetoC/{id}/atualizar",
                        dto);

            if (!resposta.Sucesso)
            {
                AdicionarErrosDaApi(
                    resposta.Erros,
                    MensagemDeErro(resposta));

                await PreencherClientes(viewModel);
                await PreencherFotosExistentes(viewModel, id);

                return View(viewModel);
            }

            TempData["MensagemSucesso"] =
                "Projeto atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/ProjetoC/Desativar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desativar(int id)
        {
            var resposta = await _api.DeleteAsync<object>($"api/ProjetoC/{id}/desativar");

            TempData[resposta.Sucesso ? "MensagemSucesso" : "MensagemErro"] =
                resposta.Sucesso ? "Projeto desativado." : MensagemDeErro(resposta);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/ProjetoC/Reativar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reativar(int id)
        {
            var resposta = await _api.PutAsync<object, object>(
                $"api/ProjetoC/{id}/reativar",
                new { });

            TempData[resposta.Sucesso ? "MensagemSucesso" : "MensagemErro"] =
                resposta.Sucesso ? "Projeto reativado." : resposta.Mensagem;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/ProjetoC/ExcluirPermanente/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPermanente(int id)
        {
            var resposta = await _api.DeleteAsync<object>(
                $"api/ProjetoC/{id}/permanente");

            TempData[resposta.Sucesso ? "MensagemSucesso" : "MensagemErro"] =
                resposta.Sucesso
                    ? "Projeto excluído permanentemente."
                    : MensagemDeErro(resposta);

            return RedirectToAction(nameof(Index));
        }

        // Quando a API bloqueia a operação porque o projeto/imagens estão em uso no
        // portfólio público, ela responde com um status HTTP de erro sem o corpo
        // padronizado. O ApiCliente marca essas falhas com CodigoHttp (401 já tem
        // mensagem própria e não passa por aqui), então trocamos o texto técnico
        // "Erro HTTP: ..." pela mensagem amigável.
        private static string MensagemDeErro<T>(ApiResposta<T> resposta)
            => resposta.CodigoHttp.HasValue
                ? MensagemProjetoNoPortfolio
                : resposta.Mensagem ?? string.Empty;

        private async Task PreencherClientes(
            ProjetoCEdicaoViewModel viewModel)
        {
            var resposta = await _api.GetAsync<List<UsuarioApiModelo>>("api/Usuarios");

            if (!resposta.Sucesso || resposta.Dados == null)
                return;

            viewModel.ClientesDisponiveis = resposta.Dados
                .OrderBy(u => u.NomeCompleto, StringComparer.OrdinalIgnoreCase)
                .ThenBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = string.IsNullOrWhiteSpace(u.NomeCompleto)
                        ? u.Email
                        : $"{u.NomeCompleto}{(u.Ativo ? string.Empty : " (Inativo)")}",
                    Selected = u.Id == viewModel.UsuarioId
                })
                .ToList();
        }

        private async Task PreencherFotosExistentes(
            ProjetoCEdicaoViewModel viewModel,
            int id)
        {
            var resposta = await _api.GetAsync<ProjetoCApiModelo>(
                $"api/ProjetoC/{id}");

            if (!resposta.Sucesso || resposta.Dados == null)
                return;

            viewModel.FotosExistentes = resposta.Dados.FotosDetalhadas
                .OrderBy(f => f.Ordem)
                .Select(f => new ProjetoCFotoEdicaoViewModel
                {
                    Id = f.Id,
                    Foto = f.Foto,
                    Ordem = f.Ordem
                })
                .ToList();

            if (!viewModel.FotosExistentes.Any() &&
                !string.IsNullOrWhiteSpace(resposta.Dados.ImagemUpload))
            {
                viewModel.FotosExistentes.Add(
                    new ProjetoCFotoEdicaoViewModel
                    {
                        Id = 0,
                        Foto = resposta.Dados.ImagemUpload,
                        Ordem = 1
                    });
            }
        }

        private async Task<(bool Sucesso, List<string> Fotos, string? Erro)> SalvarFotosAsync(
            IEnumerable<IFormFile>? arquivos)
        {
            var fotos = new List<string>();

            foreach (var arquivo in arquivos ?? Enumerable.Empty<IFormFile>())
            {
                var resultado = await _upload.SalvarImagemAsync(
                    arquivo,
                    "projetos");

                if (!resultado.Sucesso)
                {
                    return (false, fotos, resultado.Erro);
                }

                if (!string.IsNullOrWhiteSpace(resultado.CaminhoRelativo))
                {
                    fotos.Add(resultado.CaminhoRelativo);
                }
            }

            return (true, fotos, null);
        }

        private void AdicionarErrosDaApi(
            List<string>? erros,
            string mensagemGeral)
        {
            if (erros != null && erros.Count > 0)
            {
                foreach (var erro in erros)
                {
                    ModelState.AddModelError(string.Empty, erro);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, mensagemGeral);
            }
        }

        // -> corresponde a ProjetoCDto na API
        public class ProjetoCApiModelo
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public string ImagemUpload { get; set; } = string.Empty;
            public string UsuarioId { get; set; } = string.Empty;
            public string? UsuarioNome { get; set; }
            public string ClienteId { get; set; } = string.Empty;
            public List<string> Fotos { get; set; } = new();
            public List<ProjetoCFotoApiModelo> FotosDetalhadas { get; set; } = new();
            public bool Ativo { get; set; }
            public DateTime DataCadastro { get; set; }
            public DateTime? DataAtualizacao { get; set; }
            public DateTime? DataExclusao { get; set; }
        }

        public class ProjetoCFotoApiModelo
        {
            public int Id { get; set; }
            public string Foto { get; set; } = string.Empty;
            public int Ordem { get; set; }
        }

        public class CriarProjetoCApiModelo
        {
            public string Nome { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public string UsuarioId { get; set; } = string.Empty;
            public string? UsuarioNome { get; set; }
            public List<string> Fotos { get; set; } = new();
            public bool Ativo { get; set; } = true;
        }

        public class AtualizarProjetoCApiModelo
        {
            public int Id { get; set; }

            public string Nome { get; set; } = string.Empty;

            public string Descricao { get; set; } = string.Empty;

            public string UsuarioId { get; set; } = string.Empty;

            // IDs das fotos que JÁ EXISTEM e devem continuar.
            public List<int> FotosExistentesIds { get; set; } = new();

            // Apenas fotos realmente novas.
            public List<string> NovasFotos { get; set; } = new();
        }

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
