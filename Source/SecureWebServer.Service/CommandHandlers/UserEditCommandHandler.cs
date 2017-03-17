using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using SecureWebServer.Core.Entities;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.DataAccess.Repositories;
using SecureWebServer.Service.Security;

namespace SecureWebServer.Service.CommandHandlers
{
    public class UserEditCommandHandler : ICommandHandler
    {
        private readonly UserRepository _userRepository;
        private readonly SecurityProvider _securityProvider;
        
        public UserEditCommandHandler(UserRepository userRepository, SecurityProvider securityProvider)
        {
            _userRepository = userRepository;
            _securityProvider = securityProvider;
        }

        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            User user = GetUser(request.QueryString["id"]);
            return CreateHtmlResponse(requestedFile, user);
        }

        public ResponseMessage HandlePost(RequestMessage request, FileInfo requestedFile)
        {
            User user = GetUser(request.FormData["UserId"]);

            string[] roles = request.FormData.GetValues("Role") ?? new string[0];

            if (!roles.Any(_securityProvider.RoleExists))
                throw new RequestException(HttpStatusCode.BadRequest, "Invalid roles.");

            user.Roles = roles;

            _userRepository.Update(user);

            ResponseMessage responseMessage = new ResponseMessage(HttpStatusCode.Redirect);
            responseMessage.Headers["Location"] = "/User/Overview.html";
            return responseMessage;
        }

        private User GetUser(string userIdString)
        {
            Guid userId;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out userId))
                throw new RequestException(HttpStatusCode.BadRequest, "Invalid user ID.");

            User user = _userRepository.GetById(userId);

            if (user == null)
                throw new RequestException(HttpStatusCode.NotFound, "User not found.");

            return user;
        }

        private ResponseMessage CreateHtmlResponse(FileInfo requestedFile, User user)
        {
            StringBuilder rolesHtmlBuilder = new StringBuilder();

            foreach (string role in SecurityProvider.Roles)
            {
                rolesHtmlBuilder.AppendFormat("<label><input type=\"checkbox\" name=\"Role\" value=\"{0}\"{1} /> {0}</label><br/>\r\n",
                    role,
                    user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) ? " checked=\"checked\"" : string.Empty);
            }

            StringBuilder htmlBuilder = new StringBuilder(requestedFile.ReadFullString());
            htmlBuilder.Replace("{UserId}", user.Id.ToString());
            htmlBuilder.Replace("{Username}", user.Name);
            htmlBuilder.Replace("{Roles}", rolesHtmlBuilder.ToString());

            ResponseMessage response = new ResponseMessage(HttpStatusCode.OK);
            response.SetStringContent(htmlBuilder.ToString(), Encoding.UTF8, "text/html");
            return response;
        }
    }
}