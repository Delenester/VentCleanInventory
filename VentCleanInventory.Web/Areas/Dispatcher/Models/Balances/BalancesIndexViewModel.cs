using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Dispatcher.Models.Balances;

public class BalancesIndexViewModel
{
    public int? WarehouseId { get; set; }
    public int? NomenclatureId { get; set; }
    public DateTime? ExpirationBefore { get; set; }

    public IReadOnlyList<Warehouse> Warehouses { get; set; } = [];
    public IReadOnlyList<Nomenclature> Nomenclatures { get; set; } = [];

    public IReadOnlyList<Row> Rows { get; set; } = [];
    public IReadOnlyList<SummaryRow> SummaryByNomenclature { get; set; } = [];
    public int TotalPositions { get; set; }
    public decimal TotalQuantity { get; set; }
    public int ExpiringSoonCount { get; set; }

    public class Row
    {
        public required string WarehouseName { get; init; }
        public required string ItemName { get; init; }
        public string? SerialOrBatch { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public decimal Quantity { get; init; }
        public string Unit { get; init; } = "";
        public bool IsEquipment { get; init; }
    }

    public class SummaryRow
    {
        public required string ItemName { get; init; }
        public required string Unit { get; init; }
        public decimal TotalQuantity { get; init; }
        public int WarehouseCount { get; init; }
    }
}

