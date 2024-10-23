# Immutable.Api.ZkEvm.Api.TokensApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetERC20Token**](TokensApi.md#geterc20token) | **GET** /v1/chains/{chain_name}/tokens/{contract_address} | Get single ERC20 token |
| [**ListERC20Tokens**](TokensApi.md#listerc20tokens) | **GET** /v1/chains/{chain_name}/tokens | List ERC20 tokens |

<a id="geterc20token"></a>
# **GetERC20Token**
> GetTokenResult GetERC20Token (string contractAddress, string chainName)

Get single ERC20 token

Get single ERC20 token

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetERC20TokenExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new TokensApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var chainName = "chainName_example";  // string | The name of chain

            try
            {
                // Get single ERC20 token
                GetTokenResult result = apiInstance.GetERC20Token(contractAddress, chainName);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TokensApi.GetERC20Token: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetERC20TokenWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get single ERC20 token
    ApiResponse<GetTokenResult> response = apiInstance.GetERC20TokenWithHttpInfo(contractAddress, chainName);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TokensApi.GetERC20TokenWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **chainName** | **string** | The name of chain |  |

### Return type

[**GetTokenResult**](GetTokenResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listerc20tokens"></a>
# **ListERC20Tokens**
> ListTokensResult ListERC20Tokens (string chainName, DateTime? fromUpdatedAt = null, List<AssetVerificationStatus>? verificationStatus = null, bool? isCanonical = null, string? pageCursor = null, int? pageSize = null)

List ERC20 tokens

List ERC20 tokens

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListERC20TokensExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new TokensApi(config);
            var chainName = "chainName_example";  // string | The name of chain
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var verificationStatus = new List<AssetVerificationStatus>?(); // List<AssetVerificationStatus>? | List of verification status to filter by (optional) 
            var isCanonical = true;  // bool? | [Experimental - Canonical token data may be updated] Filter by canonical or non-canonical tokens. (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List ERC20 tokens
                ListTokensResult result = apiInstance.ListERC20Tokens(chainName, fromUpdatedAt, verificationStatus, isCanonical, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TokensApi.ListERC20Tokens: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListERC20TokensWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List ERC20 tokens
    ApiResponse<ListTokensResult> response = apiInstance.ListERC20TokensWithHttpInfo(chainName, fromUpdatedAt, verificationStatus, isCanonical, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TokensApi.ListERC20TokensWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **verificationStatus** | [**List&lt;AssetVerificationStatus&gt;?**](AssetVerificationStatus.md) | List of verification status to filter by | [optional]  |
| **isCanonical** | **bool?** | [Experimental - Canonical token data may be updated] Filter by canonical or non-canonical tokens. | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListTokensResult**](ListTokensResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

