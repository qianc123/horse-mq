﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Twino.Server.Http;
using Twino.Server.WebSockets;
using Timer = System.Timers.Timer;

namespace Twino.Server
{
    /// <summary>
    /// Event handler delegate for general purpose of HttpServer
    /// </summary>
    public delegate void TwinoServerEventHandler(TwinoServer server);

    public delegate void TwinoServerClientEventHandler(TwinoServer server, ServerSocket client);

    /// <summary>
    /// HttpServer of Twino.Server Library.
    /// Listens all HTTP Connection Requests and Manages them.
    /// Handshakes with requsts and figures out if they are HTTP Request or WebSocket Request.
    /// </summary>
    public class TwinoServer
    {
        #region Properties

        internal Pinger Pinger { get; private set; }

        /// <summary>
        /// Server Request Handler for all HTTP Requests
        /// </summary>
        public IHttpRequestHandler RequestHandler { get; }

        /// <summary>
        /// Default WebSocket Client container for HttpServer.
        /// If it isn't null, clients are added when connected and removed after disconnected
        /// </summary>
        public IClientContainer Container { get; }

        /// <summary>
        /// Client creation factory for HttpServer.
        /// HttpServer's ServerSocket client class is abstract and developers must create their own client classes derived from the ServerSocket class.
        /// And they have to create instance of their custom classes in the class which implements IClientFactory interface.
        /// </summary>
        public IClientFactory ClientFactory { get; }

        /// <summary>
        /// Logger class for HttpServer operations.
        /// This Logger can hurt performance when Enabled.
        /// Enable only in development or maintenance mode.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Server options. Can set programmatically with constructor parameter
        /// Or can set with "rimserver.json", "server.json" or "rim.json" options filename
        /// </summary>
        public ServerOptions Options { get; }

        /// <summary>
        /// Server status, If true, server is listening for new connections
        /// </summary>
        public bool IsRunning { get; private set; }
        
        /// <summary>
        /// Current server time as RFC 1123
        /// </summary>
        public string Time { get; private set; }
        
        //creating string from DateTime object per request uses some cpu and time (1 sec full cpu for 10million times)
        /// <summary>
        /// Server time timer
        /// </summary>
        private Timer _timeTimer;
        
        /// <summary>
        /// TcpListener for HttpServer
        /// </summary>
        private List<ConnectionHandler> _handlers = new List<ConnectionHandler>();

        #endregion

        #region Events

        /// <summary>
        /// Raises when server starts to listen and accept TCP connections
        /// </summary>
        public event TwinoServerEventHandler Started;

        /// <summary>
        /// Raises when servers stops by programmatically or due to an error
        /// </summary>
        public event TwinoServerEventHandler Stopped;

        /// <summary>
        /// Triggered when a client is connected
        /// </summary>
        public event TwinoServerClientEventHandler ClientConnected;

        /// <summary>
        /// Triggered when a client is disconnected
        /// </summary>
        public event TwinoServerClientEventHandler ClientDisconnected;

        internal void SetClientConnected(ServerSocket client)
        {
            ClientConnected?.Invoke(this, client);
        }

        internal void SetClientDisconnected(ServerSocket client)
        {
            ClientDisconnected?.Invoke(this, client);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates new TwinoServer instance.
        /// </summary>
        /// <param name="clientFactory">WebSocket client factory</param>
        /// <param name="options">Server options</param>
        public TwinoServer(IClientFactory clientFactory, ServerOptions options) : this(null, clientFactory, null, null)
        {
            Options = options;
        }

        /// <summary>
        /// Creates new TwinoServer instance.
        /// </summary>
        /// <param name="requestHandler">HTTP Request handler</param>
        /// <param name="clientFactory">WebSocket client factory</param>
        public TwinoServer(IHttpRequestHandler requestHandler, IClientFactory clientFactory)
            : this(requestHandler, clientFactory, null, null)
        {
        }

        /// <summary>
        /// Creates new TwinoServer instance.
        /// </summary>
        /// <param name="requestHandler">HTTP Request handler</param>
        /// <param name="clientFactory">WebSocket client factory</param>
        /// <param name="clientContainer">Client container for online WebSocket clients</param>
        public TwinoServer(IHttpRequestHandler requestHandler,
                         IClientFactory clientFactory,
                         IClientContainer clientContainer) : this(requestHandler, clientFactory, clientContainer, null)
        {
            Options = ServerOptions.LoadFromFile();
        }

        /// <summary>
        /// Creates new TwinoServer instance.
        /// </summary>
        /// <param name="requestHandler">HTTP Request handler</param>
        /// <param name="clientFactory">WebSocket client factory</param>
        /// <param name="clientContainer">Client container for online WebSocket clients</param>
        /// <param name="options">Server options</param>
        public TwinoServer(IHttpRequestHandler requestHandler,
                         IClientFactory clientFactory,
                         IClientContainer clientContainer,
                         ServerOptions options)
        {
            RequestHandler = requestHandler;
            ClientFactory = clientFactory;
            Container = clientContainer;
            Options = options ?? ServerOptions.LoadFromFile();
        }

        #endregion

        #region Create

        /// <summary>
        /// Creates new HTTP Server, supports only HTTP Requests (WebSockets are not supported)
        /// Options are loaded from JSON file
        /// </summary>
        public static TwinoServer CreateHttp(IHttpRequestHandler requestHandler)
        {
            return new TwinoServer(requestHandler, null);
        }

        /// <summary>
        /// Creates new HTTP Server, supports only HTTP Requests (WebSockets are not supported)
        /// Options are loaded from JSON file
        /// </summary>
        public static TwinoServer CreateHttp(HttpRequestHandlerDelegate handler)
        {
            MethodHttpRequestHandler requestHandler = new MethodHttpRequestHandler(handler);
            return new TwinoServer(requestHandler, null);
        }

        /// <summary>
        /// Creates new HTTP Server, supports only HTTP Requests (WebSockets are not supported)
        /// Options must be set with method parameter
        /// </summary>
        public static TwinoServer CreateHttp(IHttpRequestHandler requestHandler, ServerOptions options)
        {
            return new TwinoServer(requestHandler, null, null, options);
        }

        /// <summary>
        /// Creates new HTTP Server, supports only HTTP Requests (WebSockets are not supported)
        /// Options must be set with method parameter
        /// </summary>
        public static TwinoServer CreateHttp(HttpRequestHandlerDelegate handler, ServerOptions options)
        {
            MethodHttpRequestHandler requestHandler = new MethodHttpRequestHandler(handler);
            return new TwinoServer(requestHandler, null, null, options);
        }

        /// <summary>
        /// Creates new WebSocket Server, supports only WS Requests (HTTP requests are not supported)
        /// Options are loaded from JSON file
        /// </summary>
        public static TwinoServer CreateWebSocket(IClientFactory clientFactory)
        {
            return new TwinoServer(null, clientFactory);
        }

        /// <summary>
        /// Creates new WebSocket Server, supports only WS Requests (HTTP requests are not supported)
        /// Options are loaded from JSON file
        /// </summary>
        public static TwinoServer CreateWebSocket(ClientFactoryHandler handler)
        {
            DefaultClientFactory factory = new DefaultClientFactory(handler);
            return new TwinoServer(null, factory);
        }

        /// <summary>
        /// Creates new WebSocket Server, supports only WS Requests (HTTP requests are not supported)
        /// Options are loaded from JSON file.
        /// IMPORTANT: Uses default ServerSocket instances.
        /// This creation operation may be useless without Twino.SocketModels package managers.
        /// </summary>
        public static TwinoServer CreateWebSocket()
        {
            DefaultClientFactory factory = new DefaultClientFactory(async (s, r, t) => await Task.FromResult(new ServerSocket(s, r, t)));
            return new TwinoServer(null, factory);
        }

        /// <summary>
        /// Creates new WebSocket Server, supports only WS Requests (HTTP requests are not supported)
        /// Options are loaded from JSON file
        /// </summary>
        public static TwinoServer CreateWebSocket(Action<ServerSocket> action)
        {
            return CreateWebSocket(async (s, r, t) =>
            {
                ServerSocket socket = new ServerSocket(s, r, t);
                action(socket);
                return await Task.FromResult(socket);
            });
        }

        /// <summary>
        /// Creates new WebSocket Server, supports only WS Requests (HTTP requests are not supported)
        /// Options must be set with method parameter
        /// </summary>
        public static TwinoServer CreateWebSocket(IClientFactory clientFactory, ServerOptions options)
        {
            return new TwinoServer(null, clientFactory, null, options);
        }

        /// <summary>
        /// Creates new WebSocket Server, supports only WS Requests (HTTP requests are not supported)
        /// Options must be set with method parameter
        /// </summary>
        public static TwinoServer CreateWebSocket(ClientFactoryHandler handler, ServerOptions options)
        {
            DefaultClientFactory factory = new DefaultClientFactory(handler);
            return new TwinoServer(null, factory, null, options);
        }

        #endregion

        #region Start - Stop

        public void BlockWhileRunning()
        {
            while (IsRunning)
                Thread.Sleep(100);
        }

        public void Start(int port)
        {
            Options.Hosts = new List<HostOptions>();
            HostOptions host = new HostOptions
                               {
                                   Port = port,
                                   SslEnabled = false
                               };

            Options.Hosts.Add(host);

            Start();
        }

        /// <summary>
        /// Starts server and listens new connection requests
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Stop the HttpServer before restart");

            if (Options.Hosts == null)
                throw new ArgumentNullException($"Hosts", "There is no host to listen. Add hosts to Twino Options");

            if (_timeTimer == null)
            {
                _timeTimer = new Timer(1000);
                _timeTimer.Elapsed += (sender, args) => Time = DateTime.UtcNow.ToString("R");
                _timeTimer.AutoReset = true;
                _timeTimer.Start();
            }

            IsRunning = true;
            Started?.Invoke(this);
            _handlers = new List<ConnectionHandler>();

            foreach (HostOptions host in Options.Hosts)
            {
                InnerServer server = new InnerServer();
                server.Options = host;

                if (host.SslEnabled && !string.IsNullOrEmpty(host.SslCertificate))
                {
                    server.Certificate = string.IsNullOrEmpty(host.CertificateKey)
                                             ? new X509Certificate2(host.SslCertificate)
                                             : new X509Certificate2(host.SslCertificate, host.CertificateKey);
                }

                server.Listener = new TcpListener(IPAddress.Any, host.Port);

                if (Options.MaximumPendingConnections == 0)
                    server.Listener.Start();
                else
                    server.Listener.Start(Options.MaximumPendingConnections);

                ConnectionHandler handler = new ConnectionHandler(this, server);

                server.Handle = new Thread(async () => { await handler.Handle(); });
                server.Handle.IsBackground = true;
                server.Handle.Priority = ThreadPriority.Highest;
                server.Handle.Start();

                _handlers.Add(handler);
            }

            IsRunning = true;

            if (Options.PingInterval > 0)
            {
                Pinger = new Pinger(this, TimeSpan.FromMilliseconds(Options.PingInterval));
                Pinger.Start();
            }
        }

        /// <summary>
        /// Stops accepting connections.
        /// But does not disconnect connected clients.
        /// In order to disconnect all clients, you need to do it manually
        /// You can use a ClientContainer implementation to do it easily
        /// </summary>
        public void Stop()
        {
            IsRunning = false;

            if (_timeTimer != null)
            {
                _timeTimer.Stop();
                _timeTimer.Dispose();
                _timeTimer = null;
            }

            if (Pinger != null)
            {
                Pinger.Stop();
                Pinger = null;
            }

            foreach (ConnectionHandler handler in _handlers)
                handler.Dispose();

            _handlers.Clear();
            Stopped?.Invoke(this);
        }

        #endregion
    }
}