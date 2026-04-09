using System.ComponentModel;
using ModelContextProtocol.Server;
using QBFC13Lib;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class InvoiceTools
{
    [McpServerTool, Description("List recent invoices from QuickBooks Desktop.")]
    public static string ListInvoices(
        QuickBooksService qb,
        [Description("Maximum number of invoices to return")] int maxReturned = 25)
    {
        return qb.SendQuery<IInvoiceRetList>(
            req => req.AppendInvoiceQueryRq().MaxReturned.SetValue(maxReturned),
            invoices =>
            {
                var list = new List<object>();
                for (int i = 0; i < invoices.Count; i++)
                {
                    var inv = invoices.GetAt(i);
                    list.Add(new
                    {
                        TxnNumber = inv.TxnNumber?.GetValue(),
                        RefNumber = inv.RefNumber?.GetValue(),
                        Customer = inv.CustomerRef?.FullName?.GetValue(),
                        Date = inv.TxnDate?.GetValue().ToString("yyyy-MM-dd"),
                        DueDate = inv.DueDate?.GetValue().ToString("yyyy-MM-dd"),
                        Amount = inv.Subtotal?.GetValue(),
                        BalanceRemaining = inv.BalanceRemaining?.GetValue(),
                        IsPaid = inv.IsPaid?.GetValue()
                    });
                }
                return list;
            });
    }

    [McpServerTool, Description("Get overdue invoices from QuickBooks Desktop.")]
    public static string GetOverdueInvoices(
        QuickBooksService qb,
        [Description("Maximum number of invoices to return")] int maxReturned = 50)
    {
        return qb.SendQuery<IInvoiceRetList>(
            req =>
            {
                var query = req.AppendInvoiceQueryRq();
                query.MaxReturned.SetValue(maxReturned);
                query.PaidStatus.SetValue(ENPaidStatus.psNotPaidOnly);
                query.ORInvoiceQuery.InvoiceFilter.ORDateRangeFilter.DueDateRangeFilter.ToDueDateFilter.SetValue(DateTime.Today.AddDays(-1));
            },
            invoices =>
            {
                var list = new List<object>();
                var today = DateTime.Today;
                for (int i = 0; i < invoices.Count; i++)
                {
                    var inv = invoices.GetAt(i);
                    var dueDate = inv.DueDate?.GetValue();
                    list.Add(new
                    {
                        RefNumber = inv.RefNumber?.GetValue(),
                        Customer = inv.CustomerRef?.FullName?.GetValue(),
                        DueDate = dueDate?.ToString("yyyy-MM-dd"),
                        DaysOverdue = dueDate.HasValue ? (today - dueDate.Value).Days : (int?)null,
                        BalanceRemaining = inv.BalanceRemaining?.GetValue()
                    });
                }
                return list;
            });
    }
}
