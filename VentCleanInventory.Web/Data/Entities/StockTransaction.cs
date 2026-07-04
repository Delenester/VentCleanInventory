using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace VentCleanInventory.Web.Data.Entities;

public class StockTransaction
{
    public int Id { get; set; }

    public int? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }

    public int? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }

    public TransactionType TransactionType { get; set; }

    public DateTime Date { get; set; }

    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public int? ClientId { get; set; }
    public Organization? Client { get; set; }

    public int? SupplierId { get; set; }
    public Organization? Supplier { get; set; }

    public int? WorkObjectId { get; set; }
    public WorkObject? WorkObject { get; set; }

    public int? WorkLogId { get; set; }
    public WorkLog? WorkLog { get; set; }

    public RequestStatus? RequestStatusValue { get; set; }

    public int? RelatedTransactionId { get; set; }
    public StockTransaction? RelatedTransaction { get; set; }

    public WriteOffReason? WriteOffReason { get; set; }

    public string? Note { get; set; }

    public string? ItemsData { get; set; }

    public decimal? EstimatedCost { get; set; }

    public DateTime? PlannedStartDate { get; set; }

    public DateTime? PlannedEndDate { get; set; }

    [MaxLength(128)]
    public string? AssignedMasterId { get; set; }

    [MaxLength(2000)]
    public string? ClientNote { get; set; }

    [MaxLength(2000)]
    public string? ManagerNote { get; set; }

    [MaxLength(50)]
    public string? ContractNumber { get; set; }

    public decimal? Area { get; set; }

    [MaxLength(260)]
    public string? BlueprintPhotoPath { get; set; }

    [MaxLength(30)]
    public string? ActNumber { get; set; }

    public DateTime? ActDate { get; set; }

    public string? ActCreatedByUserId { get; set; }

    public WriteOffActStatus? ActStatus { get; set; }

    public List<TransactionItemDto> GetItems()
    {
        if (string.IsNullOrWhiteSpace(ItemsData)) return [];
        return JsonSerializer.Deserialize<List<TransactionItemDto>>(ItemsData) ?? [];
    }

    public void SetItems(List<TransactionItemDto> items)
    {
        ItemsData = JsonSerializer.Serialize(items);
    }
}

public class TransactionItemDto
{
    public int? InventoryItemId { get; set; }
    public int? NomenclatureId { get; set; }
    public string? NomenclatureName { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public EquipmentCondition? ConditionAtMoment { get; set; }
    public string? ConditionNote { get; set; }
}
