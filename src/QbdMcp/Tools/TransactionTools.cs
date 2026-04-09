using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBFC13Lib;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class TransactionTools
{
    [McpServerTool, Description("Void a transaction in QuickBooks Desktop.")]
    public static string VoidTransaction(
        QuickBooksService qb,
        [Description("Transaction type: Invoice, Bill, Check, SalesReceipt, CreditMemo, JournalEntry, BillPaymentCheck, ReceivePayment")] string transactionType,
        [Description("TxnID of the transaction to void")] string txnId)
    {
        ENTxnVoidType voidType;
        try
        {
            voidType = ParseTxnVoidType(transactionType);
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }

        var result = qb.SendRequest(req =>
        {
            var voidRq = req.AppendTxnVoidRq();
            voidRq.TxnVoidType.SetValue(voidType);
            voidRq.TxnID.SetValue(txnId);
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        return JsonSerializer.Serialize(new
        {
            Status = "Voided",
            TxnID = txnId,
            TransactionType = transactionType
        }, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Delete a transaction in QuickBooks Desktop. QuickBooks restricts deletion of reconciled and payroll transactions.")]
    public static string DeleteTransaction(
        QuickBooksService qb,
        [Description("Transaction type: Invoice, Bill, Check, SalesReceipt, CreditMemo, JournalEntry, BillPaymentCheck, ReceivePayment")] string transactionType,
        [Description("TxnID of the transaction to delete")] string txnId)
    {
        ENTxnDelType delType;
        try
        {
            delType = ParseTxnDelType(transactionType);
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }

        var result = qb.SendRequest(req =>
        {
            var delRq = req.AppendTxnDelRq();
            delRq.TxnDelType.SetValue(delType);
            delRq.TxnID.SetValue(txnId);
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        return JsonSerializer.Serialize(new
        {
            Status = "Deleted",
            TxnID = txnId,
            TransactionType = transactionType
        }, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Search transactions across Invoices, Bills, Checks, and SalesReceipts in QuickBooks Desktop.")]
    public static string SearchTransactions(
        QuickBooksService qb,
        [Description("Start date filter in YYYY-MM-DD format (optional)")] string? fromDate = null,
        [Description("End date filter in YYYY-MM-DD format (optional)")] string? toDate = null,
        [Description("Reference number to filter by (optional)")] string? refNumber = null,
        [Description("Maximum number of records to return per transaction type")] int maxReturned = 25)
    {
        DateTime? parsedFrom = null;
        DateTime? parsedTo = null;

        if (fromDate != null)
        {
            if (!QuickBooksService.TryParseDate(fromDate, out var pFrom, out var err))
                return err;
            parsedFrom = pFrom;
        }

        if (toDate != null)
        {
            if (!QuickBooksService.TryParseDate(toDate, out var pTo, out var err))
                return err;
            parsedTo = pTo;
        }

        var allResults = new List<object>();

        // Invoices
        try
        {
            var result = qb.SendRequest(req =>
            {
                var q = req.AppendInvoiceQueryRq();
                q.MaxReturned.SetValue(maxReturned);
                if (parsedFrom.HasValue || parsedTo.HasValue)
                {
                    if (parsedFrom.HasValue)
                        q.ORInvoiceQuery.InvoiceFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.FromTxnDate.SetValue(parsedFrom.Value);
                    if (parsedTo.HasValue)
                        q.ORInvoiceQuery.InvoiceFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.ToTxnDate.SetValue(parsedTo.Value);
                }
                if (!string.IsNullOrEmpty(refNumber))
                    q.ORInvoiceQuery.InvoiceFilter.RefNumberList.Add(refNumber);
            });

            if (result.StatusCode == 0 && result.Detail is IInvoiceRetList invoiceList)
            {
                for (int i = 0; i < invoiceList.Count; i++)
                {
                    var inv = invoiceList.GetAt(i);
                    allResults.Add(new
                    {
                        Type = "Invoice",
                        TxnID = inv.TxnID?.GetValue(),
                        RefNumber = inv.RefNumber?.GetValue(),
                        Entity = inv.CustomerRef?.FullName?.GetValue(),
                        Date = inv.TxnDate?.GetValue().ToString("yyyy-MM-dd"),
                        Amount = inv.Subtotal?.GetValue()
                    });
                }
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SearchTransactions query failed: {ex.Message}"); }

        // Bills
        try
        {
            var result = qb.SendRequest(req =>
            {
                var q = req.AppendBillQueryRq();
                q.MaxReturned.SetValue(maxReturned);
                if (parsedFrom.HasValue || parsedTo.HasValue)
                {
                    if (parsedFrom.HasValue)
                        q.ORBillQuery.BillFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.FromTxnDate.SetValue(parsedFrom.Value);
                    if (parsedTo.HasValue)
                        q.ORBillQuery.BillFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.ToTxnDate.SetValue(parsedTo.Value);
                }
                if (!string.IsNullOrEmpty(refNumber))
                    q.ORBillQuery.BillFilter.RefNumberList.Add(refNumber);
            });

            if (result.StatusCode == 0 && result.Detail is IBillRetList billList)
            {
                for (int i = 0; i < billList.Count; i++)
                {
                    var bill = billList.GetAt(i);
                    allResults.Add(new
                    {
                        Type = "Bill",
                        TxnID = bill.TxnID?.GetValue(),
                        RefNumber = bill.RefNumber?.GetValue(),
                        Entity = bill.VendorRef?.FullName?.GetValue(),
                        Date = bill.TxnDate?.GetValue().ToString("yyyy-MM-dd"),
                        Amount = bill.AmountDue?.GetValue()
                    });
                }
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SearchTransactions query failed: {ex.Message}"); }

        // Checks
        try
        {
            var result = qb.SendRequest(req =>
            {
                var q = req.AppendCheckQueryRq();
                q.MaxReturned.SetValue(maxReturned);
                if (parsedFrom.HasValue || parsedTo.HasValue)
                {
                    if (parsedFrom.HasValue)
                        q.ORCheckQuery.CheckFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.FromTxnDate.SetValue(parsedFrom.Value);
                    if (parsedTo.HasValue)
                        q.ORCheckQuery.CheckFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.ToTxnDate.SetValue(parsedTo.Value);
                }
                if (!string.IsNullOrEmpty(refNumber))
                    q.ORCheckQuery.CheckFilter.RefNumberList.Add(refNumber);
            });

            if (result.StatusCode == 0 && result.Detail is ICheckRetList checkList)
            {
                for (int i = 0; i < checkList.Count; i++)
                {
                    var chk = checkList.GetAt(i);
                    allResults.Add(new
                    {
                        Type = "Check",
                        TxnID = chk.TxnID?.GetValue(),
                        RefNumber = chk.RefNumber?.GetValue(),
                        Entity = chk.PayeeEntityRef?.FullName?.GetValue(),
                        Date = chk.TxnDate?.GetValue().ToString("yyyy-MM-dd"),
                        Amount = chk.Amount?.GetValue()
                    });
                }
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SearchTransactions query failed: {ex.Message}"); }

        // SalesReceipts
        try
        {
            var result = qb.SendRequest(req =>
            {
                var q = req.AppendSalesReceiptQueryRq();
                q.MaxReturned.SetValue(maxReturned);
                if (parsedFrom.HasValue || parsedTo.HasValue)
                {
                    if (parsedFrom.HasValue)
                        q.ORSalesReceiptQuery.SalesReceiptFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.FromTxnDate.SetValue(parsedFrom.Value);
                    if (parsedTo.HasValue)
                        q.ORSalesReceiptQuery.SalesReceiptFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateRange.ToTxnDate.SetValue(parsedTo.Value);
                }
                if (!string.IsNullOrEmpty(refNumber))
                    q.ORSalesReceiptQuery.SalesReceiptFilter.RefNumberList.Add(refNumber);
            });

            if (result.StatusCode == 0 && result.Detail is ISalesReceiptRetList srList)
            {
                for (int i = 0; i < srList.Count; i++)
                {
                    var sr = srList.GetAt(i);
                    allResults.Add(new
                    {
                        Type = "SalesReceipt",
                        TxnID = sr.TxnID?.GetValue(),
                        RefNumber = sr.RefNumber?.GetValue(),
                        Entity = sr.CustomerRef?.FullName?.GetValue(),
                        Date = sr.TxnDate?.GetValue().ToString("yyyy-MM-dd"),
                        Amount = sr.Subtotal?.GetValue()
                    });
                }
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"SearchTransactions query failed: {ex.Message}"); }

        if (allResults.Count == 0)
            return "No transactions found.";

        return JsonSerializer.Serialize(allResults, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Get a financial summary for the client: accounts receivable, accounts payable, and overdue invoice count.")]
    public static string GetClientSummary(QuickBooksService qb)
    {
        var today = DateTime.Today;
        double arTotal = 0;
        int overdueCount = 0;
        double apTotal = 0;

        // Unpaid invoices — sum BalanceRemaining, count overdue
        var invoiceResult = qb.SendRequest(req =>
        {
            var q = req.AppendInvoiceQueryRq();
            q.PaidStatus.SetValue(ENPaidStatus.psNotPaidOnly);
            q.MaxReturned.SetValue(999);
        });

        if (invoiceResult.StatusCode == 0 && invoiceResult.Detail is IInvoiceRetList invoiceList)
        {
            for (int i = 0; i < invoiceList.Count; i++)
            {
                var inv = invoiceList.GetAt(i);
                var balance = inv.BalanceRemaining?.GetValue() ?? 0;
                arTotal += balance;
                var dueDate = inv.DueDate?.GetValue();
                if (dueDate.HasValue && dueDate.Value < today)
                    overdueCount++;
            }
        }

        // Unpaid bills — sum AmountDue
        var billResult = qb.SendRequest(req =>
        {
            var q = req.AppendBillQueryRq();
            q.PaidStatus.SetValue(ENPaidStatus.psNotPaidOnly);
            q.MaxReturned.SetValue(999);
        });

        if (billResult.StatusCode == 0 && billResult.Detail is IBillRetList billList)
        {
            for (int i = 0; i < billList.Count; i++)
            {
                var bill = billList.GetAt(i);
                apTotal += bill.AmountDue?.GetValue() ?? 0;
            }
        }

        return JsonSerializer.Serialize(new
        {
            AccountsReceivable = arTotal,
            AccountsPayable = apTotal,
            OverdueInvoices = overdueCount,
            CompanyFile = qb.ActiveCompanyFile
        }, QuickBooksService.JsonOptions);
    }

    private static ENTxnVoidType ParseTxnVoidType(string type) => type.ToLowerInvariant() switch
    {
        "invoice" => ENTxnVoidType.tvtInvoice,
        "bill" => ENTxnVoidType.tvtBill,
        "check" => ENTxnVoidType.tvtCheck,
        "salesreceipt" => ENTxnVoidType.tvtSalesReceipt,
        "creditmemo" => ENTxnVoidType.tvtCreditMemo,
        "journalentry" => ENTxnVoidType.tvtJournalEntry,
        "billpaymentcheck" => ENTxnVoidType.tvtBillPaymentCheck,
        "receivepayment" => ENTxnVoidType.tvtReceivePayment,
        _ => throw new ArgumentException($"Unknown transaction type '{type}'. Valid types: Invoice, Bill, Check, SalesReceipt, CreditMemo, JournalEntry, BillPaymentCheck, ReceivePayment.")
    };

    private static ENTxnDelType ParseTxnDelType(string type) => type.ToLowerInvariant() switch
    {
        "invoice" => ENTxnDelType.tdtInvoice,
        "bill" => ENTxnDelType.tdtBill,
        "check" => ENTxnDelType.tdtCheck,
        "salesreceipt" => ENTxnDelType.tdtSalesReceipt,
        "creditmemo" => ENTxnDelType.tdtCreditMemo,
        "journalentry" => ENTxnDelType.tdtJournalEntry,
        "billpaymentcheck" => ENTxnDelType.tdtBillPaymentCheck,
        "receivepayment" => ENTxnDelType.tdtReceivePayment,
        _ => throw new ArgumentException($"Unknown transaction type '{type}'. Valid types: Invoice, Bill, Check, SalesReceipt, CreditMemo, JournalEntry, BillPaymentCheck, ReceivePayment.")
    };
}
