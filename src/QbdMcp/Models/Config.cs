namespace QbdMcp.Models;

public class AppConfig
{
    public List<CompanyFile> CompanyFiles { get; set; } = new();
}

public class CompanyFile
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}
