namespace VentCleanInventory.Web.Models;

public class WorkLogDetailViewModel
{
    public int Id { get; set; }
    public string ObjectName { get; set; } = "";
    public string ObjectAddress { get; set; } = "";
    public string MasterName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public string? ZoneName { get; set; }
    public string? Description { get; set; }
    public string? MaterialsUsed { get; set; }
    public decimal? Meters { get; set; }
    public int? Grids { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsCompleted { get; set; }
    public bool? ChecklistDone { get; set; }
    public List<ChecklistItem> Checklist { get; set; } = [];
    public int? RelatedRequestId { get; set; }
    public string? RelatedRequestStatus { get; set; }
    public string? ContractNumber { get; set; }
    public List<DefectItem> Defects { get; set; } = [];
}

public class ChecklistItem
{
    public string ItemName { get; set; } = "";
    public bool IsDone { get; set; }
    public string? Note { get; set; }
    public string? PhotoPath { get; set; }
}

public class DefectItem
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public string? ZoneName { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
