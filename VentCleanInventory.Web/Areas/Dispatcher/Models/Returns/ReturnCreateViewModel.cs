using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Dispatcher.Models.Returns;

public class ReturnCreateViewModel
{
    [Display(Name = "Мастер")]
    [Required(ErrorMessage = "Выберите мастера.")]
    public string? MasterUserId { get; set; }

    public IReadOnlyList<MasterOption> Masters { get; set; } = [];

    public int CentralWarehouseId { get; set; }
    public int MobileWarehouseId { get; set; }

    public List<Line> Lines { get; set; } = [];

    public class MasterOption
    {
        public required string Id { get; init; }
        public required string Display { get; init; }
    }

    public class Line
    {
        public int InventoryItemId { get; set; }
        public string ItemDisplay { get; set; } = "";
        public decimal Available { get; set; }

        [Display(Name = "Количество")]
        [Range(typeof(decimal), "0", "9999999", ErrorMessage = "Количество от 0 до 9999999.")]
        public decimal Quantity { get; set; }

        [Display(Name = "Состояние")]
        public VentCleanInventory.Web.Data.EquipmentCondition? Condition { get; set; }

        [Display(Name = "Примечание")]
        public string? ConditionNote { get; set; }
    }
}

