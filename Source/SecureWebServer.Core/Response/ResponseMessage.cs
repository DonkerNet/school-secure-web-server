using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using SecureWebServer.Core.Extensions;

namespace SecureWebServer.Core.Response
{
    /// <summary>
    /// Represents a response message that can be returned to the client.
    /// </summary>
    public class ResponseMessage
    {
        private Stream _content;

        /// <summary>
        /// The HTTP version used for the response.
        /// </summary>
        public const string HttpVersion = "HTTP/1.0";
        /// <summary>
        /// Gets the status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; }
        /// <summary>
        /// Gets a collection of headers to return with the response.
        /// </summary>
        public NameValueCollection Headers { get; }

        /// <summary>
        /// Creates a <see cref="ResponseMessage"/> using the specified HTTP status code.
        /// </summary>
        public ResponseMessage(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            Headers = new NameValueCollection();
        }

        #region Content methods

        /// <summary>
        /// Sets the specified stream content as the response body.
        /// </summary>
        public void SetContentStream(Stream contentStream, string contentType)
        {
            _content?.Dispose();
            _content = contentStream;

            if (!string.IsNullOrEmpty(contentType))
                Headers["Content-Type"] = contentType;
        }

        /// <summary>
        /// Sets the content from the specified byte array as the response body.
        /// </summary>
        public void SetByteContent(byte[] content, string contentType)
        {
            MemoryStream contentStream = new MemoryStream();

            if (content != null && content.Length > 0)
                contentStream.Write(content, 0, content.Length);

            SetContentStream(contentStream, contentType);
        }

        /// <summary>
        /// Sets the specified string content as the response body.
        /// </summary>
        public void SetStringContent(string content, Encoding encoding, string contentType)
        {
            byte[] buffer = null;

            if (!string.IsNullOrEmpty(content))
            {
                if (encoding == null)
                    encoding = Encoding.ASCII;

                buffer = encoding.GetBytes(content);
            }

            SetByteContent(buffer, contentType);
        }

        #endregion

        public void WriteToStream(Stream outputStream)
        {
            // Write the headers
            using (StreamWriter writer = new StreamWriter(outputStream, Encoding.ASCII, 1024, true))
            {
                // Write the Status-Line to the stream
                writer.WriteLine("{0} {1} {2}", HttpVersion, (int)StatusCode, GetStatusDescription());

                // Write all the headers to the stream
                foreach (string headerName in Headers.Keys)
                {
                    // Ignore the Content-Length header since we'll add that manually if needed
                    if (string.Equals(headerName, "Content-Length", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Ignore the Connection header since we'll add that manually
                    if (string.Equals(headerName, "Connection", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Write the header for each value that was found
                    string[] headerValues = Headers.GetValues(headerName);
                    if (headerValues != null && headerValues.Length > 0)
                    {
                        foreach (string headerValue in headerValues)
                            writer.WriteLine("{0}: {1}", headerName, headerValue);
                    }
                }

                // Write the Content-Length and Connection headers
                if (_content?.Length > 0)
                    writer.WriteLine("Content-Length: {0}", _content.Length);

                writer.WriteLine("Connection: close");

                // Write an empty line that marks the end of the headers
                writer.WriteLine();

                writer.Flush();
            }

            // Finally, write the body, if present
            if (_content?.Length > 0)
            {
                _content.Position = 0;
                _content.CopyTo(outputStream, 0, _content.Length);
            }
        }

        /// <summary>
        /// Gets a humanly readable status description, based on the status code.
        /// </summary>
        /// <returns></returns>
        public string GetStatusDescription()
        {
            switch (StatusCode)
            {
                case HttpStatusCode.OK:
                    return "OK";
                case HttpStatusCode.RequestUriTooLong:
                    return "Request URI Too Long";
                case HttpStatusCode.HttpVersionNotSupported:
                    return "HTTP Version Not Supported";
            }

            // Build a description from the enum string representation where a space is added before each uppercase character
            // i.e.: "BadRequest" becomes "Bad Request"

            string statusCodeString = StatusCode.ToString();

            StringBuilder descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append(statusCodeString[0]);

            for (int i = 1; i < statusCodeString.Length; i++)
            {
                char c = statusCodeString[i];

                if (char.IsUpper(c))
                    descriptionBuilder.Append(' ');

                descriptionBuilder.Append(c);
            }

            return descriptionBuilder.ToString();
        }

        /// <summary>
        /// Creates a string representation of the response's Status-Line and includes the headers if specified.
        /// </summary>
        public string ToString(bool includeHeaders)
        {
            StringBuilder builder = new StringBuilder();

            // Append Status-Line
            // A Status-Line looks like:  HTTP/1.1 200 OK
            builder.AppendFormat("{0} {1} {2}", HttpVersion, (int)StatusCode, GetStatusDescription());

            if (!includeHeaders)
                return builder.ToString();

            // Append headers
            foreach (string headerName in Headers.Keys)
            {
                string[] headerValues = Headers.GetValues(headerName);
                string headerValuesString = headerValues != null
                    ? string.Join(";", headerValues)
                    : string.Empty;
                builder.AppendFormat("\r\n{0}: {1}", headerName, headerValuesString);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Creates a string representation of the response's Status-Line and includes the headers.
        /// </summary>
        public override string ToString()
        {
            return ToString(true);
        }
    }
}