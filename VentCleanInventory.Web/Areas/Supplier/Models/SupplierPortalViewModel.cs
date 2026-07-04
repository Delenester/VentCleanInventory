using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Supplier.Models;

public class SupplierPortalViewModel
{
    public global::VentCleanInventory.Web.Data.Entities.Organization? Supplier { get; set; }
    public int SuppliedItemsCount { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public int RecentTransactionsCount { get; set; }
    public List<SuppliedItemInfo> SuppliedItems { get; set; } = [];
}

public class SuppliedItemInfo
{
    public string NomenclatureName { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string? BatchNumber { get; set; }
    public decimal Quantity { get; set; }
}

public class SupplierSuppliesViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<SupplyInfo> Rows { get; set; } = [];
    public decimal TotalQuantity { get; set; }
}

public class SupplyInfo
{
    public DateTime Date { get; set; }
    public TransactionType TransactionType { get; set; }
    public string? Note { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
}
