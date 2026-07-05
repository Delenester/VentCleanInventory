using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Dispatcher.Models.SupplyRequests;

public class SupplyRequestReceiveViewModel
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public int CentralWarehouseId { get; set; }
    public List<ReceiveLine> Items { get; set; } = [];

    public class ReceiveLine
    {
        public int SupplyRequestItemId { get; set; }
        public int NomenclatureId { get; set; }
        public string NomenclatureName { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal OrderedQuantity { get; set; }

        [Display(Name = "Получено")]
        public decimal ReceivedQuantity { get; set; }

        [Display(Name = "Цена")]
        public decimal? UnitPrice { get; set; }

        public bool IsEquipment { get; set; }
    }
}
