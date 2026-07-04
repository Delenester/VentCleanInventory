using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Master.Models.MyRequests;

public class RequestCreateViewModel
{
    [Display(Name = "Объект")]
    [Required(ErrorMessage = "Выберите объект.")]
    public int? WorkObjectId { get; set; }

    [Display(Name = "Клиент")]
    public int? ClientId { get; set; }

    [Display(Name = "Поставщик")]
    public int? SupplierId { get; set; }

    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.WorkObject> Objects { get; set; } = [];
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Organization> Clients { get; set; } = [];
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Organization> Suppliers { get; set; } = [];
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Nomenclature> Nomenclatures { get; set; } = [];

    public List<Line> Lines { get; set; } = [];

    public class Line
    {
        public int? NomenclatureId { get; set; }
        public decimal Quantity { get; set; }
    }
}
