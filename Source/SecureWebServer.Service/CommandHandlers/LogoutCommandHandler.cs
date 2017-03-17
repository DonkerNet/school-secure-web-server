using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    public class LogoutCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public LogoutCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            _securityProvider.LogoutUser(request, response);

            string html = requestedFile.ReadFullString();
            response.SetStringContent(html, Encoding.ASCII, "text/html");

            return response;
        }

        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            return HandleGet(request, requestedFile);
        }
    }
}