using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBFC13Lib;
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

        var lineItems = JsonSerializer.Deserialize<List<BillLineItem>>(lineItemsJson, QuickBooksService.JsonInputOptions);
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

    private class BillLineItem
    {
        public string AccountName { get; set; } = "";
        public double Amount { get; set; }
        public string? Description { get; set; }
    }
}
