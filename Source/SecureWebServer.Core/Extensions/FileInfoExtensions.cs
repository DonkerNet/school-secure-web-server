using System.IO;

namespace SecureWebServer.Core.Extensions
{
    public static class FileInfoExtensions
    {
        public static bool IsDirectory(this FileInfo fileInfo)
        {
            return fileInfo.Attributes > 0
                && (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }
}
