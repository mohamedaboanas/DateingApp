using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : DbContext
    {
          public DataContext(DbContextOptions options) : base(options)
        {
        }

        public DataContext()
        {
            
        }

        public DbSet<AppUser> Users { get; set; }
    }
}