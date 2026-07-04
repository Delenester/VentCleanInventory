using System.ComponentModel.DataAnnotations;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Dispatcher.Models.Requests;

public class RequestIssueViewModel
{
    public int RequestId { get; set; }

    public string MasterUserId { get; set; } = "";
    public int CentralWarehouseId { get; set; }
    public int MobileWarehouseId { get; set; }

    public string Header { get; set; } = "";

    public List<Line> Lines { get; set; } = [];

    public class Line
    {
        public int InventoryItemId { get; set; }
        public string ItemDisplay { get; set; } = "";
        public decimal Available { get; set; }

        [Display(Name = "Кол-во к выдаче")]
        [Range(0.000, 1000000)]
        public decimal Quantity { get; set; }

        [Display(Name = "Состояние (для оборудования)")]
        public EquipmentCondition? Condition { get; set; }

        [Display(Name = "Примечание к состоянию")]
        public string? ConditionNote { get; set; }
    }
}

