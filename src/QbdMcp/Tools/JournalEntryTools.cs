using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using Interop.QBFC17;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class JournalEntryTools
{
    [McpServerTool, Description("Create a general journal entry in QuickBooks Desktop.")]
    public static string CreateJournalEntry(
        QuickBooksService qb,
        [Description("Journal entry date in YYYY-MM-DD format")] string date,
        [Description("Journal lines as JSON array: [{\"accountName\": \"...\", \"amount\": 100.00, \"type\": \"debit\", \"memo\": \"...\"}]")] string linesJson,
        [Description("Optional reference number")] string? refNumber = null)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return "Error: Invalid date format. Use YYYY-MM-DD.";

        var lines = JsonSerializer.Deserialize<List<JournalLine>>(linesJson, QuickBooksService.JsonInputOptions);
        if (lines == null || lines.Count == 0)
            return "Error: At least one journal line is required.";

        double totalDebit = 0;
        double totalCredit = 0;
        foreach (var line in lines)
        {
            if (line.Type.Equals("debit", StringComparison.OrdinalIgnoreCase))
                totalDebit += line.Amount;
            else if (line.Type.Equals("credit", StringComparison.OrdinalIgnoreCase))
                totalCredit += line.Amount;
        }

        if (Math.Abs(totalDebit - totalCredit) > 0.005)
            return $"Error: Debits ({totalDebit:F2}) and credits ({totalCredit:F2}) must balance.";

        var result = qb.SendRequest(req =>
        {
            var je = req.AppendJournalEntryAddRq();
            je.TxnDate.SetValue(parsedDate);

            if (!string.IsNullOrEmpty(refNumber))
                je.RefNumber.SetValue(refNumber);

            foreach (var line in lines)
            {
                if (line.Type.Equals("debit", StringComparison.OrdinalIgnoreCase))
                {
                    var debitLine = je.ORJournalLineList.Append().JournalDebitLine;
                    debitLine.AccountRef.FullName.SetValue(line.AccountName);
                    debitLine.Amount.SetValue(line.Amount);
                    if (!string.IsNullOrEmpty(line.Memo))
                        debitLine.Memo.SetValue(line.Memo);
                }
                else if (line.Type.Equals("credit", StringComparison.OrdinalIgnoreCase))
                {
                    var creditLine = je.ORJournalLineList.Append().JournalCreditLine;
                    creditLine.AccountRef.FullName.SetValue(line.AccountName);
                    creditLine.Amount.SetValue(line.Amount);
                    if (!string.IsNullOrEmpty(line.Memo))
                        creditLine.Memo.SetValue(line.Memo);
                }
            }
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        var jeRet = (IJournalEntryRet)result.Detail;
        return JsonSerializer.Serialize(new
        {
            Status = "Created",
            TxnID = jeRet.TxnID.GetValue(),
            Date = jeRet.TxnDate.GetValue().ToString("yyyy-MM-dd"),
            RefNumber = jeRet.RefNumber?.GetValue()
        }, QuickBooksService.JsonOptions);
    }

    private class JournalLine
    {
        public string AccountName { get; set; } = "";
        public double Amount { get; set; }
        public string Type { get; set; } = "debit";
        public string? Memo { get; set; }
    }
}
