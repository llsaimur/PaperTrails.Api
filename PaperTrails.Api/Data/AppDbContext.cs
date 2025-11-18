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
        public DbSet<Category> Categories { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }


    }
}
