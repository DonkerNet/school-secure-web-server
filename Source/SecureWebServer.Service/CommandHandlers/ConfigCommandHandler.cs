using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Config;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    public class ConfigCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public ConfigCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            ServerConfiguration config = ServerConfiguration.Get();
            return CreateHtmlResponse(request, requestedFile, config);
        }

        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            ServerConfiguration config = ServerConfiguration.Get();

            int previousPort = config.WebPort;

            config.SetValues(request.FormData);
            config.Save();

            // If the port was changed and the host+port was specified in the headers, redirect to the new URL
            if (previousPort != config.WebPort)
            {
                string host = request.Headers["Host"];

                if (!string.IsNullOrEmpty(host))
                {
                    string[] hostParts = host.Split(new[] { ':' }, 2);

                    if (hostParts.Length > 1 && hostParts[1] == previousPort.ToString())
                    {
                        ResponseMessage response = new ResponseMessage(HttpStatusCode.Redirect);
                        response.Headers["Location"] = $"http://{hostParts[0]}:{config.WebPort}/{request.Path}";
                        return response;
                    }
                }
            }

            return CreateHtmlResponse(request, requestedFile, config);
        }

        private ResponseMessage CreateHtmlResponse(RequestMessage request, FileInfo requestedFile, ServerConfiguration config)
        {
            string saveButtonHtml = _securityProvider.UserIsInRole(request.Path, "POST", request.User)
                ? "<td><input type=\"submit\" value=\"Save\"></td>"
                : string.Empty;

            StringBuilder htmlBuilder = new StringBuilder(requestedFile.ReadFullString());
            htmlBuilder.Replace("{WebPort}", config.WebPort.ToString());
            htmlBuilder.Replace("{WebRoot}", config.WebRoot);
            htmlBuilder.Replace("{DefaultPages}", config.DefaultPages != null ? string.Join(";", config.DefaultPages) : string.Empty);
            htmlBuilder.Replace("{DirectoryBrowsing}", config.DirectoryBrowsing ? "checked='checked'" : string.Empty);
            htmlBuilder.Replace("{SaveButton}", saveButtonHtml);

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlBuilder.ToString(), Encoding.ASCII, "text/html");
            return response;
        }
    }
}