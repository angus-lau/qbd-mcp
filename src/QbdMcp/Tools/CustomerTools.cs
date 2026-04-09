using System.ComponentModel;
using ModelContextProtocol.Server;
using QBFC16Lib;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class CustomerTools
{
    [McpServerTool, Description("List customers from QuickBooks Desktop.")]
    public static string ListCustomers(
        QuickBooksService qb,
        [Description("Maximum number of customers to return")] int maxReturned = 25)
    {
        return qb.SendQuery<ICustomerRetList>(
            req => req.AppendCustomerQueryRq().MaxReturned.SetValue(maxReturned),
            customers =>
            {
                var list = new List<object>();
                for (int i = 0; i < customers.Count; i++)
                {
                    var c = customers.GetAt(i);
                    list.Add(new
                    {
                        Name = c.Name?.GetValue(),
                        Company = c.CompanyName?.GetValue(),
                        Email = c.Email?.GetValue(),
                        Phone = c.Phone?.GetValue(),
                        Balance = c.Balance?.GetValue()
                    });
                }
                return list;
            });
    }

    [McpServerTool, Description("Search for a specific customer by name in QuickBooks Desktop.")]
    public static string GetCustomer(
        QuickBooksService qb,
        [Description("Full or partial customer name to search for")] string name)
    {
        return qb.SendQuery<ICustomerRetList>(
            req =>
            {
                var query = req.AppendCustomerQueryRq();
                query.ORCustomerListQuery.CustomerListFilter.ORNameFilter.NameFilter.Name.SetValue(name);
                query.ORCustomerListQuery.CustomerListFilter.ORNameFilter.NameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
            },
            customers =>
            {
                var list = new List<object>();
                for (int i = 0; i < customers.Count; i++)
                {
                    var c = customers.GetAt(i);
                    list.Add(new
                    {
                        Name = c.Name?.GetValue(),
                        Company = c.CompanyName?.GetValue(),
                        Email = c.Email?.GetValue(),
                        Phone = c.Phone?.GetValue(),
                        Balance = c.Balance?.GetValue(),
                        Address = c.BillAddress != null ? new
                        {
                            Line1 = c.BillAddress.Addr1?.GetValue(),
                            City = c.BillAddress.City?.GetValue(),
                            State = c.BillAddress.State?.GetValue(),
                            Zip = c.BillAddress.PostalCode?.GetValue()
                        } : null
                    });
                }
                return list;
            });
    }
}
