using Microsoft.EntityFrameworkCore;
using Quetzal.Domain.Entidades;
using Quetzal.Domain.Interfaces;
using Quetzal.Infrastructure.Dados;

namespace Quetzal.Infrastructure.Repositorios
{
    public class PortfolioRepositorio : IPortfolioRepositorio
    {
        private readonly QuetzalContexto _context;

        public PortfolioRepositorio(QuetzalContexto context)
        {
            _context = context;
        }

        public async Task<bool> FotoEstaNoPortfolioAsync(
            int projetoCFotoId)
        {
            return await _context
                .Set<PortfolioFoto>()
                .AnyAsync(x =>
                    x.ProjetoCFotoId == projetoCFotoId);
        }

        public async Task<IEnumerable<Portfolio>> ObterTodosAsync(
            bool incluirInativos = false)
        {
            IQueryable<Portfolio> query = _context.Portfolios
                .Include(p => p.Ambiente)
                .Include(p => p.ProjetoC)
                .Include(p => p.Fotos)
                    .ThenInclude(pf => pf.ProjetoCFoto);

            if (!incluirInativos)
            {
                query = query.Where(p => p.Ativo);
            }

            return await query.ToListAsync();
        }

        public async Task<Portfolio?> ObterPorIdAsync(int id)
        {
            return await _context.Portfolios
                .Include(p => p.Ambiente)
                .Include(p => p.ProjetoC)
                .Include(p => p.Fotos)
                    .ThenInclude(pf => pf.ProjetoCFoto)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Portfolio>> FiltrarPorAmbienteAsync(
            string? termo,
            int? ambienteId = null)
        {
            var query = _context.Portfolios
                .Include(p => p.Ambiente)
                .Include(p => p.ProjetoC)
                .Include(p => p.Fotos)
                    .ThenInclude(pf => pf.ProjetoCFoto)
                .Where(p => p.Ativo);

            if (ambienteId.HasValue && ambienteId.Value > 0)
            {
                query = query.Where(
                    p => p.AmbienteId == ambienteId.Value);
            }

            if (!string.IsNullOrWhiteSpace(termo))
            {
                var t = $"%{termo}%";

                query = query.Where(
                    p =>
                        EF.Functions.Like(p.NomeProjeto!, t) ||
                        EF.Functions.Like(p.Descricao, t));
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Portfolio>> ObterPorAsync(
            int ambienteId)
        {
            return await _context.Portfolios
                .Include(p => p.Ambiente)
                .Include(p => p.ProjetoC)
                .Include(p => p.Fotos)
                    .ThenInclude(pf => pf.ProjetoCFoto)
                .Where(p => p.AmbienteId == ambienteId)
                .ToListAsync();
        }

        public async Task<Portfolio> AdicionarAsync(
            Portfolio portfolio)
        {
            await _context.Portfolios.AddAsync(portfolio);

            await _context.SaveChangesAsync();

            return portfolio;
        }

        public async Task AtualizarAsync(
            Portfolio portfolio)
        {
            _context.Portfolios.Update(portfolio);

            await _context.SaveChangesAsync();
        }

        public async Task DesativarAsync(int id)
        {
            var portfolio =
                await _context.Portfolios.FindAsync(id);

            if (portfolio != null)
            {
                portfolio.Ativo = false;
                portfolio.DataAtualizacao = DateTime.Now;
                portfolio.DataExclusao = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        public async Task ExcluirPermanentementeAsync(int id)
        {
            var portfolio =
                await _context.Portfolios.FindAsync(id);

            if (portfolio != null)
            {
                _context.Portfolios.Remove(portfolio);

                await _context.SaveChangesAsync();
            }
        }

        public async Task ReativarAsync(int id)
        {
            var portfolio =
                await _context.Portfolios.FindAsync(id);

            if (portfolio != null)
            {
                portfolio.Ativo = true;
                portfolio.DataExclusao = null;
                portfolio.DataAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }
    }
}