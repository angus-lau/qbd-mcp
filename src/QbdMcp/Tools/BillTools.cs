using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBFC16Lib;
using QbdMcp.Models;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class BillTools
{
    [McpServerTool, Description("Create a bill (accounts payable) in QuickBooks Desktop.")]
    public static string CreateBill(
        QuickBooksService qb,
        [Description("Vendor name exactly as it appears in QuickBooks")] string vendorName,
        [Description("Bill date in YYYY-MM-DD format")] string date,
        [Description("Due date in YYYY-MM-DD format")] string dueDate,
        [Description("Line items as JSON array: [{\"description\": \"...\", \"amount\": 100.00, \"accountName\": \"Expenses\"}]")] string lineItemsJson,
        [Description("Optional reference/bill number")] string? refNumber = null)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";
        if (!DateTime.TryParseExact(dueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDueDate))
            return "Error: Invalid due date format. Use YYYY-MM-DD.";

        var lineItems = JsonSerializer.Deserialize<List<ExpenseLine>>(lineItemsJson, QuickBooksService.JsonInputOptions);
        if (lineItems == null || lineItems.Count == 0)
            return "Error: At least one line item is required.";

        var result = qb.SendRequest(req =>
        {
            var bill = req.AppendBillAddRq();
            bill.VendorRef.FullName.SetValue(vendorName);
            bill.TxnDate.SetValue(parsedDate);
            bill.DueDate.SetValue(parsedDueDate);

            if (!string.IsNullOrEmpty(refNumber))
                bill.RefNumber.SetValue(refNumber);

            foreach (var item in lineItems)
            {
                var line = bill.ExpenseLineAddList.Append();
                line.AccountRef.FullName.SetValue(item.AccountName);
                line.Amount.SetValue(item.Amount);
                if (!string.IsNullOrEmpty(item.Description))
                    line.Memo.SetValue(item.Description);
            }
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        var billRet = (IBillRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Created",
            TxnID = billRet.TxnID.GetValue(),
            Vendor = billRet.VendorRef.FullName.GetValue(),
            Date = billRet.TxnDate.GetValue().ToString("yyyy-MM-dd"),
            Amount = billRet.AmountDue.GetValue()
        }, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Pay a bill in QuickBooks Desktop by writing a cheque.")]
    public static string PayBill(
        QuickBooksService qb,
        [Description("Vendor name exactly as it appears in QuickBooks")] string vendorName,
        [Description("TxnID of the bill to pay (from ListBills or CreateBill)")] string billTxnId,
        [Description("Bank account to pay from")] string bankAccountName,
        [Description("Payment date in YYYY-MM-DD format")] string date,
        [Description("Amount to pay")] double amount,
        [Description("Optional cheque/reference number")] string? refNumber = null)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";

        var result = qb.SendRequest(req =>
        {
            var payment = req.AppendBillPaymentCheckAddRq();
            payment.PayeeEntityRef.FullName.SetValue(vendorName);
            payment.BankAccountRef.FullName.SetValue(bankAccountName);
            payment.TxnDate.SetValue(parsedDate);

            if (!string.IsNullOrEmpty(refNumber))
                payment.RefNumber.SetValue(refNumber);

            var applied = payment.AppliedToTxnAddList.Append();
            applied.TxnID.SetValue(billTxnId);
            applied.PaymentAmount.SetValue(amount);
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        var paymentRet = (IBillPaymentCheckRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Paid",
            TxnID = paymentRet.TxnID.GetValue(),
            Vendor = paymentRet.PayeeEntityRef.FullName.GetValue(),
            Amount = paymentRet.Amount.GetValue(),
            Date = paymentRet.TxnDate.GetValue().ToString("yyyy-MM-dd")
        }, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("List bills from QuickBooks Desktop.")]
    public static string ListBills(
        QuickBooksService qb,
        [Description("Maximum number of bills to return")] int maxReturned = 25,
        [Description("Filter by vendor name (partial match, case-insensitive)")] string? vendorName = null,
        [Description("Filter by paid status: 'paidonly', 'notpaidonly', or leave blank for all")] string? paidStatus = null)
    {
        return qb.SendQuery<IBillRetList>(
            req =>
            {
                var query = req.AppendBillQueryRq();
                query.MaxReturned.SetValue(maxReturned);

                if (!string.IsNullOrEmpty(paidStatus))
                {
                    var status = paidStatus.ToLowerInvariant() switch
                    {
                        "paidonly" => ENPaidStatus.psPaidOnly,
                        "notpaidonly" => ENPaidStatus.psNotPaidOnly,
                        _ => ENPaidStatus.psAll
                    };
                    query.PaidStatus.SetValue(status);
                }
            },
            bills =>
            {
                var list = new List<object>();
                for (int i = 0; i < bills.Count; i++)
                {
                    var bill = bills.GetAt(i);
                    var vendor = bill.VendorRef?.FullName?.GetValue();

                    if (!string.IsNullOrEmpty(vendorName) &&
                        (vendor == null || !vendor.Contains(vendorName, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    list.Add(new
                    {
                        TxnID = bill.TxnID?.GetValue(),
                        Vendor = vendor,
                        RefNumber = bill.RefNumber?.GetValue(),
                        Date = bill.TxnDate?.GetValue().ToString("yyyy-MM-dd"),
                        DueDate = bill.DueDate?.GetValue().ToString("yyyy-MM-dd"),
                        AmountDue = bill.AmountDue?.GetValue(),
                        IsPaid = bill.IsPaid?.GetValue()
                    });
                }
                return list;
            });
    }
}
