using Microsoft.EntityFrameworkCore;

namespace Calculator16.Models
{
    public class CalculatorContext : DbContext
    {
        public DbSet<DataInputVariant> DataInputVariants { get; set; }
        
        public CalculatorContext(DbContextOptions<CalculatorContext> options) : base(options) 
        { 
        }
    }
}
