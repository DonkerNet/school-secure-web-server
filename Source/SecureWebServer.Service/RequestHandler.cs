using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using log4net;
using log4net.Appender;
using SecureWebServer.Core.Entities;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Helpers;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.DataAccess.Repositories;
using SecureWebServer.Service.Config;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service
{
    public class RequestHandler : IRequestHandler
    {
        private readonly UserRepository _userRepository;
        private readonly SecurityProvider _securityProvider;
        private readonly string[] _allowedMethods;

        public RequestHandler()
        {
            _userRepository = new UserRepository();
            _securityProvider = new SecurityProvider(_userRepository);
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

            request.User = _securityProvider.GetUserForRequest(request);
            
            if (!_securityProvider.UserIsInRole(request.Path, request.HttpMethod, request.User))
                throw new RequestException(HttpStatusCode.Forbidden, "Go away!");

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
            ServerConfiguration config = ServerConfiguration.Get();

            if(config.DirectoryBrowsing == false)
                throw new RequestException(HttpStatusCode.Forbidden,"Directory listing not allowed.");

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            DirectoryInfo info = new DirectoryInfo(fileInfo.FullName);

            string html = $"<html><head><title>Directory listing: { info.Name}</title></head><body><h1>Directory listing: { info.Name}</h1><br/>";


            if (info.Parent != null && info.Parent.Name == config.WebRoot == false)
            {
                if(request.Path.Contains("/"))
                html = html + $"<b><a href=\"/{request.Path.Substring(0,request.Path.LastIndexOf('/'))}\">..</a></b><br/>";
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
            response.SetStringContent(html,Encoding.ASCII,"text/html");
            return response;
        }



        private ResponseMessage HandleFileRequest(RequestMessage request, FileInfo fileInfo)
        {
            ResponseMessage response;

            switch (request.Path.ToLowerInvariant())
            {
                case "":
                case "index.html":
                    response = HandleIndexRequest(request, fileInfo);
                    break;

                case "config.html":
                    response = HandleConfigRequest(request, fileInfo);
                    break;

                case "log.html":
                    response = HandleLogRequest(request, fileInfo);
                    break;

                case "login.html":
                    response = HandleLoginRequest(request, fileInfo);
                    break;

                case "logout.html":
                    response = HandleLogoutRequest(request, fileInfo);
                    break;

                case "user/overview.html":
                    response = HandleUserOverviewRequest(request, fileInfo);
                    break;

                case "user/edit.html":
                    response = HandleUserEditRequest(request, fileInfo);
                    break;

                default:
                    response = HandleDefaultFileRequest(fileInfo);
                    break;
            }

            //Prevent XSS attacks
            response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'unsafe-inline' 'unsafe-eval'");
            //If a browser does not support the header above they will use the header below:
            response.Headers.Add("X-XSS-Protection","1; mode=block");

            return response;
        }

        private ResponseMessage HandleIndexRequest(RequestMessage request, FileInfo fileInfo)
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

            string html = fileInfo.ReadFullString();
            html = html.Replace("{Links}", linkHtmlBuilder.ToString());

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        private ResponseMessage HandleConfigRequest(RequestMessage request, FileInfo fileInfo)
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

            string saveButtonHtml = _securityProvider.UserIsInRole(request.Path, "POST", request.User)
                ? "<td><input type=\"submit\" value=\"Save\"></td>"
                : string.Empty;

            StringBuilder htmlBuilder = new StringBuilder(fileInfo.ReadFullString());
            htmlBuilder.Replace("{WebPort}", config.WebPort.ToString());
            htmlBuilder.Replace("{WebRoot}", config.WebRoot);
            htmlBuilder.Replace("{DefaultPages}", config.DefaultPages != null ? string.Join(";", config.DefaultPages) : string.Empty);
            htmlBuilder.Replace("{DirectoryBrowsing}", config.DirectoryBrowsing ? "checked='checked'" : string.Empty);
            htmlBuilder.Replace("{SaveButton}", saveButtonHtml);

            response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlBuilder.ToString(), Encoding.ASCII, "text/html");
            return response;
        }

        private ResponseMessage HandleLogRequest(RequestMessage request, FileInfo fileInfo)
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

            StringBuilder htmlBuilder = new StringBuilder(fileInfo.ReadFullString());
            htmlBuilder.Replace("{LogEntries}", logHtml);

            string clearLogButtonHtml;

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

        private ResponseMessage HandleLoginRequest(RequestMessage request, FileInfo fileInfo)
        {
            ResponseMessage response;

            if (request.HttpMethod == "POST")
            {
                string username = request.FormData["Username"];
                string password = request.FormData["Password"];

                response = new ResponseMessage(HttpStatusCode.Redirect);
                response.Headers["Location"] = "/";

                if (!_securityProvider.AuthenticateUser(username, password, response))
                    throw new RequestException(HttpStatusCode.Unauthorized, "Invalid credentials.");

                return response;
            }

            string html = fileInfo.ReadFullString();

            response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        private ResponseMessage HandleLogoutRequest(RequestMessage request, FileInfo fileInfo)
        {
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            _securityProvider.LogoutUser(request, response);

            string html = fileInfo.ReadFullString();
            response.SetStringContent(html, Encoding.ASCII, "text/html");

            return response;
        }

        private ResponseMessage HandleUserOverviewRequest(RequestMessage request, FileInfo fileInfo)
        {
            IList<User> users = _userRepository.GetAll();

            StringBuilder userListHtmlBuilder = new StringBuilder();

            foreach (User user in users)
                userListHtmlBuilder.AppendLine($"<li><a href=\"/user/edit.html?id={user.Id}\">{user.Name}</a></li>");

            string html = fileInfo.ReadFullString();
            html = html.Replace("{UserList}", userListHtmlBuilder.ToString());

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(html, Encoding.ASCII, "text/html");
            return response;
        }

        private ResponseMessage HandleUserEditRequest(RequestMessage request, FileInfo fileInfo)
        {
            Guid userId;

            string userIdString = request.HttpMethod == "GET"
                ? request.QueryString["id"]
                : request.FormData["UserId"];

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out userId))
                throw new RequestException(HttpStatusCode.BadRequest, "Invalid user ID.");
            
            User user = _userRepository.GetById(userId);

            if (user == null)
                throw new RequestException(HttpStatusCode.NotFound, "User not found.");

            if (request.HttpMethod == "POST")
            {
                string[] roles = request.FormData.GetValues("Role") ?? new string[0];

                if (!roles.Any(_securityProvider.RoleExists))
                    throw new RequestException(HttpStatusCode.BadRequest, "Invalid roles.");

                user.Roles = roles;

                _userRepository.Update(user);
            }

            StringBuilder rolesHtmlBuilder = new StringBuilder();

            foreach (string role in SecurityProvider.Roles)
            {
                rolesHtmlBuilder.AppendFormat("<label><input type=\"checkbox\" name=\"Role\" value=\"{0}\"{1} /> {0}</label><br/>\r\n",
                    role,
                    user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) ? " checked=\"checked\"" : string.Empty);
            }

            StringBuilder htmlBuilder = new StringBuilder(fileInfo.ReadFullString());
            htmlBuilder.Replace("{UserId}", user.Id.ToString());
            htmlBuilder.Replace("{Username}", user.Name);
            htmlBuilder.Replace("{Roles}", rolesHtmlBuilder.ToString());

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlBuilder.ToString(), Encoding.UTF8, "text/html");
            return response;
        }

        private ResponseMessage HandleDefaultFileRequest(FileInfo fileInfo)
        {
            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);

            // Could be any file of any size, so simply set the stream instead of copying it

            response.SetContentStream(
                fileInfo.OpenRead(),
                MimeMapping.GetMimeMapping(fileInfo.ToString()));

            return response;
        }
    }
}