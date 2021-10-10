using Microsoft.EntityFrameworkCore;

namespace InvestManager.Models
{
    public class InvestManagerContext : DbContext
    {
        public InvestManagerContext (DbContextOptions<InvestManagerContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Operation> Operation { get; set; }
        public DbSet<Parameter> Parameter { get; set; }
    }
}
