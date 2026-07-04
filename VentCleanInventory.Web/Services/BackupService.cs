using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Services;

public class BackupService(ApplicationDbContext db, ILogger<BackupService> logger)
{
    public async Task<string> RunBackupAsync(string directoryPath, CancellationToken ct = default)
    {
        Directory.CreateDirectory(directoryPath);

        var dbName = db.Database.GetDbConnection().Database;
        var fileName = $"{dbName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
        var fullPath = Path.Combine(directoryPath, fileName);

        // BACKUP DATABASE requires proper SQL Server permissions.
        var sql = $"BACKUP DATABASE [{dbName}] TO DISK = @path WITH INIT, COPY_ONLY";
        var param = new SqlParameter("@path", fullPath);

        await db.Database.ExecuteSqlRawAsync(sql, [param], ct);
        logger.LogInformation("Database backup created at {Path}", fullPath);
        return fullPath;
    }
}

