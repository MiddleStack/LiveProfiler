using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses.Negotiation;
using Nancy.TinyIoc;

namespace MiddleStack.Profiling.Nancy.Tests
{
    internal class TestingBootstrapper: DefaultNancyBootstrapper
    {
        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides((c) =>
                {
                    c.ResponseProcessors.Remove(typeof(ViewProcessor));
                });
            }
        }
    }
}
