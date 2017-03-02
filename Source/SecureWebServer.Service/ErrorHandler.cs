using System;
using System.IO;
using System.Net;
using log4net;
using SecureWebServer.Core.Error;
using SecureWebServer.Core.Request;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Service
{
    public class ErrorHandler : IErrorHandler
    {
        private readonly ILog _log;

        public ErrorHandler()
        {
            _log = LogManager.GetLogger(GetType());
        }

        public ResponseMessage Handle(Exception exception)
        {
            _log.Error(exception);

            RequestException requestException = exception as RequestException;

            if (requestException == null)
                return new ResponseMessage(HttpStatusCode.InternalServerError);

            ResponseMessage response = new ResponseMessage(requestException.StatusCode);

            string errorPagePath = $"ErrorPages\\{(int)requestException.StatusCode}.html";

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