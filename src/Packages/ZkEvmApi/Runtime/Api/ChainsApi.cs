/*
 * Immutable zkEVM API
 *
 * Immutable Multi Rollup API
 *
 * The version of the OpenAPI document: 1.0.0
 * Contact: support@immutable.com
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Mime;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Immutable.Api.ZkEvm.Api
{

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IChainsApiSync : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// List supported chains
        /// </summary>
        /// <remarks>
        /// List supported chains
        /// </remarks>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <returns>ListChainsResult</returns>
        ListChainsResult ListChains(string? pageCursor = default(string?), int? pageSize = default(int?));

        /// <summary>
        /// List supported chains
        /// </summary>
        /// <remarks>
        /// List supported chains
        /// </remarks>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <returns>ApiResponse of ListChainsResult</returns>
        ApiResponse<ListChainsResult> ListChainsWithHttpInfo(string? pageCursor = default(string?), int? pageSize = default(int?));
        #endregion Synchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IChainsApiAsync : IApiAccessor
    {
        #region Asynchronous Operations
        /// <summary>
        /// List supported chains
        /// </summary>
        /// <remarks>
        /// List supported chains
        /// </remarks>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ListChainsResult</returns>
        System.Threading.Tasks.Task<ListChainsResult> ListChainsAsync(string? pageCursor = default(string?), int? pageSize = default(int?), System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken));

        /// <summary>
        /// List supported chains
        /// </summary>
        /// <remarks>
        /// List supported chains
        /// </remarks>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (ListChainsResult)</returns>
        System.Threading.Tasks.Task<ApiResponse<ListChainsResult>> ListChainsWithHttpInfoAsync(string? pageCursor = default(string?), int? pageSize = default(int?), System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken));
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IChainsApi : IChainsApiSync, IChainsApiAsync
    {

    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class ChainsApi : IDisposable, IChainsApi
    {
        private Immutable.Api.ZkEvm.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainsApi"/> class.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <returns></returns>
        public ChainsApi() : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainsApi"/> class.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <param name="basePath">The target service's base path in URL format.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public ChainsApi(string basePath)
        {
            this.Configuration = Immutable.Api.ZkEvm.Client.Configuration.MergeConfigurations(
                Immutable.Api.ZkEvm.Client.GlobalConfiguration.Instance,
                new Immutable.Api.ZkEvm.Client.Configuration { BasePath = basePath }
            );
            this.ApiClient = new Immutable.Api.ZkEvm.Client.ApiClient(this.Configuration.BasePath);
            this.Client =  this.ApiClient;
            this.AsynchronousClient = this.ApiClient;
            this.ExceptionFactory = Immutable.Api.ZkEvm.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainsApi"/> class using Configuration object.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <param name="configuration">An instance of Configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public ChainsApi(Immutable.Api.ZkEvm.Client.Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Configuration = Immutable.Api.ZkEvm.Client.Configuration.MergeConfigurations(
                Immutable.Api.ZkEvm.Client.GlobalConfiguration.Instance,
                configuration
            );
            this.ApiClient = new Immutable.Api.ZkEvm.Client.ApiClient(this.Configuration.BasePath);
            this.Client = this.ApiClient;
            this.AsynchronousClient = this.ApiClient;
            ExceptionFactory = Immutable.Api.ZkEvm.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainsApi"/> class
        /// using a Configuration object and client instance.
        /// </summary>
        /// <param name="client">The client interface for synchronous API access.</param>
        /// <param name="asyncClient">The client interface for asynchronous API access.</param>
        /// <param name="configuration">The configuration object.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ChainsApi(Immutable.Api.ZkEvm.Client.ISynchronousClient client, Immutable.Api.ZkEvm.Client.IAsynchronousClient asyncClient, Immutable.Api.ZkEvm.Client.IReadableConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (asyncClient == null) throw new ArgumentNullException("asyncClient");
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Client = client;
            this.AsynchronousClient = asyncClient;
            this.Configuration = configuration;
            this.ExceptionFactory = Immutable.Api.ZkEvm.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Disposes resources if they were created by us
        /// </summary>
        public void Dispose()
        {
            this.ApiClient?.Dispose();
        }

        /// <summary>
        /// Holds the ApiClient if created
        /// </summary>
        public Immutable.Api.ZkEvm.Client.ApiClient ApiClient { get; set; } = null;

        /// <summary>
        /// The client for accessing this underlying API asynchronously.
        /// </summary>
        public Immutable.Api.ZkEvm.Client.IAsynchronousClient AsynchronousClient { get; set; }

        /// <summary>
        /// The client for accessing this underlying API synchronously.
        /// </summary>
        public Immutable.Api.ZkEvm.Client.ISynchronousClient Client { get; set; }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public string GetBasePath()
        {
            return this.Configuration.BasePath;
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public Immutable.Api.ZkEvm.Client.IReadableConfiguration Configuration { get; set; }

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public Immutable.Api.ZkEvm.Client.ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        /// <summary>
        /// List supported chains List supported chains
        /// </summary>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <returns>ListChainsResult</returns>
        public ListChainsResult ListChains(string? pageCursor = default(string?), int? pageSize = default(int?))
        {
            Immutable.Api.ZkEvm.Client.ApiResponse<ListChainsResult> localVarResponse = ListChainsWithHttpInfo(pageCursor, pageSize);
            return localVarResponse.Data;
        }

        /// <summary>
        /// List supported chains List supported chains
        /// </summary>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <returns>ApiResponse of ListChainsResult</returns>
        public Immutable.Api.ZkEvm.Client.ApiResponse<ListChainsResult> ListChainsWithHttpInfo(string? pageCursor = default(string?), int? pageSize = default(int?))
        {
            Immutable.Api.ZkEvm.Client.RequestOptions localVarRequestOptions = new Immutable.Api.ZkEvm.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = Immutable.Api.ZkEvm.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = Immutable.Api.ZkEvm.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            if (pageCursor != null)
            {
                localVarRequestOptions.QueryParameters.Add(Immutable.Api.ZkEvm.Client.ClientUtils.ParameterToMultiMap("", "page_cursor", pageCursor));
            }
            if (pageSize != null)
            {
                localVarRequestOptions.QueryParameters.Add(Immutable.Api.ZkEvm.Client.ClientUtils.ParameterToMultiMap("", "page_size", pageSize));
            }


            // make the HTTP request
            var localVarResponse = this.Client.Get<ListChainsResult>("/v1/chains", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ListChains", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// List supported chains List supported chains
        /// </summary>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ListChainsResult</returns>
        public async System.Threading.Tasks.Task<ListChainsResult> ListChainsAsync(string? pageCursor = default(string?), int? pageSize = default(int?), System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
        {
            var task = ListChainsWithHttpInfoAsync(pageCursor, pageSize, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            Immutable.Api.ZkEvm.Client.ApiResponse<ListChainsResult> localVarResponse = await task.ConfigureAwait(false);
#else
            Immutable.Api.ZkEvm.Client.ApiResponse<ListChainsResult> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// List supported chains List supported chains
        /// </summary>
        /// <exception cref="Immutable.Api.ZkEvm.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="pageCursor">Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional)</param>
        /// <param name="pageSize">Maximum number of items to return (optional, default to 100)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (ListChainsResult)</returns>
        public async System.Threading.Tasks.Task<Immutable.Api.ZkEvm.Client.ApiResponse<ListChainsResult>> ListChainsWithHttpInfoAsync(string? pageCursor = default(string?), int? pageSize = default(int?), System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
        {

            Immutable.Api.ZkEvm.Client.RequestOptions localVarRequestOptions = new Immutable.Api.ZkEvm.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = Immutable.Api.ZkEvm.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = Immutable.Api.ZkEvm.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            if (pageCursor != null)
            {
                localVarRequestOptions.QueryParameters.Add(Immutable.Api.ZkEvm.Client.ClientUtils.ParameterToMultiMap("", "page_cursor", pageCursor));
            }
            if (pageSize != null)
            {
                localVarRequestOptions.QueryParameters.Add(Immutable.Api.ZkEvm.Client.ClientUtils.ParameterToMultiMap("", "page_size", pageSize));
            }


            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<ListChainsResult>("/v1/chains", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ListChains", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

    }
}
