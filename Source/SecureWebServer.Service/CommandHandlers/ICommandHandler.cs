using System.IO;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Service.CommandHandlers
{
    public interface ICommandHandler
    {
        ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile);
        ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile);
    }
}