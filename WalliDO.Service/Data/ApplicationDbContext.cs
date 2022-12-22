using Microsoft.EntityFrameworkCore;

namespace WalliDO.Service.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public virtual DbSet<Entity.File> Files { get; set; } = null!;
        public virtual DbSet<Entity.Directory> Directories { get; set; } = null!;
        public virtual DbSet<Entity.Bucket> Buckets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
