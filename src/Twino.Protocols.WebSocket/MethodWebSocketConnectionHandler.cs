using System.Threading.Tasks;
using Twino.Core;
using Twino.Core.Protocols;

namespace Twino.Protocols.WebSocket
{
    public delegate Task WebSocketMessageRecievedHandler(WsServerSocket socket, WebSocketMessage message);

    public delegate Task WebSocketConnectedHandler(WsServerSocket socket, ConnectionData data);

    /// <summary>
    /// Twino WebSocket Server with WebSocketMessageRecievedHandler method implementation handler
    /// </summary>
    internal class MethodWebSocketConnectionHandler : IProtocolConnectionHandler<WebSocketMessage>
    {
        /// <summary>
        /// User defined action
        /// </summary>
        private readonly WebSocketMessageRecievedHandler _messageHandler;

        private readonly WebSocketConnectedHandler _connectedHandler;

        public MethodWebSocketConnectionHandler(WebSocketMessageRecievedHandler handler)
            : this(null, handler)
        {
        }

        public MethodWebSocketConnectionHandler(WebSocketConnectedHandler connectedHandler, WebSocketMessageRecievedHandler messageHandler)
        {
            _connectedHandler = connectedHandler;
            _messageHandler = messageHandler;
        }

        /// <summary>
        /// Triggered when a websocket client is connected. 
        /// </summary>
        public async Task<SocketBase> Connected(ITwinoServer server, IConnectionInfo connection, ConnectionData data)
        {
            WsServerSocket socket = new WsServerSocket(server, connection);

            if (_connectedHandler != null)
                await _connectedHandler(socket, data);

            return socket;
        }

        /// <summary>
        /// Triggered when handshake is completed and the connection is ready to communicate 
        /// </summary>
        public async Task Ready(ITwinoServer server, SocketBase client)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when a client sends a message to the server 
        /// </summary>
        public async Task Received(ITwinoServer server, IConnectionInfo info, SocketBase client, WebSocketMessage message)
        {
            await _messageHandler((WsServerSocket) client, message);
        }

        /// <summary>
        /// Triggered when a websocket client is disconnected. 
        /// </summary>
        public async Task Disconnected(ITwinoServer server, SocketBase client)
        {
            await Task.CompletedTask;
        }
    }
}