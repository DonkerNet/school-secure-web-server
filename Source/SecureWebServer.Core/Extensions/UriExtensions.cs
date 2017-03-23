using System;
using SecureWebServer.Core.Helpers;

namespace SecureWebServer.Core.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="Uri"/> objects.
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Converts an uri into a full file path.
        /// </summary>
        public static string GetFilePath(this Uri uri, string root)
        {
            string relativePath = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
            return PathHelper.Combine(root, relativePath);
        }
    }
}