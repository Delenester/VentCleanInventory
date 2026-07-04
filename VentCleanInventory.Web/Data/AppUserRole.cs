namespace VentCleanInventory.Web.Data;

public static class AppUserRole
{
    public const string Master = "Мастер";
    public const string Dispatcher = "Диспетчер";
    public const string Manager = "Руководитель";
    public const string Admin = "Администратор";
    public const string Client = "Клиент";
    public const string Supplier = "Поставщик";

    public static readonly string[] All = [Master, Dispatcher, Manager, Admin, Client, Supplier];

    public static readonly string[] InternalStaff = [Master, Dispatcher, Manager, Admin];
}

