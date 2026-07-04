using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Data;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<Organization>();
        builder.Ignore<Nomenclature>();
        builder.Ignore<WorkObject>();
        builder.Ignore<Warehouse>();
        builder.Ignore<InventoryItem>();
        builder.Ignore<StockBalance>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<WorkLog>();
    }
}
