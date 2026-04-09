using System.ComponentModel;
using ModelContextProtocol.Server;
using Interop.QBFC17;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class VendorTools
{
    [McpServerTool, Description("List vendors from QuickBooks Desktop.")]
    public static string ListVendors(
        QuickBooksService qb,
        [Description("Maximum number of vendors to return")] int maxReturned = 25)
    {
        return qb.SendQuery<IVendorRetList>(
            req => req.AppendVendorQueryRq().ORVendorListQuery.VendorListFilter.MaxReturned.SetValue(maxReturned),
            vendors =>
            {
                var list = new List<object>();
                for (int i = 0; i < vendors.Count; i++)
                {
                    var v = vendors.GetAt(i);
                    list.Add(new
                    {
                        Name = v.Name?.GetValue(),
                        Company = v.CompanyName?.GetValue(),
                        Email = v.Email?.GetValue(),
                        Phone = v.Phone?.GetValue(),
                        Balance = v.Balance?.GetValue()
                    });
                }
                return list;
            });
    }

    [McpServerTool, Description("Search for a specific vendor by name in QuickBooks Desktop.")]
    public static string GetVendor(
        QuickBooksService qb,
        [Description("Full or partial vendor name to search for")] string name)
    {
        return qb.SendQuery<IVendorRetList>(
            req =>
            {
                var query = req.AppendVendorQueryRq();
                query.ORVendorListQuery.VendorListFilter.ORNameFilter.NameFilter.Name.SetValue(name);
                query.ORVendorListQuery.VendorListFilter.ORNameFilter.NameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
            },
            vendors =>
            {
                var list = new List<object>();
                for (int i = 0; i < vendors.Count; i++)
                {
                    var v = vendors.GetAt(i);
                    list.Add(new
                    {
                        Name = v.Name?.GetValue(),
                        Company = v.CompanyName?.GetValue(),
                        Email = v.Email?.GetValue(),
                        Phone = v.Phone?.GetValue(),
                        Balance = v.Balance?.GetValue(),
                        Address = v.VendorAddress != null ? new
                        {
                            Line1 = v.VendorAddress.Addr1?.GetValue(),
                            City = v.VendorAddress.City?.GetValue(),
                            State = v.VendorAddress.State?.GetValue(),
                            Zip = v.VendorAddress.PostalCode?.GetValue()
                        } : null
                    });
                }
                return list;
            });
    }
}
