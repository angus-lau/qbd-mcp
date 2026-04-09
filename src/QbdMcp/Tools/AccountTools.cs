using System.ComponentModel;
using ModelContextProtocol.Server;
using QBFC16Lib;
using QbdMcp.Services;

namespace QbdMcp.Tools;

[McpServerToolType]
public static class AccountTools
{
    [McpServerTool, Description("List accounts (chart of accounts) from QuickBooks Desktop.")]
    public static string ListAccounts(
        QuickBooksService qb,
        [Description("Maximum number of accounts to return")] int maxReturned = 50)
    {
        return qb.SendQuery<IAccountRetList>(
            req => req.AppendAccountQueryRq().MaxReturned.SetValue(maxReturned),
            accounts =>
            {
                var list = new List<object>();
                for (int i = 0; i < accounts.Count; i++)
                {
                    var acct = accounts.GetAt(i);
                    list.Add(new
                    {
                        Name = acct.Name?.GetValue(),
                        FullName = acct.FullName?.GetValue(),
                        Type = acct.AccountType?.GetValue().ToString(),
                        Balance = acct.Balance?.GetValue(),
                        Description = acct.Desc?.GetValue()
                    });
                }
                return list;
            });
    }

    [McpServerTool, Description("Get the balance of a specific account in QuickBooks Desktop.")]
    public static string GetAccountBalance(
        QuickBooksService qb,
        [Description("Account name to look up")] string accountName)
    {
        return qb.SendQuery<IAccountRetList>(
            req =>
            {
                var query = req.AppendAccountQueryRq();
                var nameFilter = query.ORAccountListQuery.AccountListFilter.ORNameFilter.NameFilter;
                nameFilter.Name.SetValue(accountName);
                nameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
            },
            accounts =>
            {
                var list = new List<object>();
                for (int i = 0; i < accounts.Count; i++)
                {
                    var acct = accounts.GetAt(i);
                    list.Add(new
                    {
                        Name = acct.Name?.GetValue(),
                        FullName = acct.FullName?.GetValue(),
                        Type = acct.AccountType?.GetValue().ToString(),
                        Balance = acct.Balance?.GetValue()
                    });
                }
                return list;
            });
    }
}
