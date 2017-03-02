using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using SecureWebServer.Core.Error;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Core.Request
{
    public class RequestListener
    {
        private readonly ILog _log;
        private readonly IRequestHandler _requestHandler;
        private readonly IErrorHandler _errorHandler;
        private Socket _listenerSocket;
        private State _state;

        public RequestListener(IRequestHandler requestHandler, IErrorHandler errorHandler)
        {
            _log = LogManager.GetLogger(GetType());
            _requestHandler = requestHandler;
            _errorHandler = errorHandler;
            _state = State.Stopped;
        }

        public void Start(int port)
        {
            if (_state != State.Stopped)
                return;

            _state = State.Starting;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            _listenerSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenerSocket.Bind(endPoint);

            Thread listenerThread = new Thread(Listen);
            listenerThread.Start();

            _state = State.Started;
        }

        private void Listen()
        {
            _listenerSocket.Listen(int.MaxValue);

            while (_state == State.Started)
            {
                Socket clientSocket;

                try
                {
                    clientSocket = _listenerSocket.Accept();
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        return;
                    throw;
                }

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

        public void Stop()
        {
            if (_state != State.Started)
                return;

            _state = State.Stopping;

            _listenerSocket.Dispose();

            _state = State.Stopped;
        }

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