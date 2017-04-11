using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using log4net;
using log4net.Appender;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    /// <summary>
    /// Handler for showing or clearing the application log file.
    /// </summary>
    public class LogCommandHandler : ICommandHandler
    {
        private readonly SecurityProvider _securityProvider;
        private readonly RollingFileAppender _logAppender;

        public LogCommandHandler(SecurityProvider securityProvider)
        {
            _securityProvider = securityProvider;

            // The log file's location needs to be retrieved from the log4net appender settings
            _logAppender = LogManager
                .GetRepository()
                .GetAppenders()
                .OfType<RollingFileAppender>()
                .FirstOrDefault();
        }

        /// <summary>
        /// Shows a page with all the log entries.
        /// </summary>
        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            StringBuilder logHtmlBuilder = new StringBuilder();

            using (StreamReader reader = new StreamReader(_logAppender.File))
            {
                string logLine;

                while ((logLine = reader.ReadLine()) != null)
                {
                    logHtmlBuilder.AppendLine("<br/>");
                    logHtmlBuilder.Append(logLine.Replace(" ", "&nbsp;"));
                }
            }

            string logHtml = logHtmlBuilder.ToString();
            return CreateHtmlResponse(request, requestedFile, logHtml);
        }

        /// <summary>
        /// Clears the log if that was requested. If not, it simply handles this as a GET request.
        /// </summary>
        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            if (string.IsNullOrEmpty(request.FormData["ClearLog"]))
                return HandleGet(request, requestedFile);

            using (Stream logStream = File.OpenWrite(_logAppender.File))
            {
                logStream.SetLength(0L);
                logStream.Flush();
            }

            string logHtml = "Log cleared!";
            return CreateHtmlResponse(request, requestedFile, logHtml);
        }

        private ResponseMessage CreateHtmlResponse(RequestMessage request, FileInfo requestedFile, string logHtml)
        {
            StringBuilder htmlBuilder = new StringBuilder(requestedFile.ReadFullString());
            htmlBuilder.Replace("{LogEntries}", logHtml);

            string clearLogButtonHtml;

            // The clear button can only be present if the user has that permission
            if (_securityProvider.UserIsInRole(request.Path, "POST", request.User))
            {
                clearLogButtonHtml =
@"<form action=""Log.html"" method=""POST"">
<input type=""submit"" name=""ClearLog"" value=""Clear log""/>
</form>";
            }
            else
            {
                clearLogButtonHtml = string.Empty;
            }

            htmlBuilder.Replace("{ClearLogButton}", clearLogButtonHtml);

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlBuilder.ToString(), Encoding.ASCII, "text/html");
            return response;
        }
    }
}