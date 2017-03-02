using System;
using System.IO;
using System.Net;
using System.Text;
using log4net;
using SecureWebServer.Core.Error;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Service
{
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILog _log;

        public ErrorHandler()
        {
            _log = LogManager.GetLogger(GetType());
        }

        public ResponseMessage Handle(Exception exception)
        {
            _log.Error(exception);

            RequestException requestException = exception as RequestException;

            if (requestException == null)
                return new ResponseMessage(HttpStatusCode.InternalServerError);

            ResponseMessage response = new ResponseMessage(requestException.StatusCode);

            string errorPagePath = $"ErrorPages\\{(int)requestException.StatusCode}.html";

            if (File.Exists(errorPagePath))
                response.SetContentStream(File.OpenRead(errorPagePath), "text/html");
            else
                response.SetStringContent(requestException.Message, Encoding.ASCII, "text/plain");

            return response;
        }
    }
}