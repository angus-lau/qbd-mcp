using System.Text.Json;
using QBFC13Lib;

namespace QbdMcp.Services;

public class QuickBooksService : IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static readonly JsonSerializerOptions JsonInputOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly object _lock = new();
    private QBSessionManager? _sessionManager;

    private void EnsureConnected()
    {
        if (_sessionManager != null) return;

        _sessionManager = new QBSessionManager();
        _sessionManager.OpenConnection("QbdMcp", "QuickBooks Desktop MCP Server");
        _sessionManager.BeginSession("", ENOpenMode.omDontCare);
    }

    public string ActiveCompanyFile
    {
        get
        {
            lock (_lock)
            {
                EnsureConnected();
                return _sessionManager!.ActiveCompanyFileName;
            }
        }
    }

    public IResponse SendRequest(Action<IMsgSetRequest> buildRequest)
    {
        lock (_lock)
        {
            EnsureConnected();

            var requestSet = _sessionManager!.CreateMsgSetRequest("US", 13, 0);
            buildRequest(requestSet);
            var response = _sessionManager.DoRequests(requestSet);
            return response.ResponseList.GetAt(0);
        }
    }

    public string SendQuery<T>(Action<IMsgSetRequest> buildRequest, Func<T, List<object>> mapResults) where T : class
    {
        var result = SendRequest(buildRequest);
        if (result.StatusCode != 0)
            return $"Error: {result.StatusMessage}";

        if (result.Detail == null)
            return "No results found.";

        var detail = (T)result.Detail;
        var items = mapResults(detail);
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            if (_sessionManager == null) return;

            _sessionManager.EndSession();
            _sessionManager.CloseConnection();
            _sessionManager = null;
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
