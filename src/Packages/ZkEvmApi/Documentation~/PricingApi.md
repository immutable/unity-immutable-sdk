# Immutable.Api.ZkEvm.Api.PricingApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**QuotesForNFTs**](PricingApi.md#quotesfornfts) | **GET** /experimental/chains/{chain_name}/quotes/{contract_address}/nfts | Experimental: Get pricing data for a list of token ids |
| [**QuotesForStacks**](PricingApi.md#quotesforstacks) | **GET** /experimental/chains/{chain_name}/quotes/{contract_address}/stacks | Experimental: Get pricing data for a list of stack ids |

<a id="quotesfornfts"></a>
# **QuotesForNFTs**
> QuotesForNFTsResult QuotesForNFTs (string chainName, string contractAddress, List<string> tokenId, string? pageCursor = null)

Experimental: Get pricing data for a list of token ids

![Experimental](https://img.shields.io/badge/status-experimental-yellow) Get pricing data for a list of token ids

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class QuotesForNFTsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new PricingApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = "contractAddress_example";  // string | Contract address for collection that these token ids are on
            var tokenId = new List<string>(); // List<string> | List of token ids to get pricing data for
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // Experimental: Get pricing data for a list of token ids
                QuotesForNFTsResult result = apiInstance.QuotesForNFTs(chainName, contractAddress, tokenId, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PricingApi.QuotesForNFTs: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the QuotesForNFTsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Experimental: Get pricing data for a list of token ids
    ApiResponse<QuotesForNFTsResult> response = apiInstance.QuotesForNFTsWithHttpInfo(chainName, contractAddress, tokenId, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PricingApi.QuotesForNFTsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string** | Contract address for collection that these token ids are on |  |
| **tokenId** | [**List&lt;string&gt;**](string.md) | List of token ids to get pricing data for |  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**QuotesForNFTsResult**](QuotesForNFTsResult.md)

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

<a id="quotesforstacks"></a>
# **QuotesForStacks**
> QuotesForStacksResult QuotesForStacks (string chainName, string contractAddress, List<Guid> stackId, string? pageCursor = null)

Experimental: Get pricing data for a list of stack ids

![Experimental](https://img.shields.io/badge/status-experimental-yellow) Get pricing data for a list of stack ids

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class QuotesForStacksExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new PricingApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = "contractAddress_example";  // string | Contract address for collection that these stacks are on
            var stackId = new List<Guid>(); // List<Guid> | List of stack ids to get pricing data for
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // Experimental: Get pricing data for a list of stack ids
                QuotesForStacksResult result = apiInstance.QuotesForStacks(chainName, contractAddress, stackId, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PricingApi.QuotesForStacks: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the QuotesForStacksWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Experimental: Get pricing data for a list of stack ids
    ApiResponse<QuotesForStacksResult> response = apiInstance.QuotesForStacksWithHttpInfo(chainName, contractAddress, stackId, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PricingApi.QuotesForStacksWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string** | Contract address for collection that these stacks are on |  |
| **stackId** | [**List&lt;Guid&gt;**](Guid.md) | List of stack ids to get pricing data for |  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**QuotesForStacksResult**](QuotesForStacksResult.md)

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

