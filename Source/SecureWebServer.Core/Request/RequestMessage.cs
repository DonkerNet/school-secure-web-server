using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using SecureWebServer.Core.Entities;
using SecureWebServer.Core.Extensions;

namespace SecureWebServer.Core.Request
{
    public class RequestMessage
    {
        #region Read-only properties

        public string HttpMethod { get; }
        public string Path { get; }
        public NameValueCollection QueryString { get; }
        public string HttpVersion { get; }
        public NameValueCollection Headers { get; }
        public NameValueCollection FormData { get; }

        #endregion

        public User User { get; set; }

        private RequestMessage(string httpMethod, string path, NameValueCollection queryString, string httpVersion, NameValueCollection headers, NameValueCollection formData)
        {
            HttpMethod = httpMethod;
            Path = path;
            QueryString = queryString;
            HttpVersion = httpVersion;
            Headers = headers;
            FormData = formData;
        }

        public static RequestMessage Create(Stream inputStream)
        {
            NameValueCollection headers = new NameValueCollection();
            NameValueCollection queryString = new NameValueCollection();
            NameValueCollection formData = new NameValueCollection();
            string httpMethod = null;
            string path = null;
            string httpVersion = null;
            Stream content = null;

            bool readRequestLine = false;

            foreach (string header in ReadHeaders(inputStream))
            {
                if (!readRequestLine)
                {
                    if (string.IsNullOrEmpty(header))
                        return null;

                    string[] requestLineParts = header.Split(new[] { ' ' }, 3);

                    if (requestLineParts.Length != 3)
                        throw new RequestException(HttpStatusCode.BadRequest, "Request-Line malformed.");

                    httpMethod = requestLineParts[0].ToUpperInvariant();
                    httpVersion = requestLineParts[2].ToUpperInvariant();

                    string[] requestUriParts = requestLineParts[1].Trim('/').Split('?');
                    path = requestUriParts[0];

                    if (requestUriParts.Length > 1)
                        ParseQueryString(queryString, requestUriParts[1]);

                    readRequestLine = true;
                    continue;
                }

                string[] headerParts = header.Split(new[] { ':' }, 2);

                string headerName = headerParts[0].Trim();
                string headerValue = headerParts.Length > 1 ? headerParts[1].Trim() : string.Empty;

                headers.Add(headerName, headerValue);
            }

            if (!readRequestLine)
                return null;
            
            // Read body if the Content-Length header is present
            // TODO: what if there is a body but no Content-Length header?

            string contentLengthString = headers["Content-Length"];

            if (!string.IsNullOrEmpty(contentLengthString))
            {
                int contentLength;
                if (!int.TryParse(contentLengthString, out contentLength))
                    throw new RequestException(HttpStatusCode.BadRequest, "Invalid Content-Length header value.");

                // Copy the content to a new stream and reset the streams position
                content = new MemoryStream();
                inputStream.CopyTo(content, 0, contentLength);
                content.Position = 0L;

                // Read and parse formdata if the body contains formdata

                ParseFormData(headers, formData, content);
            }

            return new RequestMessage(httpMethod, path, queryString, httpVersion, headers, formData);
        }

        /// <summary>
        /// Enumerates through all the headers (including the Request-Line).
        /// </summary>
        private static IEnumerable<string> ReadHeaders(Stream inputStream)
        {
            // ASCII uses 1 byte per character

            // We read one character at a time
            // We don't use a StreamReader since it buffers more characters than we want

            using (BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true))
            {
                StringBuilder builder = new StringBuilder();

                while (true)
                {
                    char c;

                    try
                    {
                        c = reader.ReadChar();
                    }
                    catch (EndOfStreamException)
                    {
                        yield break;
                    }

                    if (c == '\n')
                    {
                        string header = builder.ToString(0, builder.Length - 1);

                        if (string.IsNullOrEmpty(header))
                            yield break;

                        yield return header;

                        builder = new StringBuilder();
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
            }
        }

        private static void ParseQueryString(NameValueCollection queryStringCollection, string queryString)
        {
            foreach (string field in queryString.Split(new []{ '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fieldParts = field.Split('=');
                string fieldName = fieldParts[0];
                string fieldValue = fieldParts.Length > 1 ? HttpUtility.UrlDecode(fieldParts[1]) : string.Empty;
                queryStringCollection.Add(fieldName, fieldValue);
            }
        }

        /// <summary>
        /// Checks if the request has a body containg form data, parses and places it into the form data collection.
        /// </summary>
        private static void ParseFormData(NameValueCollection headers, NameValueCollection formData, Stream content)
        {
            string contentType = headers["Content-Type"];

            if (!string.IsNullOrEmpty(contentType))
            {
                string[] contentTypeParts = contentType.Split(';');

                if (string.Equals(contentTypeParts[0], "application/x-www-form-urlencoded", StringComparison.InvariantCultureIgnoreCase)) // TODO: multipart/form-data support?
                {
                    // Parse charset if present or use default

                    string charsetPart = contentTypeParts.FirstOrDefault(ctp => ctp.StartsWith("charset", StringComparison.InvariantCultureIgnoreCase));
                    string charset = charsetPart != null && charsetPart.Contains('=')
                        ? charsetPart.Split(new[] { '=' }, 2).Last()
                        : null;

                    if (string.IsNullOrEmpty(charset))
                        charset = "ISO-8859-1";

                    Encoding encoding = Encoding.GetEncoding(charset);

                    // Convert the content stream to a string
                    byte[] contentBytes = new byte[content.Length];
                    content.Read(contentBytes, 0, contentBytes.Length);
                    content.Position = 0L;
                    string formDataString = encoding.GetString(contentBytes);

                    // Extract the form data fields and put them in the header collection
                    foreach (string formDataField in formDataString.Split('&').Where(i => !string.IsNullOrEmpty(i)))
                    {
                        string[] itemParts = formDataField.Split(new[] { '=' }, 2);

                        string itemName = HttpUtility.UrlDecode(itemParts[0].Trim());
                        string itemValue = itemParts.Length > 1 ? HttpUtility.UrlDecode(itemParts[1].Trim()) : string.Empty;

                        formData.Add(itemName, itemValue);
                    }
                }
            }
        }

        public string ToString(bool includeHeaders)
        {
            StringBuilder builder = new StringBuilder();

            // Append Request-Line
            builder.AppendFormat("{0} /{1}", HttpMethod, Path);

            if (QueryString.Count > 0)
            {
                builder.Append('?');

                int fieldCount = 0;

                foreach (string fieldName in QueryString.Keys)
                {
                    if (fieldCount > 0)
                        builder.Append('&');

                    string[] fieldValues = QueryString.GetValues(fieldName);

                    if (fieldValues != null && fieldValues.Length > 0)
                        builder.AppendFormat("{0}={1}", fieldName, string.Join(",", fieldValues.Select(HttpUtility.UrlEncode)));
                    else
                        builder.Append(fieldName);

                    ++fieldCount;
                }
            }

            builder.AppendFormat(" {0}", HttpVersion);

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

        public override string ToString()
        {
            return ToString(true);
        }
    }
}