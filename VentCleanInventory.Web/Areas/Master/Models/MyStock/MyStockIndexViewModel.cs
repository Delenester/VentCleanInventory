namespace VentCleanInventory.Web.Areas.Master.Models.MyStock;

public class MyStockIndexViewModel
{
    public string Filter { get; set; } = "all"; // all|equip|cons

    public IReadOnlyList<Row> Rows { get; set; } = [];

    public class Row
    {
        public required int InventoryItemId { get; init; }
        public required string Name { get; init; }
        public string? SerialNumber { get; init; }
        public decimal Quantity { get; init; }
        public bool IsEquipment { get; init; }
    }
}

