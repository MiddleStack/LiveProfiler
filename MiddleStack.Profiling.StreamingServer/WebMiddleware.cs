using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MiddleStack.Profiling.StreamingServer
{
    internal class WebMiddleware : OwinMiddleware
    {
        private readonly OwinMiddleware _next;

        public WebMiddleware(OwinMiddleware next) : base(next)
        {
            _next = next;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Method == "GET")
            {
                var relativePath = context.Request.Path == new PathString("/")
                    ? "index.html"
                    : Regex.Replace(context.Request.Path.Value.Replace("/", @"\"), @"^\\", String.Empty);

                var directory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

                var file = new FileInfo(Path.Combine(Path.Combine(directory.FullName, "WebAssets"), relativePath));

                if (file.Exists)
                {
                    context.Response.ContentType = GetContentTypeForResource(file.Name);
                    using (var stream = file.OpenRead())
                    {
                        await stream.CopyToAsync(context.Response.Body).ConfigureAwait(false);
                    }
                }
                else
                {
                    await _next.Invoke(context);
                }
            }
        }

        private string GetContentTypeForResource(string fileName)
        {
            var mimeType = MimeMapping.GetMimeMapping(fileName);

            return mimeType;
        }
    }
}
