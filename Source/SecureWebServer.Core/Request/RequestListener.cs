using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using SecureWebServer.Core.Error;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Core.Request
{
    /// <summary>
    /// Listens for new requests on a separate thread and handles each new request on it's own thread.
    /// </summary>
    public class RequestListener
    {
        private readonly ILog _log;
        private readonly IRequestHandler _requestHandler;
        private readonly IErrorHandler _errorHandler;
        private Socket _listenerSocket;
        private State _state;

        /// <summary>
        /// Creates a new <see cref="RequestListener"/> instance.
        /// </summary>
        /// <param name="requestHandler">The handler that should handle a request and return a proper response.</param>
        /// <param name="errorHandler">The handler to use when an error occurs and a proper error response should be created.</param>
        public RequestListener(IRequestHandler requestHandler, IErrorHandler errorHandler)
        {
            _log = LogManager.GetLogger(GetType());
            _requestHandler = requestHandler;
            _errorHandler = errorHandler;
            _state = State.Stopped;
        }

        /// <summary>
        /// Starts listening for new requests for the specified port.
        /// </summary>
        public void Start(int port)
        {
            if (_state != State.Stopped)
                return;

            _state = State.Starting;

            // Listen for any IP address using the specified port
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            _listenerSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenerSocket.Bind(endPoint);

            // Listening occurs on a separate thread
            Thread listenerThread = new Thread(Listen);
            listenerThread.Start();

            _state = State.Started;
        }

        // This method runs on it's own thread and blocks untill a new request occurs
        // It then creates a new thread that will handle the request and then continues to listen for the next request
        private void Listen()
        {
            _listenerSocket.Listen(int.MaxValue);

            // Keep listening while the state is set to Started
            while (_state == State.Started)
            {
                Socket clientSocket;

                // Block until a new client connects

                try
                {
                    clientSocket = _listenerSocket.Accept();
                }
                catch (SocketException ex)
                {
                    // If the connection was closed, stop listening right away
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        return;

                    // Otherwise, an unexpected exception occured so just throw it
                    throw;
                }

                // When a new client is connected, handle the request on a new thread

                Thread requestThread = new Thread(OnRequest)
                {
                    Name = Guid.NewGuid().ToString()
                };

                requestThread.Start(clientSocket);
            }

            _listenerSocket.Close();
        }

        private void OnRequest(object clientSocketObj)
        {
            using (Socket clientSocket = (Socket)clientSocketObj)
            {
                IPEndPoint endPoint = (IPEndPoint)clientSocket.RemoteEndPoint;

                using (NetworkStream networkStream = new NetworkStream(clientSocket))
                {
                    ResponseMessage response = null;

                    try
                    {
                        RequestMessage request = RequestMessage.Create(networkStream);

                        if (request != null)
                        {
                            _log.InfoFormat("Received request from {0}:{1}.\r\n{2}", endPoint.Address, endPoint.Port, request.ToString(_log.IsDebugEnabled));
                            response = _requestHandler.Handle(request) ?? new ResponseMessage(HttpStatusCode.OK);
                        }
                    }
                    catch (Exception ex)
                    {
                        response = _errorHandler.Handle(ex);
                    }

                    if (response != null)
                    {
                        _log.InfoFormat("Sending response to {0}:{1}.\r\n{2}", endPoint.Address, endPoint.Port, response.ToString(_log.IsDebugEnabled));
                        response.WriteToStream(networkStream);
                    }
                }
            }
        }

        /// <summary>
        /// Stops listening for new requests.
        /// </summary>
        public void Stop()
        {
            if (_state != State.Started)
                return;

            _state = State.Stopping;

            _listenerSocket.Dispose();

            _state = State.Stopped;
        }

        /// <summary>
        /// Stops the listener and starts it again using the specified port.
        /// </summary>
        public void Restart(int port)
        {
            Stop();
            Start(port);
        }

        public enum State
        {
            Stopped,
            Starting,
            Started,
            Stopping
        }
    }
}