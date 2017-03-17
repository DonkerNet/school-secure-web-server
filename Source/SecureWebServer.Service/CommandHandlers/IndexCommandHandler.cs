using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    public class IndexCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public IndexCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

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

        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            return HandleGet(request, requestedFile);
        }
    }
}