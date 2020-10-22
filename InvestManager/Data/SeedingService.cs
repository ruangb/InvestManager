using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InvestManager.Models;
using static InvestManager.Models.Enums;
using static InvestManager.Models.Operation;

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
            if (_context.Operation.Any() || _context.Share.Any())
                return;

            Operation op1 = new Operation(2, "AZUL4", 24.53, 10, DateTime.Now, "Venda");

            _context.Add(op1);

            _context.SaveChanges();
        }
    }
}
