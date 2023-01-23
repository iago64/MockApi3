using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Text;
using Microsoft.VisualStudio.Web.CodeGeneration;
using static System.Net.Mime.MediaTypeNames;

namespace MockApi3.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RequestResponseLoggingMiddleware(RequestDelegate next,ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestResponseLoggingMiddleware>();
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task Invoke(HttpContext context)
        {
            await LogRequest(context);
            await LogResponse(context);
        }

        private async Task LogRequest(HttpContext context)
        {
            context.Request.EnableBuffering();
            await using var requestStream = _recyclableMemoryStreamManager.GetStream();
            await context.Request.Body.CopyToAsync(requestStream);
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"host\": \"{context.Request.Host}\",");
            sb.Append($"\"path\": \"{context.Request.Path}\",");
            sb.Append($"\"query_string\": \"{context.Request.QueryString}\",");
            sb.Append($"\"method\": \"{context.Request.Method}\",");
            sb.Append($"\"headers\": [");
            foreach (var header in context.Request.Headers)
            {
                sb.Append($"{{\"{header.Key}\": \"{header.Value}\"}},");
            }
            sb.Append("],");
            string request_body = ReadStreamInChunks(requestStream);
            sb.Append($"\"request_body\": {request_body.Replace(Environment.NewLine, "")}");
            sb.Append("}");
            _logger.LogInformation(sb.ToString());
            context.Request.Body.Position = 0;
        }
        private static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;
            stream.Seek(0, SeekOrigin.Begin);
            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);
            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;
            do
            {
                readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);
            return textWriter.ToString();
        }

        private async Task LogResponse(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;
            await _next(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"host\": \"{context.Request.Host}\",");
            sb.Append($"\"path\": \"{context.Request.Path}\",");
            sb.Append($"\"query_string\": \"{context.Request.QueryString}\",");
            sb.Append($"\"method\": \"{context.Request.Method}\",");
            sb.Append($"\"headers\": [");
            foreach (var header in context.Request.Headers)
            {
                sb.Append($"{{\"{header.Key}\": \"{header.Value}\"}},");
            }
            sb.Append("],");
            sb.Append($"\"response_body\": {text.Replace(Environment.NewLine, "")}");
            sb.Append("}");
            _logger.LogInformation(sb.ToString());
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
