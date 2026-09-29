using Quetzal.Application.DTOs;

namespace Quetzal.Application.Servicos.Interfaces
{
    public interface IProjetoCServico
    {
        Task<ApiResposta<IEnumerable<ProjetoCDto>>> ObterTodosAsync(bool incluirInativos = false);
        Task<ApiResposta<ProjetoCDto>> ObterPorIdAsync(int id);
        Task<ApiResposta<ProjetoCDto>> ObterMeuAsync(string usuarioId);
        //Task<ApiResposta<IEnumerable<ProjetoCDto>>> FiltrarPorAmbienteAsync(string? termo, int? ambienteId = null);
        //Task<ApiResposta<IEnumerable<ProjetoCDto>>> ObterPorAsync(int projetoId);
        Task<ApiResposta<ProjetoCDto>> CadastrarAsync(CriarProjetoCDto dto);
        Task<ApiResposta<ProjetoCDto>> AtualizarAsync(int id, AtualizarProjetoCDto dto);
        Task<ApiResposta<bool>> DesativarAsync(int id);
        Task<ApiResposta<bool>> ExcluirPermanentementeAsync(int id);
        Task<ApiResposta<bool>> ReativarAsync(int id);

        //verificar a questão das listas de projetos por ambiente. 



    }
}
