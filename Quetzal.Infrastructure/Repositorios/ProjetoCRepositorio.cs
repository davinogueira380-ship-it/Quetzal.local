using Microsoft.EntityFrameworkCore;
using Quetzal.Domain.Entidades;
using Quetzal.Domain.Interfaces;
using Quetzal.Infrastructure.Dados;

namespace Quetzal.Infrastructure.Repositorios
{
    public class ProjetoCRepositorio : IProjetoCRepositorio
    {
        private readonly QuetzalContexto _context;

        public ProjetoCRepositorio(QuetzalContexto context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjetoC>> ObterTodosAsync(
            bool incluirInativos = false)
        {
            IQueryable<ProjetoC> query = _context.ProjetoC
                .Include(p => p.Usuario)
                .Include(p => p.Fotos);

            if (!incluirInativos)
            {
                query = query.Where(p => p.Ativo);
            }

            return await query.ToListAsync();
        }

        public async Task<ProjetoC?> ObterPorIdAsync(int id)
        {
            return await _context.ProjetoC
                .Include(p => p.Usuario)
                .Include(p => p.Fotos)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProjetoC?> ObterPorUsuarioIdAsync(string usuarioId)
        {
            return await _context.ProjetoC
                .Include(p => p.Usuario)
                .Include(p => p.Fotos)
                .Where(p => p.UsuarioId == usuarioId && p.Ativo)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<ProjetoC> AdicionarAsync(ProjetoC projetoC)
        {
            _context.ProjetoC.Add(projetoC);

            await _context.SaveChangesAsync();

            return projetoC;
        }

        public async Task AtualizarAsync(ProjetoC projetoC)
        {
            // _context.ProjetoC.Update(projetoC);

            // O projeto já foi carregado pelo mesmo DbContext
            // e, portanto, já está sendo rastreado pelo EF Core.


            await _context.SaveChangesAsync();
        }

        public async Task DesativarAsync(int id)
        {
            var projetoC = await _context.ProjetoC.FindAsync(id);

            if (projetoC != null)
            {
                projetoC.Ativo = false;
                projetoC.DataAtualizacao = DateTime.Now;
                projetoC.DataExclusao = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        public async Task ExcluirPermanentementeAsync(int id)
        {
            var projetoC = await _context.ProjetoC
                .Include(p => p.Fotos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projetoC != null)
            {
                _context.ProjetoC.Remove(projetoC);

                await _context.SaveChangesAsync();
            }
        }

        public async Task ReativarAsync(int id)
        {
            var projetoC = await _context.ProjetoC.FindAsync(id);

            if (projetoC != null)
            {
                projetoC.Ativo = true;
                projetoC.DataAtualizacao = DateTime.Now;
                projetoC.DataExclusao = null;

                await _context.SaveChangesAsync();
            }
        }
    }
}