namespace QbdMcp.Models;

public class ExpenseLine
{
    public string AccountName { get; set; } = "";
    public double Amount { get; set; }
    public string? Description { get; set; }
}

public class SalesLineItem
{
    public string? ItemName { get; set; }
    public string? Description { get; set; }
    public double? Rate { get; set; }
    public double? Quantity { get; set; }
}
