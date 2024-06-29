using AccountingTool;
using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.Services;
using Microsoft.EntityFrameworkCore;

namespace Financial_Manager.Client.Model
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext() : base()
        {
            Database.EnsureCreated();
        }

        public DbSet<FinancialOperation> FinancialOperations { get; set; } = null!;
        public DbSet<OperationCategory> OperationCategories { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configService = Ioc.Default.GetRequiredService<IConfigurationService>();
            optionsBuilder.UseSqlite(configService.GetConfigurationSettings()!.LocalDataBaseConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<FinancialOperation>(entity =>
            {
                entity.Property(p => p.Currency).HasConversion<string>();
                entity.Property(p => p.Type).HasConversion<string>();

                entity.HasOne(f => f.Category).
                WithMany(c => c.FinancialOperations).
                HasForeignKey(f => f.CategoryId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(p => p.UserStatus).HasConversion<string>();
                entity.Property(p => p.UserRole).HasConversion<string>();
                entity.Property(p => p.SelectedSortingType).HasConversion<string>();
            });
        }
    }
}
