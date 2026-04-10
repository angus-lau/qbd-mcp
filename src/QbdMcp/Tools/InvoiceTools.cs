using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using Interop.QBFC17;
using QbdMcp.Models;
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
            req => req.AppendInvoiceQueryRq().ORInvoiceQuery.InvoiceFilter.MaxReturned.SetValue(maxReturned),
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

    [McpServerTool, Description("Create an invoice in QuickBooks Desktop. Customer name is fuzzy-matched.")]
    public static string CreateInvoice(
        QuickBooksService qb,
        NameResolver resolver,
        [Description("Customer name (partial match OK)")] string customerName,
        [Description("Invoice date in YYYY-MM-DD format")] string date,
        [Description("Due date in YYYY-MM-DD format")] string dueDate,
        [Description("Line items as JSON array: [{\"itemName\": \"...\", \"description\": \"...\", \"rate\": 100.00, \"quantity\": 1}]")] string lineItemsJson,
        [Description("Optional invoice/reference number")] string? refNumber = null)
    {
        var customerResult = resolver.ResolveCustomer(customerName);
        if (!customerResult.Success)
            return customerResult.ErrorMessage!;

        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";
        if (!DateTime.TryParseExact(dueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDueDate))
            return "Error: Invalid due date format. Use YYYY-MM-DD.";

        var lineItems = JsonSerializer.Deserialize<List<SalesLineItem>>(lineItemsJson, QuickBooksService.JsonInputOptions);
        if (lineItems == null || lineItems.Count == 0)
            return "Error: At least one line item is required.";

        var result = qb.SendRequest(req =>
        {
            var invoice = req.AppendInvoiceAddRq();
            invoice.CustomerRef.FullName.SetValue(customerResult.ResolvedName);
            invoice.TxnDate.SetValue(parsedDate);
            invoice.DueDate.SetValue(parsedDueDate);

            if (!string.IsNullOrEmpty(refNumber))
                invoice.RefNumber.SetValue(refNumber);

            foreach (var item in lineItems)
            {
                var line = invoice.ORInvoiceLineAddList.Append().InvoiceLineAdd;
                if (!string.IsNullOrEmpty(item.ItemName))
                    line.ItemRef.FullName.SetValue(item.ItemName);
                if (!string.IsNullOrEmpty(item.Description))
                    line.Desc.SetValue(item.Description);
                if (item.Rate.HasValue)
                    line.ORRatePriceLevel.Rate.SetValue(item.Rate.Value);
                if (item.Quantity.HasValue)
                    line.Quantity.SetValue(item.Quantity.Value);
            }
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        var invoiceRet = (IInvoiceRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Created",
            TxnID = invoiceRet.TxnID.GetValue(),
            RefNumber = invoiceRet.RefNumber?.GetValue(),
            Customer = invoiceRet.CustomerRef.FullName.GetValue(),
            Date = invoiceRet.TxnDate.GetValue().ToString("yyyy-MM-dd"),
            Total = invoiceRet.Subtotal?.GetValue()
        }, QuickBooksService.JsonOptions);
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
                var filter = query.ORInvoiceQuery.InvoiceFilter;
                filter.MaxReturned.SetValue(maxReturned);
                filter.PaidStatus.SetValue(ENPaidStatus.psNotPaidOnly);
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
