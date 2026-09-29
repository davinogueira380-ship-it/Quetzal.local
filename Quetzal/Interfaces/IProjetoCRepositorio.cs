using Quetzal.Domain.Entidades;

namespace Quetzal.Domain.Interfaces
{
    public interface IProjetoCRepositorio
    {
        Task<IEnumerable<ProjetoC>> ObterTodosAsync(bool incluirInativos = false);
        Task<ProjetoC?> ObterPorIdAsync(int id);
        Task<ProjetoC?> ObterPorUsuarioIdAsync(string usuarioId);

        //Task<IEnumerable<ProjetoC>> FiltrarPorAmbienteAsync(string? termo, int? ambienteId = null);
        //Task<IEnumerable<ProjetoC>> ObterPorAmbienteAsync(int ambienteId);
        Task<ProjetoC> AdicionarAsync(ProjetoC projetoC);
        Task AtualizarAsync(ProjetoC projetoC);
        Task DesativarAsync(int id);
        Task ExcluirPermanentementeAsync(int id);
        Task ReativarAsync(int id);
    }
}
