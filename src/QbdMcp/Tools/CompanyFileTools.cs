using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class CompanyFileTools
{
    [McpServerTool, Description("List all configured QuickBooks company files and show which one is currently active.")]
    public static string ListCompanyFiles(
        ConfigService config,
        QuickBooksService qb)
    {
        var companyFiles = config.GetCompanyFiles();
        var result = companyFiles.Select(cf => new
        {
            cf.Name,
            cf.Path,
            IsActive = cf.Path.Equals(qb.ActiveCompanyFile, StringComparison.OrdinalIgnoreCase)
                       || (string.IsNullOrEmpty(qb.ActiveCompanyFile) && string.IsNullOrEmpty(cf.Path))
        }).ToList();

        return JsonSerializer.Serialize(result, QuickBooksService.JsonOptions);
    }

    [McpServerTool, Description("Switch to a different QuickBooks company file by name. Use ListCompanyFiles to see available options.")]
    public static string SwitchCompanyFile(
        ConfigService config,
        QuickBooksService qb,
        [Description("Name of the company file to switch to (as configured in config.json)")] string companyName)
    {
        var companyFile = config.FindCompanyFile(companyName);
        if (companyFile == null)
        {
            var available = config.GetCompanyFiles().Select(cf => cf.Name);
            return JsonSerializer.Serialize(new
            {
                Success = false,
                Error = $"Company file '{companyName}' not found in config.",
                AvailableFiles = available
            }, QuickBooksService.JsonOptions);
        }

        try
        {
            qb.SwitchCompanyFile(companyFile.Path);
            return JsonSerializer.Serialize(new
            {
                Success = true,
                Message = $"Switched to company file: {companyFile.Name}",
                Path = companyFile.Path
            }, QuickBooksService.JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Success = false,
                Error = $"Failed to switch company file: {ex.Message}"
            }, QuickBooksService.JsonOptions);
        }
    }
}
