using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Client.Models;

public class ClientRequestViewModel
{
    [Display(Name = "Название объекта")]
    [Required(ErrorMessage = "Введите название объекта")]
    [MaxLength(200)]
    public string ObjectName { get; set; } = "";

    [Display(Name = "Адрес / улица")]
    [Required(ErrorMessage = "Введите адрес")]
    [MaxLength(500)]
    public string ObjectAddress { get; set; } = "";

    [Display(Name = "Тип услуги")]
    [Required(ErrorMessage = "Выберите тип услуги")]
    public string ServiceType { get; set; } = "";

    [Display(Name = "Описание проблемы")]
    [Required(ErrorMessage = "Опишите проблему")]
    [MaxLength(2000)]
    public string Description { get; set; } = "";

    [Display(Name = "Зона/площадка")]
    [MaxLength(200)]
    public string? ZoneName { get; set; }

    [Display(Name = "Площадь (м²)")]
    public decimal? Area { get; set; }

    [Display(Name = "Примерная стоимость (руб.)")]
    public decimal? EstimatedCost { get; set; }

    [Display(Name = "Фото / чертёж")]
    public IFormFile? BlueprintPhoto { get; set; }

    public static readonly string[] ServiceTypes = [
        "Плановая чистка вентиляции",
        "Замена фильтров",
        "Ремонт оборудования",
        "Профилактическое обслуживание",
        "Диагностика/осмотр",
        "Аварийный ремонт",
        "Прочее"
    ];
}
