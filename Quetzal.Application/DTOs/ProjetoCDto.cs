using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Quetzal.Application.DTOs
{
    public class ProjetoCFotoDto
    {
        public int Id { get; set; }

        public string Foto { get; set; } = string.Empty;

        public int Ordem { get; set; }
    }

    public class ProjetoCDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string UsuarioId { get; set; } = string.Empty;

        public string? UsuarioNome { get; set; }

        public string ClienteId { get; set; } = string.Empty;

        public List<string> Fotos { get; set; } =
            new List<string>();

        public List<ProjetoCFotoDto> FotosDetalhadas { get; set; } =
            new List<ProjetoCFotoDto>();

        public bool Ativo { get; set; }

        public DateTime DataCadastro { get; set; }

        public DateTime? DataAtualizacao { get; set; }

        public DateTime? DataExclusao { get; set; }
    }

    public class CriarProjetoCDto
    {
        [Required(
            ErrorMessage = "O nome do Projeto é obrigatório.")]
        [MaxLength(
            200,
            ErrorMessage =
                "O nome do Projeto não pode ter mais que 200 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(
            ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(
            ErrorMessage = "O cliente é obrigatório.")]
        public string UsuarioId { get; set; } = string.Empty;

        public string? UsuarioNome { get; set; }

        public List<string> Fotos { get; set; } =
            new List<string>();

        public bool Ativo { get; set; } = true;
    }

    public class AtualizarProjetoCDto
    {
        [Required(
            ErrorMessage = "O Id é obrigatório para atualizar.")]
        public int Id { get; set; }

        [Required(
            ErrorMessage = "O nome do Projeto é obrigatório.")]
        [MaxLength(
            200,
            ErrorMessage =
                "O nome do Projeto não pode ter mais que 200 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(
            ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(
            ErrorMessage = "O cliente é obrigatório.")]
        public string UsuarioId { get; set; } = string.Empty;

        public string? UsuarioNome { get; set; }

        public List<int> FotosExistentesIds { get; set; } =
            new List<int>();

        public List<string> NovasFotos { get; set; } =
            new List<string>();

        public bool Ativo { get; set; } = true;
    }

}