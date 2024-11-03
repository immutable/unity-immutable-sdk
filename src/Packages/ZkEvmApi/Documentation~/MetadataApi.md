# Immutable.Api.ZkEvm.Api.MetadataApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetMetadata**](MetadataApi.md#getmetadata) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/metadata/{metadata_id} | Get metadata by ID |
| [**ListMetadata**](MetadataApi.md#listmetadata) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/metadata | Get a list of metadata from the given contract |
| [**ListMetadataForChain**](MetadataApi.md#listmetadataforchain) | **GET** /v1/chains/{chain_name}/metadata | Get a list of metadata from the given chain |
| [**ListStacks**](MetadataApi.md#liststacks) | **GET** /v1/chains/{chain_name}/stacks | List NFT stack bundles by stack_id. Response will include Market, Listings &amp; Stack Count information for each stack |
| [**RefreshMetadataByID**](MetadataApi.md#refreshmetadatabyid) | **POST** /v1/chains/{chain_name}/collections/{contract_address}/metadata/refresh-metadata | Refresh stacked metadata |
| [**RefreshNFTMetadataByTokenID**](MetadataApi.md#refreshnftmetadatabytokenid) | **POST** /v1/chains/{chain_name}/collections/{contract_address}/nfts/refresh-metadata | Refresh NFT metadata |

<a id="getmetadata"></a>
# **GetMetadata**
> GetMetadataResult GetMetadata (string chainName, string contractAddress, Guid metadataId)

Get metadata by ID

Get metadata by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetMetadataExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new MetadataApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string | The address of metadata contract
            var metadataId = "metadataId_example";  // Guid | The id of the metadata

            try
            {
                // Get metadata by ID
                GetMetadataResult result = apiInstance.GetMetadata(chainName, contractAddress, metadataId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MetadataApi.GetMetadata: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMetadataWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get metadata by ID
    ApiResponse<GetMetadataResult> response = apiInstance.GetMetadataWithHttpInfo(chainName, contractAddress, metadataId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MetadataApi.GetMetadataWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string** | The address of metadata contract |  |
| **metadataId** | **Guid** | The id of the metadata |  |

### Return type

[**GetMetadataResult**](GetMetadataResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | 200 response |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listmetadata"></a>
# **ListMetadata**
> ListMetadataResult ListMetadata (string chainName, string contractAddress, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

Get a list of metadata from the given contract

Get a list of metadata from the given contract

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListMetadataExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new MetadataApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string | The address of metadata contract
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // Get a list of metadata from the given contract
                ListMetadataResult result = apiInstance.ListMetadata(chainName, contractAddress, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MetadataApi.ListMetadata: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListMetadataWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a list of metadata from the given contract
    ApiResponse<ListMetadataResult> response = apiInstance.ListMetadataWithHttpInfo(chainName, contractAddress, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MetadataApi.ListMetadataWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string** | The address of metadata contract |  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListMetadataResult**](ListMetadataResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | 200 response |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listmetadataforchain"></a>
# **ListMetadataForChain**
> ListMetadataResult ListMetadataForChain (string chainName, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

Get a list of metadata from the given chain

Get a list of metadata from the given chain

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListMetadataForChainExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new MetadataApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // Get a list of metadata from the given chain
                ListMetadataResult result = apiInstance.ListMetadataForChain(chainName, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MetadataApi.ListMetadataForChain: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListMetadataForChainWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a list of metadata from the given chain
    ApiResponse<ListMetadataResult> response = apiInstance.ListMetadataForChainWithHttpInfo(chainName, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MetadataApi.ListMetadataForChainWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListMetadataResult**](ListMetadataResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | 200 response |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="liststacks"></a>
# **ListStacks**
> List&lt;StackBundle&gt; ListStacks (string chainName, List<Guid> stackId)

List NFT stack bundles by stack_id. Response will include Market, Listings & Stack Count information for each stack

List NFT stack bundles by stack_id. This endpoint functions similarly to `ListMetadataByID` but extends the response to include Market, Listings & Stack Count information for each stack.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListStacksExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new MetadataApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var stackId = new List<Guid>(); // List<Guid> | List of stack_id to filter by

            try
            {
                // List NFT stack bundles by stack_id. Response will include Market, Listings & Stack Count information for each stack
                List<StackBundle> result = apiInstance.ListStacks(chainName, stackId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MetadataApi.ListStacks: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListStacksWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List NFT stack bundles by stack_id. Response will include Market, Listings & Stack Count information for each stack
    ApiResponse<List<StackBundle>> response = apiInstance.ListStacksWithHttpInfo(chainName, stackId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MetadataApi.ListStacksWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **stackId** | [**List&lt;Guid&gt;**](Guid.md) | List of stack_id to filter by |  |

### Return type

[**List&lt;StackBundle&gt;**](StackBundle.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | 200 response |  -  |
| **400** | Bad Request (400) |  -  |
| **401** | Unauthorised Request (401) |  -  |
| **403** | Forbidden Request (403) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **429** | Too Many Requests (429) |  * Retry-After -  <br>  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="refreshmetadatabyid"></a>
# **RefreshMetadataByID**
> MetadataRefreshRateLimitResult RefreshMetadataByID (string chainName, string contractAddress, RefreshMetadataByIDRequest refreshMetadataByIDRequest)

Refresh stacked metadata

Refresh stacked metadata

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class RefreshMetadataByIDExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");

            var apiInstance = new MetadataApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = "contractAddress_example";  // string | Contract address
            var refreshMetadataByIDRequest = new RefreshMetadataByIDRequest(); // RefreshMetadataByIDRequest | NFT Metadata Refresh Request

            try
            {
                // Refresh stacked metadata
                MetadataRefreshRateLimitResult result = apiInstance.RefreshMetadataByID(chainName, contractAddress, refreshMetadataByIDRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MetadataApi.RefreshMetadataByID: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RefreshMetadataByIDWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Refresh stacked metadata
    ApiResponse<MetadataRefreshRateLimitResult> response = apiInstance.RefreshMetadataByIDWithHttpInfo(chainName, contractAddress, refreshMetadataByIDRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MetadataApi.RefreshMetadataByIDWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string** | Contract address |  |
| **refreshMetadataByIDRequest** | [**RefreshMetadataByIDRequest**](RefreshMetadataByIDRequest.md) | NFT Metadata Refresh Request |  |

### Return type

[**MetadataRefreshRateLimitResult**](MetadataRefreshRateLimitResult.md)

### Authorization

[ImmutableApiKey](../README.md#ImmutableApiKey)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Accepted |  * imx-refreshes-limit -  <br>  * imx-refresh-limit-reset -  <br>  * imx-remaining-refreshes -  <br>  * retry-after -  <br>  |
| **400** | Bad Request (400) |  -  |
| **401** | Unauthorised Request (401) |  -  |
| **403** | Forbidden Request (403) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **429** | Too Many Metadata refreshes (429) |  * imx-refreshes-limit -  <br>  * imx-refresh-limit-reset -  <br>  * imx-remaining-refreshes -  <br>  * Retry-After -  <br>  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="refreshnftmetadatabytokenid"></a>
# **RefreshNFTMetadataByTokenID**
> MetadataRefreshRateLimitResult RefreshNFTMetadataByTokenID (string contractAddress, string chainName, RefreshNFTMetadataByTokenIDRequest refreshNFTMetadataByTokenIDRequest)

Refresh NFT metadata

Refresh NFT metadata

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class RefreshNFTMetadataByTokenIDExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");

            var apiInstance = new MetadataApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var chainName = "chainName_example";  // string | The name of chain
            var refreshNFTMetadataByTokenIDRequest = new RefreshNFTMetadataByTokenIDRequest(); // RefreshNFTMetadataByTokenIDRequest | the request body

            try
            {
                // Refresh NFT metadata
                MetadataRefreshRateLimitResult result = apiInstance.RefreshNFTMetadataByTokenID(contractAddress, chainName, refreshNFTMetadataByTokenIDRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MetadataApi.RefreshNFTMetadataByTokenID: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RefreshNFTMetadataByTokenIDWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Refresh NFT metadata
    ApiResponse<MetadataRefreshRateLimitResult> response = apiInstance.RefreshNFTMetadataByTokenIDWithHttpInfo(contractAddress, chainName, refreshNFTMetadataByTokenIDRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MetadataApi.RefreshNFTMetadataByTokenIDWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **chainName** | **string** | The name of chain |  |
| **refreshNFTMetadataByTokenIDRequest** | [**RefreshNFTMetadataByTokenIDRequest**](RefreshNFTMetadataByTokenIDRequest.md) | the request body |  |

### Return type

[**MetadataRefreshRateLimitResult**](MetadataRefreshRateLimitResult.md)

### Authorization

[ImmutableApiKey](../README.md#ImmutableApiKey)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Accepted |  * imx-refreshes-limit -  <br>  * imx-refresh-limit-reset -  <br>  * imx-remaining-refreshes -  <br>  * retry-after -  <br>  |
| **400** | Bad Request (400) |  -  |
| **401** | Unauthorised Request (401) |  -  |
| **403** | Forbidden Request (403) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **429** | Too Many Metadata refreshes (429) |  * imx-refreshes-limit -  <br>  * imx-refresh-limit-reset -  <br>  * imx-remaining-refreshes -  <br>  * Retry-After -  <br>  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

