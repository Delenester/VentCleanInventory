using System.ComponentModel.DataAnnotations;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Master.Models.Work;

public class WorkConsumeViewModel
{
    [Display(Name = "Объект")]
    [Required(ErrorMessage = "Выберите объект.")]
    public int? WorkObjectId { get; set; }

    [Display(Name = "Зона")]
    [MaxLength(200)]
    public string? ZoneName { get; set; }

    public IReadOnlyList<WorkObject> Objects { get; set; } = [];

    public List<Line> Lines { get; set; } = [new Line()];

    [Display(Name = "Погонные метры воздуховодов")]
    public decimal? Meters { get; set; }

    [Display(Name = "Количество очищенных решёток")]
    public decimal? Grids { get; set; }

    [Display(Name = "Фото брака/дефекта")]
    public Microsoft.AspNetCore.Http.IFormFile? DefectPhoto { get; set; }

    public class Line
    {
        [Display(Name = "Материал (из моего склада)")]
        [Required(ErrorMessage = "Выберите материал.")]
        public int? InventoryItemId { get; set; }

        public string? ItemDisplay { get; set; }

        [Display(Name = "Кол-во")]
        [Range(0.001, 1000000, ErrorMessage = "Количество должно быть больше 0.")]
        public decimal Quantity { get; set; } = 1;
    }
}
