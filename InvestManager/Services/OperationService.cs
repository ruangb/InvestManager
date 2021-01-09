using InvestManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace InvestManager.Services
{
    public class OperationService
    {
        private readonly InvestManagerContext _context;

        public OperationService(InvestManagerContext context)
        {
            _context = context;
        }

        public async Task<List<Operation>> FindAllAsync()
        {
            return await _context.Operation.ToListAsync();
        }

        public async Task InsertAsync(Operation obj)
        {
            _context.Add(obj);
            await _context.SaveChangesAsync();
        }

        public async Task<Operation> FindByIdAsync(int id)
        {
            return await _context.Operation.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                _context.Operation.Remove(await _context.Operation.FindAsync(id));
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException e)
            {
                // implementar erro
            }
        }

        public async Task UpdateAsync(Operation obj)
        {
            bool hasAny = await _context.Operation.AnyAsync(x => x.Id == obj.Id);
            if (!hasAny)
                throw new NotFoundException("Id not found");

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

    [Serializable]
    internal class NotFoundException : Exception
    {
        public NotFoundException()
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class IntegrityException : Exception
    {
        public IntegrityException()
        {
        }

        public IntegrityException(string message) : base(message)
        {
        }

        public IntegrityException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IntegrityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
