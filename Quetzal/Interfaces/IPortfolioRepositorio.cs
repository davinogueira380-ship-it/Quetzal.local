
// Objetivo: "Define o contrato (interface) que qualquer implementação de repositorio dos Portfolios que deve seguir no Quetzal.
// Abstrai o acesso a dados e permite injecao de dependencia."
 


using Quetzal.Domain.Entidades;

namespace Quetzal.Domain.Interfaces
{
    public interface IPortfolioRepositorio
    {
        Task<bool> FotoEstaNoPortfolioAsync(int projetoCFotoId);

        Task<IEnumerable<Portfolio>> ObterTodosAsync(bool incluirInativos = false);

        Task<Portfolio?> ObterPorIdAsync(int id);

        Task<IEnumerable<Portfolio>> FiltrarPorAmbienteAsync(string? termo, int? ambienteId = null);

        Task<IEnumerable<Portfolio>> ObterPorAsync(int ambienteId);

        Task<Portfolio> AdicionarAsync(Portfolio portfolio);

        Task AtualizarAsync(Portfolio portfolio);

        Task DesativarAsync(int id);

        Task ExcluirPermanentementeAsync(int id);

        Task ReativarAsync(int id);



    }
}
