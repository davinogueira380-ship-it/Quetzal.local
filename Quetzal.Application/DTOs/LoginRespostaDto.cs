using System;
using System.Collections.Generic;

namespace Quetzal.Application.DTOs
{
    public class LoginRespostaDto
    {
        public string UsuarioId { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expiracao { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Perfis { get; set; } = new();
    }
}
