namespace VentCleanInventory.Web.Data;

public static class ServicePriceList
{
    public static readonly Dictionary<string, decimal> PricesPerM2 = new()
    {
        ["Плановая чистка вентиляции"] = 12.50m,
        ["Замена фильтров"] = 8.00m,
        ["Ремонт оборудования"] = 20.00m,
        ["Профилактическое обслуживание"] = 15.00m,
        ["Диагностика/осмотр"] = 5.00m,
        ["Аварийный ремонт"] = 25.00m,
        ["Прочее"] = 10.00m,
    };

    public static decimal GetPrice(string serviceType)
    {
        return PricesPerM2.GetValueOrDefault(serviceType, 10.00m);
    }
}
