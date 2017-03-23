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
    /// <summary>
    /// Class for creating a response with a proper error page for any error that occured.
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILog _log;

        public ErrorHandler()
        {
            _log = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Creates a response with a proper error page for any error that occured.
        /// </summary>
        public ResponseMessage Handle(Exception exception)
        {
            _log.Error(exception);

            HttpStatusCode statusCode;
            string message;

            RequestException requestException = exception as RequestException;

            if (requestException == null)
            {
                // Unknown/unexpected error
                // Don't show the user any error information!
                statusCode = HttpStatusCode.InternalServerError;
                message = null;
            }
            else
            {
                // Deliberate error
                // Show the error message that was set
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

            // If an error page is present for the response's status code, show that instead of the message
            // Otherwise, simply return the error message as plain text

            if (File.Exists(errorPagePath))
                response.SetContentStream(File.OpenRead(errorPagePath), "text/html");
            else if (!string.IsNullOrEmpty(message))
                response.SetStringContent(message, Encoding.ASCII, "text/plain");

            return response;
        }
    }
}