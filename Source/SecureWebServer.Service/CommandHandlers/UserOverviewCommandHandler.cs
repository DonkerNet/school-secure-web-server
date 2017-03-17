using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Entities;
using SecureWebServer.Core.Extensions;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.DataAccess.Repositories;

namespace SecureWebServer.Service.CommandHandlers
{
    public class UserOverviewCommandHandler : ICommandHandler
    {
        private readonly UserRepository _userRepository;

        public UserOverviewCommandHandler(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public ResponseMessage HandleGet(RequestMessage request, FileInfo requestedFile)
        {
            IList<User> users = _userRepository.GetAll();

            StringBuilder userListHtmlBuilder = new StringBuilder();

            foreach (User user in users)
                userListHtmlBuilder.AppendLine($"<li><a href=\"/user/edit.html?id={user.Id}\">{user.Name}</a></li>");

            string html = requestedFile.ReadFullString();
            html = html.Replace("{UserList}", userListHtmlBuilder.ToString());

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