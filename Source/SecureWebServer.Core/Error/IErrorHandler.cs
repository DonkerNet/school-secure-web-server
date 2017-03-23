using System;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Core.Error
{
    /// <summary>
    /// Interface for a class that creates a proper response for an exception that occured.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Creates a proper response for an exception that occured.
        /// </summary>
        ResponseMessage Handle(Exception exception);
    }
}