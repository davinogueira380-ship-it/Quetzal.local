using System;
using System.Collections.Generic;
using System.Text;

namespace Quetzal.Domain.Entidades
{
    public class PortfolioFoto
    {

        public int Id { get; set; }

        // Portfólio ao qual a foto foi selecionada
        public int PortfolioId { get; set; }

        public Portfolio Portfolio { get; set; } = null!;

        // Foto original cadastrada no ProjetoC
        public int ProjetoCFotoId { get; set; }

        public ProjetoCFoto ProjetoCFoto { get; set; } = null!;
    }
}
