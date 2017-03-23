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
    /// Handles a request to show the homepage.
    /// </summary>
    public class IndexCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public IndexCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

        /// <summary>
        /// Builds the index page with a list of links available to the user, based on their permissions.
        /// </summary>
        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            StringBuilder linkHtmlBuilder = new StringBuilder();

            if (request.User == null)
            {
                linkHtmlBuilder.AppendLine("<li><a href=\"Login.html\">Login</a></li>");
            }
            else
            {
                if (_securityProvider.UserIsInRole("config.html", "GET", request.User))
                    linkHtmlBuilder.AppendLine("<li><a href=\"Config.html\">Manage server configuration</a></li>");

                if (_securityProvider.UserIsInRole("user/overview.html", "GET", request.User))
                    linkHtmlBuilder.AppendLine("<li><a href=\"User/Overview.html\">Edit user permissions</a></li>");

                linkHtmlBuilder.AppendLine("<li><a href=\"Logout.html\">Logout</a></li>");
            }

            string html = requestedFile.ReadFullString();
            html = html.Replace("{Links}", linkHtmlBuilder.ToString());

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        /// <summary>
        /// Simply handle a POST request as a GET.
        /// </summary>
        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            return HandleGet(request, requestedFile);
        }
    }
}