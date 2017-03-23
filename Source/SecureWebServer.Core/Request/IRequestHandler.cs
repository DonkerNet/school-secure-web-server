using SecureWebServer.Core.Response;

namespace SecureWebServer.Core.Request
{
    /// <summary>
    /// Interface for a class that handles a requests and returns a proper response.
    /// </summary>
    public interface IRequestHandler
    {
        /// <summary>
        /// Handles a requests and returns a proper response.
        /// </summary>
        ResponseMessage Handle(RequestMessage request);
    }
}