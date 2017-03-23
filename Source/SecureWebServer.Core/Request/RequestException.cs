using System;
using System.Net;

namespace SecureWebServer.Core.Request
{
    /// <summary>
    /// An exception to throw when a request is not OK (status 200) and an error response should be returned to the client.
    /// </summary>
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