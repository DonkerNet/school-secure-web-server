using System.IO;
using System.Net;
using System.Text;
using System.Web;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Helpers;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.DataAccess.Repositories;
using SecureWebServer.Service.CommandHandlers;
using SecureWebServer.Service.Config;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service
{
    /// <summary>
    /// This class is the entry point for any new request that needs handling.
    /// </summary>
    public class RequestHandler : IRequestHandler
    {
        private readonly SecurityProvider _securityProvider;
        private readonly CommandHandlerFactory _commandHandlerFactory;

        public RequestHandler()
        {
            UserRepository userRepository = new UserRepository();
            _securityProvider = new SecurityProvider(userRepository);
            _commandHandlerFactory = new CommandHandlerFactory(userRepository, _securityProvider);
        }

        /// <summary>
        /// Handles a new request.
        /// </summary>
        public ResponseMessage Handle(RequestMessage request)
        {
            // Check if there is an authenticated user and if the client is allowed to execute this request
            request.User = _securityProvider.GetUserForRequest(request);
            if (!_securityProvider.UserIsInRole(request.Path, request.HttpMethod, request.User))
                throw new RequestException(HttpStatusCode.Forbidden, "Go away!");

            ServerConfiguration config = ServerConfiguration.Get();

            // If no path is specified, try to find a valid default page to show to the user
            string actualPath = request.Path;
            if (string.IsNullOrEmpty(actualPath))
                actualPath = config.GetExistingDefaultPage();

            // Map and parse the relative path to get the actual file info
            FileInfo requestedFile = new FileInfo(PathHelper.Combine(config.WebRoot, actualPath));

            ResponseMessage response;

            // Check if we have a specific handler for this request, if so: use it to handle the request

            ICommandHandler commandHandler = _commandHandlerFactory.Create(actualPath);

            if (commandHandler != null)
            {
                switch (request.HttpMethod)
                {
                    case "GET":
                        response = commandHandler.HandleGet(request, requestedFile);
                        break;
                    case "POST":
                        response = commandHandler.HandlePost(request, requestedFile);
                        break;
                    default:
                        throw new RequestException(HttpStatusCode.MethodNotAllowed, "Only GET or POST are allowed.");
                }
            }
            else
            {
                // If there is no specific handler available, handle the request as a regular GET for a directory or file

                if (request.HttpMethod != "GET")
                    throw new RequestException(HttpStatusCode.MethodNotAllowed, "Only GET is allowed.");

                bool isFile = requestedFile.Exists;
                bool isDirectory = requestedFile.DirectoryExists();

                if (!isFile && !isDirectory)
                    throw new RequestException(HttpStatusCode.NotFound, "The specified resource was not found.");

                response = isDirectory
                    ? HandleDirectoryBrowseRequest(request, requestedFile)
                    : HandleFileRequest(requestedFile);
            }

            AddDefaultResponseHeaders(response);
            return response;
        }

        // Create a basic page showing the files and subfolders of the requested directory.
        private ResponseMessage HandleDirectoryBrowseRequest(RequestMessage request, FileInfo fileInfo)
        {
            ServerConfiguration config = ServerConfiguration.Get();

            if (config.DirectoryBrowsing == false)
                throw new RequestException(HttpStatusCode.Forbidden, "Directory listing not allowed.");

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            DirectoryInfo info = new DirectoryInfo(fileInfo.FullName);

            string html = $"<html><head><title>Directory listing: { info.Name}</title></head><body><h1>Directory listing: { info.Name}</h1><br/>";

            if (info.Parent != null && info.Parent.Name == config.WebRoot == false)
            {
                if (request.Path.Contains("/"))
                    html = html + $"<b><a href=\"/{request.Path.Substring(0, request.Path.LastIndexOf('/'))}\">..</a></b><br/>";
            }

            foreach (var file in info.GetDirectories())
            {
                html = html + $"<b><a href=\"{info.Name}/{file.Name}\">{file.Name}</a><br/></b>";
            }

            foreach (var file in info.GetFiles())
            {
                html = html + $"<b><a href=\"{info.Name}/{file.Name}\">{file.Name}</a><br/></b>";
            }

            html = html + "</html></body>";
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        // Simply create a response containing a requested file
        private ResponseMessage HandleFileRequest(FileInfo fileInfo)
        {
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            // Could be any file of any size, so simply set the stream instead of copying it

            response.SetContentStream(
                fileInfo.OpenRead(),
                MimeMapping.GetMimeMapping(fileInfo.ToString()));

            return response;
        }

        private void AddDefaultResponseHeaders(ResponseMessage response)
        {
            //Prevent XSS attacks
            response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'unsafe-inline' 'unsafe-eval'");

            //If a browser does not support the header above they will use the header below:
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
        }
    }
}