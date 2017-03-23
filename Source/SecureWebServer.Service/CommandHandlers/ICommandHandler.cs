using System.IO;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Service.CommandHandlers
{
    /// <summary>
    /// Interface for a command handling class that supports HTTP GET and POST requests.
    /// </summary>
    public interface ICommandHandler
    {
        ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile);
        ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile);
    }
}