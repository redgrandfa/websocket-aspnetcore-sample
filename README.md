# Implement WebSockets in ASP.NET Core 8

## Client JS
```js
// const ws = new WebSocket("ws://localhost:/ws"); 
const ws = new WebSocket("wss://localhost:7024/ws"); // wss is secure connection
ws.onopen = () => { ... }
ws.onmessage = (e) => { ... } // e.data := received from server
ws.send( ... )  
ws.onclose = () => { ... }

```

## Server 
### Server Receive:
```csharp
if (context.WebSockets.IsWebSocketRequest){
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

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


    // Close connection
    await webSocket.CloseAsync(
        receiveResult.CloseStatus.Value,
        receiveResult.CloseStatusDescription,
        CancellationToken.None);
}

```

### Server Send:
```csharp
WebSocket ws = ...;
ws.SendAsync( new ArraySegment<byte>(buffer),
    WebSocketMessageType.Text,
    endOfMessage: true,
    CancellationToken.None);
```

## About CORS
- The WebSocket protocol is upgraded from HTTP.
- After the upgrade, CORS does not apply to the WebSocket connection.


---
## Usage
1. Run Server Project
2. Open `client.html` in a browser.
3. Server sends messages periodically (by timer) and displays them on the web page.
4. Client sends a message manually and sees it on the server console.

