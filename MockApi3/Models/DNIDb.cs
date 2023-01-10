using Microsoft.EntityFrameworkCore;

namespace MockApi3.Models
{
    public class DNIContext : DbContext
    {
        public DNIContext(DbContextOptions<DNIContext> options)
        : base(options) { }

        public DbSet<DNI> DNIs { get; set; }
    }
}
