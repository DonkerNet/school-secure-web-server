using System;
using System.Net;

namespace SecureWebServer.Core.Request
{
    public class RequestException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public RequestException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public RequestException(HttpStatusCode statusCode, string message)
           : this(statusCode, message, null)
        {
        }
    }
}