using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Services;

public static class StockTransactionReportHelper
{
    public static IEnumerable<(StockTransaction Tx, TransactionItemDto Item)> ExpandItems(
        IEnumerable<StockTransaction> transactions)
        => transactions.SelectMany(tx => tx.GetItems().Select(item => (tx, item)));
}
