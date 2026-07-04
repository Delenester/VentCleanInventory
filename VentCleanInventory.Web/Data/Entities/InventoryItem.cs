using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class InventoryItem
{
    public int Id { get; set; }

    public int NomenclatureId { get; set; }
    public Nomenclature Nomenclature { get; set; } = null!;

    [MaxLength(50)]
    public string? SerialNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public decimal? PurchasePrice { get; set; }

    public DateTime? ExpirationDate { get; set; }

    [MaxLength(100)]
    public string? BatchNumber { get; set; }
}

