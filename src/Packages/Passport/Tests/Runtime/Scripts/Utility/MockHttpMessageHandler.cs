using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Immutable.Passport.Utility.Tests
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        // Requests made in order
        public List<HttpRequestMessage> Requests { get; private set; } = new List<HttpRequestMessage>();

        // Responses to return matching request order 
        public List<HttpResponseMessage> Responses { get; private set; } = new List<HttpResponseMessage>();
        public int responseDelay = 0;

        public MockHttpMessageHandler()
        {
        }

        public HttpClient ToHttpClient()
        {
            return new HttpClient(this);
        }

        /// <summary>
        /// Maps the request to the most appropriate configured response
        /// </summary>
        /// <param name="request">The request being sent</param>
        /// <param name="cancellationToken">The token used to cancel the request</param>
        /// <returns>A Task containing the future response message</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (Requests.Count <= Responses.Count)
            {
                await Task.Delay(responseDelay);
                cancellationToken.ThrowIfCancellationRequested();
                return Responses[Requests.Count - 1];
            }

            throw new Exception($"No response for this request: {request.RequestUri}");
        }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        /// <param name="disposing">true if called from Dispose(); false if called from dtor()</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}