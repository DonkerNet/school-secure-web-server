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

            HttpStatusCode statusCode;
            string message;

            RequestException requestException = exception as RequestException;

            if (requestException == null)
            {
                // Unknown/unexpected error
                statusCode = HttpStatusCode.InternalServerError;
                message = null;
            }
            else
            {
                // Deliberate error
                statusCode = requestException.StatusCode;
                message = requestException.Message;
            }

            ResponseMessage response = CreateResponse(statusCode, message);
            return response;
        }

        private ResponseMessage CreateResponse(HttpStatusCode statusCode, string message)
        {
            ResponseMessage response = new ResponseMessage(statusCode);

            string errorPagePath = $"ErrorPages\\{(int)statusCode}.html";

            if (File.Exists(errorPagePath))
                response.SetContentStream(File.OpenRead(errorPagePath), "text/html");
            else if (!string.IsNullOrEmpty(message))
                response.SetStringContent(message, Encoding.ASCII, "text/plain");

            return response;
        }
    }
}