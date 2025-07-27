using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


var app = builder.Build();
app.UseWebSockets(); // required.

var clients = new ConcurrentDictionary<Guid, WebSocket>();  // Keep track of all connected clients.



// Server Receive:
app.Use(async (context, next) =>  
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            // Upgrade TCP connection from an HTTP connection to the WebSocket protocol.
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Register connected client
            var id = Guid.NewGuid();
            clients.TryAdd(id, webSocket); 


            byte[] buffer = new byte[1024 * 4];
            var ReceiveAsync = async () => await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            WebSocketReceiveResult receiveResult = await ReceiveAsync();
            while (!receiveResult.CloseStatus.HasValue)
            {
                //receiveResult.Count                  // The length in bytes of the data received this time.
                //receiveResult.EndOfMessage           // bool: is received competely?
                //receiveResult.MessageType            // enum: Text | Binary | Close
                //receiveResult.CloseStatus            // enum: If not null, it indicates that the client has requested to close the connection.
                //receiveResult.CloseStatusDescription // string

                string receivedText = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                Console.WriteLine($"Received: {receivedText}");

                receiveResult = await ReceiveAsync();
            }

            // Unregister client
            clients.TryRemove(new KeyValuePair<Guid, WebSocket>(id, webSocket));
            // Close connection
            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

// Server Send:
var timer = new Timer(async _ =>
{
    var msg = $"Server broadcast at {DateTime.Now:HH:mm:ss}";
    var buffer = Encoding.UTF8.GetBytes(msg);
    var tasks = clients.Values
        .Where(ws => ws.State != WebSocketState.Closed)
        .Select(ws =>
            ws.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None)
        );
    await Task.WhenAll(tasks);
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));


app.Run();
