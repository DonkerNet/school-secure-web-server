﻿using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Config;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    /// <summary>
    /// Handles a request for showing or updating the webserver's configuration.
    /// </summary>
    public class ConfigCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;

        public ConfigCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;
        }

        /// <summary>
        /// Shows the configuration of the webserver.
        /// </summary>
        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            ServerConfiguration config = ServerConfiguration.Get();
            return CreateHtmlResponse(request, requestedFile, config);
        }

        /// <summary>
        /// Saves the new configuration and redirects to the homepage using the correct port.
        /// </summary>
        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            ServerConfiguration config = ServerConfiguration.Get();

            int previousPort = config.WebPort;

            config.SetValues(request.FormData);
            config.Save();

            ResponseMessage response = new ResponseMessage(HttpStatusCode.Redirect);
            response.Headers["Location"] = "/";

            // If the port was changed and the host+port was specified in the headers, redirect to the new URL
            if (previousPort != config.WebPort)
            {
                string host = request.Headers["Host"];

                if (!string.IsNullOrEmpty(host))
                {
                    string[] hostParts = host.Split(new[] { ':' }, 2);

                    if (hostParts.Length > 1 && hostParts[1] == previousPort.ToString())
                    {
                        response.Headers["Location"] = $"http://{hostParts[0]}:{config.WebPort}";
                        return response;
                    }
                }
            }

            return response;
        }

        private ResponseMessage CreateHtmlResponse(RequestMessage request, FileInfo requestedFile, ServerConfiguration config)
        {
            // The save button is only visible for those with the proper permissions
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