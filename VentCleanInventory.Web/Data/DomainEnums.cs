namespace VentCleanInventory.Web.Data;

public enum WarehouseType
{
    Central = 1,
    Mobile = 2,
}

public enum TransactionType
{
    Receipt = 1,
    Issue = 2,
    Return = 3,
    InventoryAdjustment = 4,
    WriteOff = 5,
    Transfer = 6,
}

public enum RequestStatus
{
    New = 1,
    Approved = 2,
    Completed = 3,
    Rejected = 4,
    ClientConfirmed = 5,
    Assigned = 6,
    InProgress = 7,
}

public enum EquipmentCondition
{
    Ok = 1,
    Wear = 2,
    NeedsMaintenance = 3,
}

public enum WriteOffReason
{
    Wear = 1,
    Expired = 2,
    Lost = 3,
}

public enum AccountType
{
    Internal = 0,
    Client = 1,
    Supplier = 2,
}

public enum WriteOffActStatus
{
    Draft = 1,
    Approved = 2,
}

public enum OrganizationType
{
    Client = 1,
    Supplier = 2,
}

public enum SupplyRequestStatus
{
    New = 1,
    Sent = 2,
    Confirmed = 3,
    Partial = 4,
    Completed = 5,
    Cancelled = 6,
}

