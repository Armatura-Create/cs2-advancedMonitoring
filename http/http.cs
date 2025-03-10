using System.Net;
using System.Text;
using System.Text.Json;
using AdvancedMonitoring.dto;
using AdvancedMonitoring.library;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring.http;

public class HttpSupport {

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    private readonly Dictionary<string, DateTime> _requestTimestamps = [];
    private readonly TimeSpan _requestInterval = TimeSpan.FromSeconds(5); // Интервал в секундах между запросами

    public void StartHttpListener(string ip, int port, string endpoint) {
        if (!HttpListener.IsSupported) {
            Console.WriteLine("HTTP listener is not supported on this platform.");
            return;
        }

        _listener?.Stop();
        _listener?.Close();
        _cts?.Cancel();

        _cts = new CancellationTokenSource();

        // Сформируем строку префикса
        // Например: "http://192.168.1.10:27015/monitoring-info/"
        var prefix = $"http://{ip}:{port}/{endpoint}/";
        Library.PrintConsole($"Starting HTTP server {prefix}");

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _listener.Start();

        Task.Run(() => ListenForRequests(_cts.Token));

        Library.PrintConsole("HTTP server started.");
    }

    private async Task ListenForRequests(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_listener == null)
            {
                Library.PrintConsole("Listener is null.");
                return;
            }
            
            try
            {
                var context = await _listener.GetContextAsync();
                var clientIp = context.Request.RemoteEndPoint?.Address.ToString();

                
                
                if (IsRequestAllowed(clientIp))
                {
                    Library.PrintConsole("Request from: " + clientIp);
                    if (context.Request.HttpMethod == "GET" || context.Request.HttpMethod == "POST")
                    {
                        ProcessRequest(context);
                    }
                    else
                    {
                        SendError(context, HttpStatusCode.MethodNotAllowed, "Method Not Allowed - Only GET and POST are supported.");
                    }
                }
                else
                {
                    Library.PrintConsole("Too many requests from: " + clientIp);

                    _requestTimestamps.TryGetValue(clientIp!, out DateTime lastRequestTime);

                    SendError(context, HttpStatusCode.TooManyRequests, $"Too Many Requests - Please wait {_requestInterval.TotalSeconds - (DateTime.UtcNow - lastRequestTime).TotalSeconds} seconds.");
                }
            }
            catch (Exception ex)
            {
                Library.PrintConsole("Error receiving request: " + ex.Message);
            }
        }
    }

    private void SendError(HttpListenerContext context, HttpStatusCode errorCode, string message)
    {
        context.Response.StatusCode = (int) errorCode;
        context.Response.StatusDescription = message;
        context.Response.Close();
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            ServerDto serverData = Instance.Cache.GetCurrentServerData();

            if (!Instance.Config.ShowBots)
            {
                serverData.Players.RemoveAll(p => p.IsBot);
            }
            
            if (!Instance.Config.ShowHLTV)
            {
                serverData.Players.RemoveAll(p => p.IsHLTV);
            }

            var responseString = JsonSerializer.Serialize(serverData);
            Library.PrintConsole("Response: " + responseString);

            SendResponse(context, responseString);
        }
        catch (Exception ex)
        {
            Library.PrintConsole("Error processing request: " + ex.Message);
            SendError(context, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private void SendResponse(HttpListenerContext context, string responseString)
    {
        context.Response.ContentType = "application/json";
        context.Response.ContentEncoding = Encoding.UTF8;
        context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(responseString);

        using var output = context.Response.OutputStream;
        output.Write(Encoding.UTF8.GetBytes(responseString));
        context.Response.Close();
    }

    private bool IsRequestAllowed(string? clientIp)
    {
        if (string.IsNullOrEmpty(clientIp))
        {
            return false;
        }

        if (_requestTimestamps.TryGetValue(clientIp, out DateTime lastRequestTime))
        {
            if (DateTime.UtcNow - lastRequestTime < _requestInterval)
            {
                return false;
            }
        }

        _requestTimestamps[clientIp] = DateTime.UtcNow;
        return true;
    }


    public void StopHttpListener()
    {   
        _listener?.Stop();
        _listener?.Close();

        _cts?.Cancel();
        
        Library.PrintConsole("HTTP server stopped.");
    }
}