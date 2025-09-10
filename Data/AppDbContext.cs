using Microsoft.EntityFrameworkCore;
using PROG7312_POE.Domain;

namespace PROG7312_POE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Issue> Issues => Set<Issue>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
    }
}
