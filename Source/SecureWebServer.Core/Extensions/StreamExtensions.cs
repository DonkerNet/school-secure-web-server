using System;
using System.IO;

namespace SecureWebServer.Core.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="Stream"/> objects.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Copies a specific amount of bytes to another stream, using the specified buffer size.
        /// </summary>
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

        /// <summary>
        /// Copies a specific amount of bytes to another stream.
        /// </summary>
        public static void CopyTo(this Stream source, Stream target, int offset, long count)
        {
            CopyTo(source, target, offset, count, 81920);
        }
    }
}