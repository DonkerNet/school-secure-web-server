using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    /// <summary>
    /// Handler for authenticating a user.
    /// </summary>
    public class LoginCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public LoginCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

        /// <summary>
        /// Shows the login page.
        /// </summary>
        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            string html = requestedFile.ReadFullString();
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        /// <summary>
        /// Authenticates the user and redirects to the main page if succeeded.
        /// </summary>
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