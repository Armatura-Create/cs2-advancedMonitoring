using System.Net;
using System.Text;
using System.Text.Json;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class HttpSupport {

    private HttpListener? Listener = null;
    private CancellationTokenSource? cts = null;

    private readonly Dictionary<string, DateTime> RequestTimestamps = [];
    private readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(5); // Интервал в секундах между запросами

    public void StartHttpListener(int port, string endpoint) {
        if (!HttpListener.IsSupported) 
        {
            Console.WriteLine("HTTP listener is not supported on this platform.");
            return;
        }

        Listener?.Stop();
        Listener?.Close();
        cts?.Cancel();

        cts = new CancellationTokenSource();

        Library.PrintConsole($"Starting HTTP server http://*:{port}/{endpoint}/");

        Listener = new HttpListener();
        Listener.Prefixes.Add($"http://*:{port}/{endpoint}/");
        Listener.Start();

        Task.Run(() => ListenForRequests(cts.Token));
        
        Library.PrintConsole("HTTP server started.");
    }

    private async Task ListenForRequests(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (Listener == null)
            {
                Library.PrintConsole("Listener is null.");
                return;
            }
             try
            {
                var context = await Listener.GetContextAsync();
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

                    RequestTimestamps.TryGetValue(clientIp!, out DateTime lastRequestTime);

                    SendError(context, HttpStatusCode.TooManyRequests, $"Too Many Requests - Please wait {RequestInterval.TotalSeconds - (DateTime.UtcNow - lastRequestTime).TotalSeconds} seconds.");
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

        if (RequestTimestamps.TryGetValue(clientIp, out DateTime lastRequestTime))
        {
            if (DateTime.UtcNow - lastRequestTime < RequestInterval)
            {
                return false;
            }
        }

        RequestTimestamps[clientIp] = DateTime.UtcNow;
        return true;
    }


    public void StopHttpListener()
    {   
        Listener?.Stop();
        Listener?.Close();

        cts?.Cancel();
        
        Library.PrintConsole("HTTP server stopped.");
    }
}