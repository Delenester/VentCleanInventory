using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Nomenclature> Nomenclatures => Set<Nomenclature>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<WorkObject> WorkObjects => Set<WorkObject>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<WorkLog> WorkLogs => Set<WorkLog>();
    public DbSet<DefectReport> DefectReports => Set<DefectReport>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WorkChecklist> WorkChecklists => Set<WorkChecklist>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("dbo");

        builder.Ignore<ApplicationUser>();

        builder.Entity<Nomenclature>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<InventoryItem>()
            .HasIndex(x => x.SerialNumber)
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL");

        builder.Entity<Warehouse>()
            .HasIndex(x => x.MasterUserId)
            .HasFilter("[MasterUserId] IS NOT NULL");

        builder.Entity<StockBalance>()
            .HasIndex(x => new { x.WarehouseId, x.InventoryItemId })
            .IsUnique();

        builder.Entity<InventoryItem>()
            .Property(x => x.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Entity<StockBalance>()
            .Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Entity<WorkLog>()
            .Property(x => x.Meters)
            .HasPrecision(18, 2);

        builder.Entity<Organization>()
            .HasIndex(x => new { x.Type, x.Unp })
            .IsUnique()
            .HasFilter("[Unp] IS NOT NULL");

        builder.Entity<StockTransaction>()
            .HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<StockTransaction>()
            .HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<StockTransaction>()
            .HasOne(x => x.RelatedTransaction)
            .WithMany()
            .HasForeignKey(x => x.RelatedTransactionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<InventoryItem>()
            .HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Feedback>()
            .HasOne(x => x.WorkLog)
            .WithMany()
            .HasForeignKey(x => x.WorkLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
