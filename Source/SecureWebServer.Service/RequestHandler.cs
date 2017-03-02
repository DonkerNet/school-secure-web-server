using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using log4net;
using log4net.Appender;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Helpers;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Config;

namespace SecureWebServer.Service
{
    public class RequestHandler : IRequestHandler
    {
        private readonly string[] _allowedMethods;

        public RequestHandler()
        {
            _allowedMethods = new[] { "GET", "POST" };
        }

        public ResponseMessage Handle(RequestMessage request)
        {
            if (!_allowedMethods.Contains(request.HttpMethod))
                throw new RequestException(HttpStatusCode.MethodNotAllowed, $"Only the following methods are supported: {string.Join(", ", _allowedMethods)}.");

            ServerConfiguration config = ServerConfiguration.Get();

            // Convert the request path to a physical file path
            string filePath = PathHelper.Combine(config.WebRoot, request.Path);

            // If no path was specified, show the default page instead
            if (filePath == config.WebRoot)
                filePath = GetDefaultPagePath(config);

            FileInfo fileInfo = new FileInfo(filePath);
            bool isDirectory = fileInfo.IsDirectory();

            if (!fileInfo.Exists && !isDirectory)
                throw new RequestException(HttpStatusCode.NotFound, "The specified resource was not found.");

            return isDirectory
                ? HandleDirectoryBrowseRequest(request, fileInfo)
                : HandleFileRequest(request, fileInfo);
        }

        private string GetDefaultPagePath(ServerConfiguration config)
        {
            foreach (string defaultPage in config.DefaultPages)
            {
                string path = PathHelper.Combine(config.WebRoot, defaultPage);
                if (File.Exists(path))
                    return path;
            }

            return config.WebRoot;
        }

        /// <summary>
        /// 
        /// </summary>
        private ResponseMessage HandleDirectoryBrowseRequest(RequestMessage request, FileInfo fileInfo)
        {
            return new ResponseMessage(HttpStatusCode.Forbidden); // TODO: directory listing
        }

        private ResponseMessage HandleFileRequest(RequestMessage request, FileInfo fileInfo)
        {
            ResponseMessage response;

            switch (request.Path.ToLowerInvariant())
            {
                case "config.html":
                    response = HandleConfigFileRequest(request, fileInfo);
                    break;

                case "log.html":
                    response = HandleLogFileRequest(request, fileInfo);
                    break;

                default:
                    response = HandleDefaultFileRequest(fileInfo);
                    break;
            }

            return response;
        }

        private ResponseMessage HandleConfigFileRequest(RequestMessage request, FileInfo fileInfo)
        {
            // This method handles GET and POST requests for the configuration page

            ServerConfiguration config = ServerConfiguration.Get();

            ResponseMessage response;

            if (request.HttpMethod == "POST")
            {
                int previousPort = config.WebPort;

                config.SetValues(request.FormData);
                config.Save();

                // If the port was changed and the host+port was specified in the headers, redirect to the new URL
                if (previousPort != config.WebPort)
                {
                    string host = request.Headers["Host"];

                    if (!string.IsNullOrEmpty(host))
                    {
                        string[] hostParts = host.Split(new[] {':'}, 2);

                        if (hostParts.Length > 1 && hostParts[1] == previousPort.ToString())
                        {
                            response = new ResponseMessage(HttpStatusCode.Redirect);
                            response.Headers["Location"] = $"http://{hostParts[0]}:{config.WebPort}/{request.Path}";
                            return response;
                        }
                    }
                }
            }

            string htmlTemplate;

            using (StreamReader reader = new StreamReader(fileInfo.OpenRead()))
                htmlTemplate = reader.ReadToEnd();

            StringBuilder htmlBuilder = new StringBuilder(htmlTemplate);
            htmlBuilder.Replace("{WebPort}", config.WebPort.ToString());
            htmlBuilder.Replace("{WebRoot}", config.WebRoot);
            htmlBuilder.Replace("{DefaultPages}", config.DefaultPages != null ? string.Join(";", config.DefaultPages) : string.Empty);
            htmlBuilder.Replace("{DirectoryBrowsing}", config.DirectoryBrowsing ? "checked='checked'" : string.Empty);

            response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlBuilder.ToString(), "text/html");
            return response;
        }

        private ResponseMessage HandleLogFileRequest(RequestMessage request, FileInfo fileInfo)
        {
            // This method reads the log that log4net is currently writing to

            RollingFileAppender logAppender = LogManager
                .GetRepository()
                .GetAppenders()
                .OfType<RollingFileAppender>()
                .FirstOrDefault();

            string logHtml;

            if (logAppender == null || !File.Exists(logAppender.File))
            {
                logHtml = "No log file found.";
            }
            else
            {
                if (request.HttpMethod == "POST" && !string.IsNullOrEmpty(request.FormData["ClearLog"]))
                {
                    // Clear the log content if requested

                    using (Stream logStream = File.OpenWrite(logAppender.File))
                    {
                        logStream.SetLength(0L);
                        logStream.Flush();
                    }

                    logHtml = "Log cleared!";
                }
                else
                {
                    // Convert the log lines to HTML

                    StringBuilder logHtmlBuilder = new StringBuilder();

                    using (StreamReader reader = new StreamReader(logAppender.File))
                    {
                        string logLine;

                        while ((logLine = reader.ReadLine()) != null)
                        {
                            logHtmlBuilder.Append(logLine.Replace(" ", "&nbsp;"));
                            logHtmlBuilder.AppendLine("<br/>");
                        }
                    }

                    logHtml = logHtmlBuilder.ToString();
                }
            }

            string htmlTemplate;

            using (StreamReader reader = new StreamReader(fileInfo.OpenRead()))
                htmlTemplate = reader.ReadToEnd();

            htmlTemplate = htmlTemplate.Replace("{LogEntries}", logHtml);

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlTemplate, "text/html");
            return response;
        }

        private ResponseMessage HandleDefaultFileRequest(FileInfo fileInfo)
        {
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            using (Stream fileStream = fileInfo.OpenRead())
                fileStream.CopyTo(response.Content);

            response.Headers["Content-Type"] = MimeMapping.GetMimeMapping(fileInfo.ToString());

            return response;
        }
    }
}