using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Quetzal.Application.DTOs;
using Quetzal.Application.Servicos.Interfaces;
using Quetzal.Domain.Entidades;
using Quetzal.Domain.Interfaces;
using Quetzal.Application.Utilitarios;
namespace Quetzal.Application.Servicos.Implementacoes
{
    public class ProjetoCServico : IProjetoCServico
    {
        private readonly IProjetoCRepositorio _repositorio;
        private readonly IMapper _mapper;
        private readonly IPortfolioRepositorio _portfolioRepositorio;

        public ProjetoCServico(
            IProjetoCRepositorio repositorio,
            IPortfolioRepositorio portfolioRepositorio,
            IMapper mapper)
        {
            _repositorio = repositorio;
            _portfolioRepositorio = portfolioRepositorio;
            _mapper = mapper;
        }

        // =========================================================
        // OBTER TODOS
        // =========================================================

        public async Task<ApiResposta<IEnumerable<ProjetoCDto>>> ObterTodosAsync(
            bool incluirInativos = false)
        {
            try
            {
                var projetosC =
                    await _repositorio.ObterTodosAsync(incluirInativos);

                var dtos =
                    _mapper.Map<IEnumerable<ProjetoCDto>>(projetosC);

                return ApiResposta<IEnumerable<ProjetoCDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return ApiResposta<IEnumerable<ProjetoCDto>>.Falha(
                    $"Erro ao obter projetos: {ex.Message}");
            }
        }

        // =========================================================
        // OBTER POR ID
        // =========================================================

        public async Task<ApiResposta<ProjetoCDto>> ObterPorIdAsync(int id)
        {
            try
            {
                var projetoC =
                    await _repositorio.ObterPorIdAsync(id);

                if (projetoC == null)
                {
                    return ApiResposta<ProjetoCDto>.Falha(
                        "Projeto nao encontrado.");
                }

                var dto =
                    _mapper.Map<ProjetoCDto>(projetoC);

                return ApiResposta<ProjetoCDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResposta<ProjetoCDto>.Falha(
                    $"Erro ao obter o projeto: {ex.Message}");
            }
        }

        // =========================================================
        // MEU PROJETO
        // =========================================================

        public async Task<ApiResposta<ProjetoCDto>> ObterMeuAsync(string usuarioId)
        {
            try
            {
                var projetoC = await _repositorio.ObterPorUsuarioIdAsync(usuarioId);

                if (projetoC == null)
                {
                    return ApiResposta<ProjetoCDto>.Falha(
                        "Nenhum projeto ativo foi vinculado a este usuário.");
                }

                var dto = _mapper.Map<ProjetoCDto>(projetoC);
                return ApiResposta<ProjetoCDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResposta<ProjetoCDto>.Falha(
                    $"Erro ao obter o projeto do usuário: {ex.Message}");
            }
        }

        // =========================================================
        // CADASTRAR
        // =========================================================

        public async Task<ApiResposta<ProjetoCDto>> CadastrarAsync(
            CriarProjetoCDto dto)
        {
            try
            {
                var projetoC =
                    _mapper.Map<ProjetoC>(dto);

                // Define o usuário/cliente proprietário do projeto.
                if (!string.IsNullOrWhiteSpace(dto.UsuarioId))
                {
                    projetoC.UsuarioId = dto.UsuarioId;
                }

                // =================================================
                // FOTOS
                // =================================================
                // O DTO recebe List<string>.
                //
                // Aqui cada string é transformada em uma entidade
                // ProjetoCFoto para ser armazenada na tabela
                // ProjetoCFotos.
                // =================================================

                projetoC.Fotos = (dto.Fotos ?? new List<string>())
                    .Select((foto, indice) => new ProjetoCFoto
                    {
                        ProjetoC = projetoC,
                        Foto = ProcessadorImagem.ReduzirImagemBase64(foto),
                        Ordem = indice + 1
                    })
                    .ToList();

                var projetoCAdicionado =
                    await _repositorio.AdicionarAsync(projetoC);

                var projetoCDto =
                    _mapper.Map<ProjetoCDto>(projetoCAdicionado);

                return ApiResposta<ProjetoCDto>.Ok(
                    projetoCDto,
                    "Projeto do cliente cadastrado com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                return ApiResposta<ProjetoCDto>.Falha(
                    $"Erro ao cadastrar o projeto do cliente no banco: {DetalharExcecao(ex)}");
            }
            catch (Exception ex)
            {
                return ApiResposta<ProjetoCDto>.Falha(
                    $"Erro ao cadastrar o projeto do cliente: {DetalharExcecao(ex)}");
            }
        }

        // =========================================================
        // ATUALIZAR
        // =========================================================

        public async Task<ApiResposta<ProjetoCDto>> AtualizarAsync(
                 int id, AtualizarProjetoCDto dto)
        {
            try
            {
                var projetoCExistente =
                    await _repositorio.ObterPorIdAsync(id);

                if (projetoCExistente == null)
                {
                    return ApiResposta<ProjetoCDto>.Falha(
                        "Projeto do cliente não encontrado.");
                }

                // =================================================
                // ATUALIZA DADOS BÁSICOS
                // =================================================

                projetoCExistente.NomeProjeto = dto.Nome;
                projetoCExistente.Descricao = dto.Descricao;

                if (!string.IsNullOrWhiteSpace(dto.UsuarioId))
                {
                    projetoCExistente.UsuarioId = dto.UsuarioId;
                }

                projetoCExistente.DataAtualizacao = DateTime.UtcNow;

                // =================================================
                // FOTOS EXISTENTES QUE DEVEM PERMANECER
                // =================================================

                var fotosExistentesIds =
                    (dto.FotosExistentesIds ?? new List<int>())
                    .Distinct()
                    .ToHashSet();

                var fotosAtuais =
                    projetoCExistente.Fotos
                    .OrderBy(f => f.Ordem)
                    .ToList();

                // =================================================
                // IDENTIFICA FOTOS QUE O USUÁRIO DESEJOU REMOVER
                // =================================================

                var fotosParaRemover =
    fotosAtuais
        .Where(f => !fotosExistentesIds.Contains(f.Id))
        .ToList();

                foreach (var foto in fotosParaRemover)
                {
                    // Verifica se esta foto está sendo utilizada
                    // em algum Portfólio.
                    var estaNoPortfolio =
                        await _portfolioRepositorio
                            .FotoEstaNoPortfolioAsync(foto.Id);

                    if (estaNoPortfolio)
                    {
                        return ApiResposta<ProjetoCDto>.Falha(
                            $"A foto {foto.Id} não pode ser removida porque está sendo utilizada no portfólio.");
                    }

                    // Se não está no portfólio, pode remover.
                    projetoCExistente.Fotos.Remove(foto);
                }

                // =================================================
                // IMPORTANTE:
                // NÃO APAGA AUTOMATICAMENTE FOTO QUE PODE ESTAR
                // SENDO UTILIZADA PELO PORTFÓLIO.
                //
                // Por enquanto, apenas remove da coleção as fotos
                // que não possuem Id válido/antigas não persistidas.
                //
                // Fotos persistidas devem ser tratadas separadamente
                // se houver relação com PortfolioFotos.
                // =================================================

                foreach (var foto in fotosParaRemover)
                {
                    // Se a foto já existe no banco, NÃO removemos aqui.
                    // Isso evita quebrar:
                    // PortfolioFotos.ProjetoCFotoId -> ProjetoCFotos.Id

                    if (foto.Id <= 0)
                    {
                        projetoCExistente.Fotos.Remove(foto);
                    }
                }

                // =================================================
                // REORGANIZA AS FOTOS EXISTENTES
                // =================================================

                var ordem = 1;

                foreach (var foto in fotosAtuais
                    .Where(f => fotosExistentesIds.Contains(f.Id))
                    .OrderBy(f => f.Ordem))
                {
                    foto.Ordem = ordem++;
                }

                // =================================================
                // ADICIONA SOMENTE AS NOVAS FOTOS
                // =================================================

                var novasFotos =
                    (dto.NovasFotos ?? new List<string>())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Distinct()
                    .ToList();

                foreach (var novaFoto in novasFotos)
                {
                    projetoCExistente.Fotos.Add(
                        new ProjetoCFoto
                        {
                            ProjetoCId = projetoCExistente.Id,
<<<<<<< HEAD
                            Foto = novaFoto,
                            Ordem = ordem++
=======
                            Foto = ProcessadorImagem.ReduzirImagemBase64(novasFotos[i]),
                            Ordem = i + 1
>>>>>>> 3389567ab67d36a62bdc222ec25e4c880aae4f64
                        });
                }

                // =================================================
                // SALVA
                // =================================================

                await _repositorio.AtualizarAsync(projetoCExistente);

                // Busca novamente para garantir dados atualizados
                var projetoAtualizado =
                    await _repositorio.ObterPorIdAsync(id);

                var projetoCDto =
                    _mapper.Map<ProjetoCDto>(
                        projetoAtualizado ?? projetoCExistente);

                return ApiResposta<ProjetoCDto>.Ok(
                    projetoCDto,
                    "Projeto do cliente atualizado com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                return ApiResposta<ProjetoCDto>.Falha(
                    $"Erro ao atualizar o projeto do cliente no banco: {DetalharExcecao(ex)}");
            }
            catch (Exception ex)
            {
                return ApiResposta<ProjetoCDto>.Falha(
                    $"Erro ao atualizar o projeto do cliente: {DetalharExcecao(ex)}");
            }
        }

        // =========================================================
        // DESATIVAR
        // =========================================================

        public async Task<ApiResposta<bool>> DesativarAsync(int id)
        {
            try
            {
                var projetoC =
                    await _repositorio.ObterPorIdAsync(id);

                if (projetoC == null)
                {
                    return ApiResposta<bool>.Falha(
                        "Projeto do cliente não encontrado.");
                }

                await _repositorio.DesativarAsync(id);

                return ApiResposta<bool>.Ok(
                    true,
                    "Projeto do cliente desativado com sucesso.");
            }
            catch (Exception ex)
            {
                return ApiResposta<bool>.Falha(
                    $"Erro ao desativar o projeto do cliente: {ex.Message}");
            }
        }

        // =========================================================
        // EXCLUIR PERMANENTEMENTE
        // =========================================================

        public async Task<ApiResposta<bool>> ExcluirPermanentementeAsync(
            int id)
        {
            try
            {
                var projetoC =
                    await _repositorio.ObterPorIdAsync(id);

                if (projetoC == null)
                {
                    return ApiResposta<bool>.Falha(
                        "Projeto do cliente não encontrado.");
                }

                await _repositorio.ExcluirPermanentementeAsync(id);

                return ApiResposta<bool>.Ok(
                    true,
                    "Projeto do cliente excluído permanentemente com sucesso.");
            }
            catch (Exception ex)
            {
                return ApiResposta<bool>.Falha(
                    $"Erro ao excluir permanentemente o projeto do cliente: {ex.Message}");
            }
        }

        // =========================================================
        // REATIVAR
        // =========================================================

        private static string DetalharExcecao(Exception ex)
        {
            var mensagens = new List<string>();
            var atual = ex;

            while (atual != null)
            {
                if (!string.IsNullOrWhiteSpace(atual.Message) &&
                    !mensagens.Contains(atual.Message))
                {
                    mensagens.Add(atual.Message);
                }

                atual = atual.InnerException;
            }

            return string.Join(" | ", mensagens);
        }

        public async Task<ApiResposta<bool>> ReativarAsync(int id)
        {
            try
            {
                var projetoC =
                    await _repositorio.ObterPorIdAsync(id);

                if (projetoC == null)
                {
                    return ApiResposta<bool>.Falha(
                        "Projeto do cliente não encontrado.");
                }

                if (projetoC.Ativo)
                {
                    return ApiResposta<bool>.Falha(
                        "Projeto do cliente já está ativo.");
                }

                await _repositorio.ReativarAsync(id);

                return ApiResposta<bool>.Ok(
                    true,
                    "Projeto do cliente reativado com sucesso.");
            }
            catch (Exception ex)
            {
                return ApiResposta<bool>.Falha(
                    $"Erro ao reativar o projeto do cliente: {ex.Message}");
            }
        }
    }
}