using SecureWebServer.Core.Response;

namespace SecureWebServer.Core.Request
{
    public interface IRequestHandler
    {
        ResponseMessage Handle(RequestMessage request);
    }
}