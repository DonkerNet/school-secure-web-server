using System;
using System.IO;

namespace SecureWebServer.Core.Extensions
{
    public static class StreamExtensions
    {
        public static void CopyTo(this Stream source, Stream target, int offset, long count, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];

            do
            {
                int bytesToRead = Math.Min(bufferSize, (int)count);
                int bytesRead = source.Read(buffer, 0, bytesToRead);
                target.Write(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            while (count > 0);
        }

        public static void CopyTo(this Stream source, Stream target, int offset, long count)
        {
            CopyTo(source, target, offset, count, 81920);
        }
    }
}