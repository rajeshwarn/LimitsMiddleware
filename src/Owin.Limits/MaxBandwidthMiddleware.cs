﻿namespace Owin.Limits
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Owin.Limits.Annotations;

    [UsedImplicitly]
    internal class MaxBandwidthMiddleware : MiddlewareBase
    {
        private readonly MaxBandwidthOptions _options;

        public MaxBandwidthMiddleware(Func<IDictionary<string, object>, Task> next, MaxBandwidthOptions options)
            : base(next.ToAppFunc(), options.Tracer)
        {
            _options = options;
        }

        protected override Task InvokeInternal(AppFunc next, IDictionary<string, object> environment)
        {
            environment.MustNotNull("environment");
            
            var context = new OwinContext(environment);
            Stream requestBodyStream = context.Request.Body ?? Stream.Null;
            Stream responseBodyStream = context.Response.Body;
            int maxBytesPerSecond = _options.GetMaxBytesPerSecond();
            if (maxBytesPerSecond < 0)
            {
                maxBytesPerSecond = 0;
            }
            _options.Tracer.AsVerbose("Configure streams to be limited.");
            context.Request.Body = new ThrottledStream(requestBodyStream, maxBytesPerSecond);
            context.Response.Body = new ThrottledStream(responseBodyStream, maxBytesPerSecond);

            //TODO consider SendFile interception
            _options.Tracer.AsVerbose("With configured limit forwarded.");
            return next(environment);
        }
    }
}