using System;
using SecureWebServer.Core.Helpers;

namespace SecureWebServer.Core.Extensions
{
    public static class UriExtensions
    {
        public static string GetFilePath(this Uri uri, string root)
        {
            string relativePath = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
            return PathHelper.Combine(root, relativePath);
        }
    }
}