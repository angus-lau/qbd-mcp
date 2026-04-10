using Interop.QBFC17;
using QbdMcp.Models;

namespace QbdMcp.Services;

public class NameResolver
{
    private readonly QuickBooksService _qb;

    public NameResolver(QuickBooksService qb)
    {
        _qb = qb;
    }

    public ResolveResult ResolveVendor(string input)
    {
        var names = new List<string>();

        var result = _qb.SendRequest(req =>
        {
            var query = req.AppendVendorQueryRq();
            var filter = query.ORVendorListQuery.VendorListFilter.ORNameFilter.NameFilter;
            filter.Name.SetValue(input);
            filter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
        });

        if (result.StatusCode == 0 && result.Detail is IVendorRetList vendors)
        {
            for (int i = 0; i < vendors.Count; i++)
                names.Add(vendors.GetAt(i).Name.GetValue());
        }

        return Resolve(input, names);
    }

    public ResolveResult ResolveCustomer(string input)
    {
        var names = new List<string>();

        var result = _qb.SendRequest(req =>
        {
            var query = req.AppendCustomerQueryRq();
            var filter = query.ORCustomerListQuery.CustomerListFilter.ORNameFilter.NameFilter;
            filter.Name.SetValue(input);
            filter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
        });

        if (result.StatusCode == 0 && result.Detail is ICustomerRetList customers)
        {
            for (int i = 0; i < customers.Count; i++)
                names.Add(customers.GetAt(i).Name.GetValue());
        }

        return Resolve(input, names);
    }

    public ResolveResult ResolveAccount(string input)
    {
        var names = new List<string>();

        var result = _qb.SendRequest(req =>
        {
            var query = req.AppendAccountQueryRq();
            var filter = query.ORAccountListQuery.AccountListFilter.ORNameFilter.NameFilter;
            filter.Name.SetValue(input);
            filter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
        });

        if (result.StatusCode == 0 && result.Detail is IAccountRetList accounts)
        {
            for (int i = 0; i < accounts.Count; i++)
                names.Add(accounts.GetAt(i).FullName.GetValue());
        }

        return Resolve(input, names);
    }

    internal static ResolveResult Resolve(string input, List<string> candidates)
    {
        if (candidates.Count == 0)
            return ResolveResult.NotFound(input, candidates);

        // Exact match (case-insensitive)
        var exact = candidates.FirstOrDefault(c => c.Equals(input, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return ResolveResult.Found(exact);

        // Single partial match
        if (candidates.Count == 1)
            return ResolveResult.Found(candidates[0]);

        // Multiple matches — ambiguous
        return ResolveResult.Ambiguous(input, candidates);
    }
}
