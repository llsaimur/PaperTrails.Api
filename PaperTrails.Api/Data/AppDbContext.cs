using Microsoft.EntityFrameworkCore;
using PaperTrails.Api.Models;

namespace PaperTrails.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
