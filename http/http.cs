using System.Net;
using System.Text;
using System.Text.Json;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class HttpSupport {

    private HttpListener? Listener = null;
    private CancellationTokenSource? cts = null;

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
                Library.PrintConsole("Request from: " + context.Request.RemoteEndPoint?.Address);
                ProcessRequest(context);
            }
            catch (Exception ex)
            {
                Library.PrintConsole("Error receiving request: " + ex.Message);
            }
        }
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
            var errorString = "{\"error\": \"" + ex.Message + "\"}";
            SendResponse(context, errorString);
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


    public void StopHttpListener()
    {   
        Listener?.Stop();
        Listener?.Close();

        cts?.Cancel();
        
        Library.PrintConsole("HTTP server stopped.");
    }
}