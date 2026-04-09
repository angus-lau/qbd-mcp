using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBFC16Lib;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class ReportTools
{
    [McpServerTool, Description("Get a Trial Balance report from QuickBooks Desktop.")]
    public static string GetTrialBalance(
        QuickBooksService qb,
        [Description("Start date in YYYY-MM-DD format")] string fromDate,
        [Description("End date in YYYY-MM-DD format")] string toDate)
    {
        if (!DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            return "Error: Invalid fromDate format. Use YYYY-MM-DD.";
        if (!DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            return "Error: Invalid toDate format. Use YYYY-MM-DD.";

        var result = qb.SendRequest(req =>
        {
            var query = req.AppendGeneralSummaryReportQueryRq();
            query.GeneralSummaryReportType.SetValue(ENGeneralSummaryReportType.gsrtTrialBalance);
            query.ORReportPeriod.ReportPeriod.FromReportDate.SetValue(parsedFrom);
            query.ORReportPeriod.ReportPeriod.ToReportDate.SetValue(parsedTo);
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        return ParseReportResponse(result);
    }

    [McpServerTool, Description("Get a General Ledger report from QuickBooks Desktop.")]
    public static string GetGeneralLedger(
        QuickBooksService qb,
        [Description("Start date in YYYY-MM-DD format")] string fromDate,
        [Description("End date in YYYY-MM-DD format")] string toDate)
    {
        if (!DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            return "Error: Invalid fromDate format. Use YYYY-MM-DD.";
        if (!DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            return "Error: Invalid toDate format. Use YYYY-MM-DD.";

        var result = qb.SendRequest(req =>
        {
            var query = req.AppendGeneralDetailReportQueryRq();
            query.GeneralDetailReportType.SetValue(ENGeneralDetailReportType.gdrtGeneralLedger);
            query.ORReportPeriod.ReportPeriod.FromReportDate.SetValue(parsedFrom);
            query.ORReportPeriod.ReportPeriod.ToReportDate.SetValue(parsedTo);
        });

        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        return ParseReportResponse(result);
    }

    private static string ParseReportResponse(IResponse result)
    {
        var reportRet = (IReportRet)result.Detail;

        if (reportRet.ReportData?.ORReportDataList == null)
            return "No report data.";

        var rows = new List<Dictionary<string, string?>>();

        for (int i = 0; i < reportRet.ReportData.ORReportDataList.Count; i++)
        {
            var orData = reportRet.ReportData.ORReportDataList.GetAt(i);
            if (orData.DataRow == null)
                continue;

            var row = new Dictionary<string, string?>();
            var dataRow = orData.DataRow;

            for (int j = 0; j < dataRow.ColDataList.Count; j++)
            {
                var colData = dataRow.ColDataList.GetAt(j);
                string key;
                if (reportRet.ColDescs != null && j < reportRet.ColDescs.Count)
                {
                    var colDesc = reportRet.ColDescs.GetAt(j);
                    key = colDesc.ColTitle?.GetValue() ?? $"Col{j}";
                }
                else
                {
                    key = $"Col{j}";
                }
                row[key] = colData.Value?.GetValue();
            }

            rows.Add(row);
        }

        return JsonSerializer.Serialize(rows, QuickBooksService.JsonOptions);
    }
}
