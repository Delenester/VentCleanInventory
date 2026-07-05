using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Supplier.Models;

public class SupplyDetailViewModel
{
    public DateTime Date { get; set; }
    public TransactionType TransactionType { get; set; }
    public string? Note { get; set; }
    public List<ItemInfo> Items { get; set; } = new();

    public class ItemInfo
    {
        public string NomenclatureName { get; set; } = "";
        public decimal Quantity { get; set; }
    }
}
