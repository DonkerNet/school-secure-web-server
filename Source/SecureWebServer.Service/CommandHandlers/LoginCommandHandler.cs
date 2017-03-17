using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    public class LoginCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public LoginCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            string html = requestedFile.ReadFullString();
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            string username = request.FormData["Username"];
            string password = request.FormData["Password"];

            ResponseMessage response = new ResponseMessage(HttpStatusCode.Redirect);
            response.Headers["Location"] = "/";

            if (!_securityProvider.AuthenticateUser(username, password, response))
                throw new RequestException(HttpStatusCode.Unauthorized, "Invalid credentials.");

            return response;
        }
    }
}