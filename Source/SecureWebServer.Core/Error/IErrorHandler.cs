using System;
using SecureWebServer.Core.Response;

namespace SecureWebServer.Core.Error
{
    public interface IErrorHandler
    {
        ResponseMessage Handle(Exception exception);
    }
}