using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Quetzal.UI.ViewModels
{
    // Telas: /Admin/ProjetoC/Criar e /Admin/ProjetoC/Editar/{id}
    public class ProjetoCEdicaoViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "O nome do Projeto é obrigatório")]
        [MaxLength(200, ErrorMessage = "O nome do Projeto não pode ter mais que 200 caracteres.")]
        [Display(Name = "Nome do Projeto")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Descrição é obrigatória.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        // Mantido por compatibilidade com outros pontos da UI.
        // O novo formulário usa FotosArquivos.
        [Display(Name = "Imagem do Projeto")]
        public IFormFile? ImagemArquivo { get; set; }

        public string? ImagemAtualUrl { get; set; }

        [Required(ErrorMessage = "O cliente é obrigatório.")]
        [Display(Name = "Cliente")]
        public string? UsuarioId { get; set; }

        // Somente leitura na edição.
        public string ClienteNome { get; set; } = string.Empty;

        public List<SelectListItem> ClientesDisponiveis { get; set; } = new();

        // Fotos já salvas no ProjetoC.
        public List<ProjetoCFotoEdicaoViewModel> FotosExistentes { get; set; } = new();

        // Fotos existentes que continuarão vinculadas após o salvar.
        public List<int> FotosExistentesSelecionadas { get; set; } = new();

        // Novos arquivos escolhidos no formulário.
        public List<IFormFile>? FotosArquivos { get; set; }

        public bool EhEdicao => Id.HasValue && Id.Value > 0;

        public string TituloPagina => EhEdicao ? "Editar Projeto" : "Novo Projeto";
    }

    public class ProjetoCFotoEdicaoViewModel
    {
        public int Id { get; set; }

        public string Foto { get; set; } = string.Empty;

        public int Ordem { get; set; }
    }
}
