using System.Text.Json;
using QbdMcp.Models;

namespace QbdMcp.Services;

public class ConfigService
{
    private readonly AppConfig _config;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ConfigService()
    {
        var configDir = AppContext.BaseDirectory;
        var configPath = Path.Combine(configDir, "config.json");

        if (!File.Exists(configPath))
        {
            _config = new AppConfig
            {
                CompanyFiles = new List<CompanyFile>
                {
                    new CompanyFile
                    {
                        Name = "Example Company",
                        Path = @"C:\Users\Public\Documents\Intuit\QuickBooks\Company Files\Example.qbw"
                    }
                }
            };

            File.WriteAllText(configPath, JsonSerializer.Serialize(_config, _jsonOptions));
        }
        else
        {
            var json = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<AppConfig>(json, QuickBooksService.JsonInputOptions) ?? new AppConfig();
        }
    }

    public List<CompanyFile> GetCompanyFiles() => _config.CompanyFiles;

    public CompanyFile? FindCompanyFile(string name)
    {
        return _config.CompanyFiles.FirstOrDefault(
            cf => cf.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
