# Immutable.Api.ZkEvm.Api.StacksApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ListFilters**](StacksApi.md#listfilters) | **GET** /experimental/chains/{chain_name}/search/filters/{contract_address} | Experimental: Get list of metadata attribute filters |
| [**ListStacks**](StacksApi.md#liststacks) | **GET** /experimental/chains/{chain_name}/stacks | Experimental: List NFT stacks by stack_id |
| [**SearchNFTs**](StacksApi.md#searchnfts) | **GET** /experimental/chains/{chain_name}/search/nfts | Experimental: Search NFTs |
| [**SearchStacks**](StacksApi.md#searchstacks) | **GET** /experimental/chains/{chain_name}/search/stacks | Experimental: Search NFT stacks |

<a id="listfilters"></a>
# **ListFilters**
> ListFiltersResult ListFilters (string chainName, string contractAddress)

Experimental: Get list of metadata attribute filters

![Experimental](https://img.shields.io/badge/status-experimental-yellow) Get list of metadata filters

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListFiltersExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new StacksApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string | Contract addresses for collection

            try
            {
                // Experimental: Get list of metadata attribute filters
                ListFiltersResult result = apiInstance.ListFilters(chainName, contractAddress);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling StacksApi.ListFilters: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListFiltersWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Experimental: Get list of metadata attribute filters
    ApiResponse<ListFiltersResult> response = apiInstance.ListFiltersWithHttpInfo(chainName, contractAddress);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling StacksApi.ListFiltersWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string** | Contract addresses for collection |  |

### Return type

[**ListFiltersResult**](ListFiltersResult.md)

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

<a id="liststacks"></a>
# **ListStacks**
> List&lt;StackBundle&gt; ListStacks (string chainName, List<Guid> stackId)

Experimental: List NFT stacks by stack_id

![Experimental](https://img.shields.io/badge/status-experimental-yellow) NFT stacks

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
            var apiInstance = new StacksApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var stackId = new List<Guid>(); // List<Guid> | List of stack_id to filter by

            try
            {
                // Experimental: List NFT stacks by stack_id
                List<StackBundle> result = apiInstance.ListStacks(chainName, stackId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling StacksApi.ListStacks: " + e.Message);
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
    // Experimental: List NFT stacks by stack_id
    ApiResponse<List<StackBundle>> response = apiInstance.ListStacksWithHttpInfo(chainName, stackId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling StacksApi.ListStacksWithHttpInfo: " + e.Message);
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

<a id="searchnfts"></a>
# **SearchNFTs**
> SearchNFTsResult SearchNFTs (string chainName, List<string> contractAddress, string? accountAddress = null, List<Guid>? stackId = null, bool? onlyIncludeOwnerListings = null, int? pageSize = null, string? pageCursor = null)

Experimental: Search NFTs

![Experimental](https://img.shields.io/badge/status-experimental-yellow) Search NFTs

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class SearchNFTsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new StacksApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = new List<string>(); // List<string> | List of contract addresses to filter by
            var accountAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string? | Account address to filter by (optional) 
            var stackId = new List<Guid>?(); // List<Guid>? | Filters NFTs that belong to any of these stacks (optional) 
            var onlyIncludeOwnerListings = true;  // bool? | Whether the listings should include only the owner created listings (optional) 
            var pageSize = 100;  // int? | Number of results to return per page (optional)  (default to 100)
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // Experimental: Search NFTs
                SearchNFTsResult result = apiInstance.SearchNFTs(chainName, contractAddress, accountAddress, stackId, onlyIncludeOwnerListings, pageSize, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling StacksApi.SearchNFTs: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SearchNFTsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Experimental: Search NFTs
    ApiResponse<SearchNFTsResult> response = apiInstance.SearchNFTsWithHttpInfo(chainName, contractAddress, accountAddress, stackId, onlyIncludeOwnerListings, pageSize, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling StacksApi.SearchNFTsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | [**List&lt;string&gt;**](string.md) | List of contract addresses to filter by |  |
| **accountAddress** | **string?** | Account address to filter by | [optional]  |
| **stackId** | [**List&lt;Guid&gt;?**](Guid.md) | Filters NFTs that belong to any of these stacks | [optional]  |
| **onlyIncludeOwnerListings** | **bool?** | Whether the listings should include only the owner created listings | [optional]  |
| **pageSize** | **int?** | Number of results to return per page | [optional] [default to 100] |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**SearchNFTsResult**](SearchNFTsResult.md)

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

<a id="searchstacks"></a>
# **SearchStacks**
> SearchStacksResult SearchStacks (string chainName, List<string> contractAddress, string? accountAddress = null, bool? onlyIncludeOwnerListings = null, bool? onlyIfHasActiveListings = null, string? traits = null, string? keyword = null, string? sortBy = null, int? pageSize = null, string? pageCursor = null)

Experimental: Search NFT stacks

![Experimental](https://img.shields.io/badge/status-experimental-yellow) Search NFT stacks

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class SearchStacksExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new StacksApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = new List<string>(); // List<string> | List of contract addresses to filter by
            var accountAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string? | Account address to filter by (optional) 
            var onlyIncludeOwnerListings = true;  // bool? | Whether to the listings should include only the owner created listings (optional) 
            var onlyIfHasActiveListings = true;  // bool? | Filters results to include only stacks that have a current active listing. False and 'null' return all unfiltered stacks. (optional) 
            var traits = "traits_example";  // string? | JSON encoded traits to filter by. e.g. encodeURIComponent(JSON.stringify({\"rarity\": {\"values\": [\"common\", \"rare\"], \"condition\": \"eq\"}})) (optional) 
            var keyword = sword;  // string? | Keyword to search NFT name and description. Alphanumeric characters only. (optional) 
            var sortBy = "cheapest_first";  // string? | Sort results in a specific order (optional) 
            var pageSize = 100;  // int? | Number of results to return per page (optional)  (default to 100)
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // Experimental: Search NFT stacks
                SearchStacksResult result = apiInstance.SearchStacks(chainName, contractAddress, accountAddress, onlyIncludeOwnerListings, onlyIfHasActiveListings, traits, keyword, sortBy, pageSize, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling StacksApi.SearchStacks: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SearchStacksWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Experimental: Search NFT stacks
    ApiResponse<SearchStacksResult> response = apiInstance.SearchStacksWithHttpInfo(chainName, contractAddress, accountAddress, onlyIncludeOwnerListings, onlyIfHasActiveListings, traits, keyword, sortBy, pageSize, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling StacksApi.SearchStacksWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | [**List&lt;string&gt;**](string.md) | List of contract addresses to filter by |  |
| **accountAddress** | **string?** | Account address to filter by | [optional]  |
| **onlyIncludeOwnerListings** | **bool?** | Whether to the listings should include only the owner created listings | [optional]  |
| **onlyIfHasActiveListings** | **bool?** | Filters results to include only stacks that have a current active listing. False and &#39;null&#39; return all unfiltered stacks. | [optional]  |
| **traits** | **string?** | JSON encoded traits to filter by. e.g. encodeURIComponent(JSON.stringify({\&quot;rarity\&quot;: {\&quot;values\&quot;: [\&quot;common\&quot;, \&quot;rare\&quot;], \&quot;condition\&quot;: \&quot;eq\&quot;}})) | [optional]  |
| **keyword** | **string?** | Keyword to search NFT name and description. Alphanumeric characters only. | [optional]  |
| **sortBy** | **string?** | Sort results in a specific order | [optional]  |
| **pageSize** | **int?** | Number of results to return per page | [optional] [default to 100] |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**SearchStacksResult**](SearchStacksResult.md)

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

