using InvestManager.Manager.Repositories;
using InvestManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace InvestManager.Services
{
    public class ParameterService : IParameterRepository
    {
        private readonly InvestManagerContext _context;

        public ParameterService(InvestManagerContext context)
        {
            _context = context;
        }

        public async Task<List<Parameter>> FindAllAsync()
        {
            return await _context.Parameter.ToListAsync();
        }

        public async Task InsertAsync(Parameter obj)
        {
            _context.Add(obj);
            await _context.SaveChangesAsync();
        }

        public async Task<Parameter> FindByIdAsync(int id)
        {
            return await _context.Parameter.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                _context.Parameter.Remove(await _context.Parameter.FindAsync(id));
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException e)
            {
                // implementar erro
            }
        }

        public async Task UpdateAsync(Parameter obj)
        {
            bool hasAny = await _context.Parameter.AnyAsync(x => x.Id == obj.Id);
            if (!hasAny)
                throw new NotFoundException("Id not Found");

            try
            {
                _context.Update(obj);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                throw new DBConcurrencyException(e.Message);
            }
        }
    }
}
