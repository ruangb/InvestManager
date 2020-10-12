using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InvestManager.Models
{
    public class InvestManagerContext : DbContext
    {
        public InvestManagerContext (DbContextOptions<InvestManagerContext> options)
            : base(options)
        {
        }

        public DbSet<InvestManager.Models.Operation> Operation { get; set; }
    }
}
