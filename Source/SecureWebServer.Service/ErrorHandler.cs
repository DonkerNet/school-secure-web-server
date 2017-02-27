using System;
using System.IO;
using System.Net;
using SecureWebServer.Core.Error;
using SecureWebServer.Core.Helpers;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;
using SecureWebServer.Service.Config;

namespace SecureWebServer.Service
{
    public class ErrorHandler : IErrorHandler
    {
        public ResponseMessage Handle(Exception exception)
        {
            // TODO: log exception

            RequestException requestException = exception as RequestException;

            if (requestException == null)
                return new ResponseMessage(HttpStatusCode.InternalServerError);

            ResponseMessage response = new ResponseMessage(requestException.StatusCode);

            ServerConfiguration config = ServerConfiguration.Get();

            string errorPagePath = PathHelper.Combine(config.WebRoot, $"ErrorPages\\{(int)requestException.StatusCode}.html");

            if (File.Exists(errorPagePath))
            {
                using (Stream fileStream = File.OpenRead(errorPagePath))
                    fileStream.CopyTo(response.Content);

                response.Headers["Content-Type"] = "text/html";
            }
            else
            {
                response.SetStringContent(requestException.Message);
            }
            
            return response;
        }
    }
}