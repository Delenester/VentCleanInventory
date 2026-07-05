using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class SupplyRequest
{
    public int Id { get; set; }

    [Display(Name = "Номер")]
    public string Number { get; set; } = "";

    [Display(Name = "Дата создания")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Поставщик")]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Display(Name = "Статус")]
    public SupplyRequestStatus Status { get; set; } = SupplyRequestStatus.New;

    [Display(Name = "Примечание")]
    [MaxLength(1000)]
    public string? Note { get; set; }

    public List<SupplyRequestItem> Items { get; set; } = [];
}

public class SupplyRequestItem
{
    public int Id { get; set; }

    public int SupplyRequestId { get; set; }
    public SupplyRequest SupplyRequest { get; set; } = null!;

    public int NomenclatureId { get; set; }
    public Nomenclature Nomenclature { get; set; } = null!;

    [Display(Name = "Количество")]
    public decimal Quantity { get; set; }

    [Display(Name = "Подтверждено поставщиком")]
    public decimal? ConfirmedQuantity { get; set; }

    [Display(Name = "Получено")]
    public decimal ReceivedQuantity { get; set; }

    [Display(Name = "Цена")]
    public decimal? UnitPrice { get; set; }

    [Display(Name = "Примечание")]
    [MaxLength(500)]
    public string? Note { get; set; }
}
