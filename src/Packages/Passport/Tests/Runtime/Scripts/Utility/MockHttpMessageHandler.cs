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
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Debug.Log("Request made: " + request.RequestUri);
            Requests.Add(request);
            if (Requests.Count <= Responses.Count)
            {
                return Task.FromResult(Responses[Requests.Count - 1]);
            }

            return Task.FromException<HttpResponseMessage>(new Exception($"No response for this request: {request.RequestUri}"));
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