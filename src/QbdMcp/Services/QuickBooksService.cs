using System.Runtime.InteropServices;
using System.Text.Json;
using QBFC16Lib;

namespace QbdMcp.Services;

public class QuickBooksService : IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static readonly JsonSerializerOptions JsonInputOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly object _lock = new();
    private QBSessionManager? _sessionManager;
    private string _activeCompanyFile = "";

    public string ActiveCompanyFile => _activeCompanyFile;

    private void EnsureConnected()
    {
        if (_sessionManager != null) return;

        _sessionManager = new QBSessionManager();
        _sessionManager.OpenConnection("QbdMcp", "QuickBooks Desktop MCP Server");
        _sessionManager.BeginSession(_activeCompanyFile, ENOpenMode.omDontCare);
    }

    public void SwitchCompanyFile(string companyFilePath)
    {
        lock (_lock)
        {
            DisconnectInternal();
            _activeCompanyFile = companyFilePath;
            EnsureConnected();
        }
    }

    public static bool TryParseDate(string input, out DateTime result, out string error)
    {
        error = "";
        if (DateTime.TryParseExact(input, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            return true;
        error = $"Error: Invalid date format '{input}'. Use YYYY-MM-DD.";
        return false;
    }

    public IResponse SendRequest(Action<IMsgSetRequest> buildRequest)
    {
        lock (_lock)
        {
            EnsureConnected();

            try
            {
                var requestSet = _sessionManager!.CreateMsgSetRequest("US", 16, 0);
                buildRequest(requestSet);
                var response = _sessionManager.DoRequests(requestSet);
                return response.ResponseList.GetAt(0);
            }
            catch (COMException)
            {
                _sessionManager = null;
                throw;
            }
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

    private void DisconnectInternal()
    {
        if (_sessionManager == null) return;

        _sessionManager.EndSession();
        _sessionManager.CloseConnection();
        _sessionManager = null;
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            DisconnectInternal();
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
