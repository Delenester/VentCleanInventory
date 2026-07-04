using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class Warehouse
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public global::VentCleanInventory.Web.Data.WarehouseType Type { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public string? MasterUserId { get; set; }
    public global::VentCleanInventory.Web.Data.ApplicationUser? MasterUser { get; set; }
}

