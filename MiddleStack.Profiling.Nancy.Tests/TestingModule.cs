using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Responses.Negotiation;

namespace MiddleStack.Profiling.Nancy.Tests
{
    public class TestingModule: NancyModule
    {
        public const string GetPathPrefix = "/BF0CF237612B451EADBA14B7C11B3DF4";
        public const string GetPath = GetPathPrefix + "/{StatusCode:int}";
        public const string GetContentType = "application/json";
        public const string GetContent = "D91ECCA259FE49E09423B37A115BFFB6";
        public const string PostPathPrefix = "/227AF4C89DBF46E299A00E5F60A5C3FD";
        public const string PostPath = PostPathPrefix + "/{StatusCode:int}";
        public const string PostContentType = "EDEED7F80A3C45859C57FE66E71C9CDD/07430A39B74946E89CBF75832B13A524";
        public const string PostContent = "1187DC748B34423BA93292593332C347";

        public TestingModule()
        {
            this.UseLiveProfiler(correlationIdGetter: context => context.Request.Query.CorrelationId);

            Get[GetPath] = _ =>
            {
                if (Context.Request.Query.Throw)
                {
                    throw new Exception("Failure!");
                }

                return Negotiate.WithStatusCode((HttpStatusCode) (int) _.StatusCode)
                    .WithContentType(GetContentType)
                    .WithModel(GetContent);
            };

            Post[PostPath] = _ => Negotiate.WithStatusCode((HttpStatusCode)(int)_.StatusCode)
                .WithContentType(PostContentType)
                .WithModel(PostContent);
        }
    }
}
