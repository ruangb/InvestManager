using System;
using System.Linq;
using InvestManager.Models;

namespace InvestManager.Data
{
    public class SeedingService
    {
        private InvestManagerContext _context;

        public SeedingService(InvestManagerContext context)
        {
            _context = context;
        }

        public void Seed()
        {
            if (_context.Operation.Any())
                return;
        }
    }
}
