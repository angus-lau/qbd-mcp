using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBFC13Lib;
using QbdMcp.Models;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class PaymentTools
{
    [McpServerTool, Description("Receive a customer payment in QuickBooks Desktop.")]
    public static string ReceivePayment(
        QuickBooksService qb,
        [Description("Customer name exactly as it appears in QuickBooks")] string customerName,
        [Description("Total payment amount")] double amount,
        [Description("Payment date in YYYY-MM-DD format")] string date,
        [Description("Payment method (e.g. Cash, Cheque, Visa)")] string paymentMethod,
        [Description("TxnID of an invoice to apply the payment to. If omitted, payment is auto-applied.")] string? invoiceTxnId = null,
        [Description("Account to deposit funds into. If omitted, uses the default Undeposited Funds account.")] string? depositToAccount = null)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";

        var result = qb.SendRequest(req =>
        {
            var payment = req.AppendReceivePaymentAddRq();
            payment.CustomerRef.FullName.SetValue(customerName);
            payment.TotalAmount.SetValue(amount);
            payment.TxnDate.SetValue(parsedDate);
            payment.PaymentMethodRef.FullName.SetValue(paymentMethod);

            if (!string.IsNullOrEmpty(depositToAccount))
                payment.DepositToAccountRef.FullName.SetValue(depositToAccount);

            if (!string.IsNullOrEmpty(invoiceTxnId))
            {
                var applied = payment.ORApplyPayment.AppliedToTxnAddList.Append();
                applied.TxnID.SetValue(invoiceTxnId);
                applied.PaymentAmount.SetValue(amount);
            }
            else
            {
                payment.ORApplyPayment.IsAutoApply.SetValue(true);
            }
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        var paymentRet = (IReceivePaymentRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Created",
            TxnID = paymentRet.TxnID.GetValue(),
            Customer = paymentRet.CustomerRef.FullName.GetValue(),
            Amount = paymentRet.TotalAmount.GetValue(),
            Date = paymentRet.TxnDate.GetValue().ToString("yyyy-MM-dd")
        }, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Write a cheque to a payee in QuickBooks Desktop.")]
    public static string MakeCheque(
        QuickBooksService qb,
        [Description("Payee name exactly as it appears in QuickBooks")] string payeeName,
        [Description("Bank account to write the cheque from")] string bankAccountName,
        [Description("Cheque date in YYYY-MM-DD format")] string date,
        [Description("Total cheque amount")] double amount,
        [Description("Expense lines as JSON array: [{\"accountName\": \"Expenses\", \"amount\": 100.00, \"description\": \"...\"}]")] string expenseLinesJson,
        [Description("Optional cheque/reference number")] string? refNumber = null)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";

        var expenseLines = JsonSerializer.Deserialize<List<ExpenseLine>>(expenseLinesJson, QuickBooksService.JsonInputOptions);
        if (expenseLines == null || expenseLines.Count == 0)
            return "Error: At least one expense line is required.";

        var result = qb.SendRequest(req =>
        {
            var check = req.AppendCheckAddRq();
            check.PayeeEntityRef.FullName.SetValue(payeeName);
            check.AccountRef.FullName.SetValue(bankAccountName);
            check.TxnDate.SetValue(parsedDate);

            if (!string.IsNullOrEmpty(refNumber))
                check.RefNumber.SetValue(refNumber);

            foreach (var line in expenseLines)
            {
                var expLine = check.ExpenseLineAddList.Append();
                expLine.AccountRef.FullName.SetValue(line.AccountName);
                expLine.Amount.SetValue(line.Amount);
                if (!string.IsNullOrEmpty(line.Description))
                    expLine.Memo.SetValue(line.Description);
            }
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        var checkRet = (ICheckRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Created",
            TxnID = checkRet.TxnID.GetValue(),
            Payee = checkRet.PayeeEntityRef.FullName.GetValue(),
            Amount = checkRet.Amount.GetValue(),
            Date = checkRet.TxnDate.GetValue().ToString("yyyy-MM-dd"),
            RefNumber = checkRet.RefNumber?.GetValue()
        }, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Create a sales receipt in QuickBooks Desktop.")]
    public static string CreateSalesReceipt(
        QuickBooksService qb,
        [Description("Customer name exactly as it appears in QuickBooks")] string customerName,
        [Description("Receipt date in YYYY-MM-DD format")] string date,
        [Description("Line items as JSON array: [{\"itemName\": \"Widget\", \"description\": \"...\", \"rate\": 50.00, \"quantity\": 2}]")] string lineItemsJson,
        [Description("Payment method (e.g. Cash, Cheque, Visa)")] string paymentMethod,
        [Description("Optional reference number")] string? refNumber = null)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";

        var lineItems = JsonSerializer.Deserialize<List<SalesLineItem>>(lineItemsJson, QuickBooksService.JsonInputOptions);
        if (lineItems == null || lineItems.Count == 0)
            return "Error: At least one line item is required.";

        var result = qb.SendRequest(req =>
        {
            var receipt = req.AppendSalesReceiptAddRq();
            receipt.CustomerRef.FullName.SetValue(customerName);
            receipt.TxnDate.SetValue(parsedDate);
            receipt.PaymentMethodRef.FullName.SetValue(paymentMethod);

            if (!string.IsNullOrEmpty(refNumber))
                receipt.RefNumber.SetValue(refNumber);

            foreach (var item in lineItems)
            {
                var line = receipt.ORSalesReceiptLineAddList.Append().SalesReceiptLineAdd;
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

        var receiptRet = (ISalesReceiptRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Created",
            TxnID = receiptRet.TxnID.GetValue(),
            Customer = receiptRet.CustomerRef.FullName.GetValue(),
            Total = receiptRet.Subtotal.GetValue(),
            Date = receiptRet.TxnDate.GetValue().ToString("yyyy-MM-dd")
        }, QuickBooksService.JsonOptions);
    }
}
