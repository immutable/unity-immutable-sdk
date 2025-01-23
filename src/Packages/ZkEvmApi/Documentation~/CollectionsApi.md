# Immutable.Api.ZkEvm.Api.CollectionsApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetCollection**](CollectionsApi.md#getcollection) | **GET** /v1/chains/{chain_name}/collections/{contract_address} | Get collection by contract address |
| [**ListCollections**](CollectionsApi.md#listcollections) | **GET** /v1/chains/{chain_name}/collections | List all collections |
| [**ListCollectionsByNFTOwner**](CollectionsApi.md#listcollectionsbynftowner) | **GET** /v1/chains/{chain_name}/accounts/{account_address}/collections | List collections by NFT owner |
| [**RefreshCollectionMetadata**](CollectionsApi.md#refreshcollectionmetadata) | **POST** /v1/chains/{chain_name}/collections/{contract_address}/refresh-metadata | Refresh collection metadata |

<a id="getcollection"></a>
# **GetCollection**
> GetCollectionResult GetCollection (string contractAddress, string chainName)

Get collection by contract address

Get collection by contract address

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetCollectionExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new CollectionsApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address contract
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain

            try
            {
                // Get collection by contract address
                GetCollectionResult result = apiInstance.GetCollection(contractAddress, chainName);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling CollectionsApi.GetCollection: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetCollectionWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get collection by contract address
    ApiResponse<GetCollectionResult> response = apiInstance.GetCollectionWithHttpInfo(contractAddress, chainName);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling CollectionsApi.GetCollectionWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address contract |  |
| **chainName** | **string** | The name of chain |  |

### Return type

[**GetCollectionResult**](GetCollectionResult.md)

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

<a id="listcollections"></a>
# **ListCollections**
> ListCollectionsResult ListCollections (string chainName, List<string>? contractAddress = null, List<AssetVerificationStatus>? verificationStatus = null, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

List all collections

List all collections

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListCollectionsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new CollectionsApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = new List<string>?(); // List<string>? | List of contract addresses to filter by (optional) 
            var verificationStatus = new List<AssetVerificationStatus>?(); // List<AssetVerificationStatus>? | List of verification status to filter by (optional) 
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List all collections
                ListCollectionsResult result = apiInstance.ListCollections(chainName, contractAddress, verificationStatus, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling CollectionsApi.ListCollections: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListCollectionsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all collections
    ApiResponse<ListCollectionsResult> response = apiInstance.ListCollectionsWithHttpInfo(chainName, contractAddress, verificationStatus, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling CollectionsApi.ListCollectionsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | [**List&lt;string&gt;?**](string.md) | List of contract addresses to filter by | [optional]  |
| **verificationStatus** | [**List&lt;AssetVerificationStatus&gt;?**](AssetVerificationStatus.md) | List of verification status to filter by | [optional]  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListCollectionsResult**](ListCollectionsResult.md)

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

<a id="listcollectionsbynftowner"></a>
# **ListCollectionsByNFTOwner**
> ListCollectionsResult ListCollectionsByNFTOwner (string accountAddress, string chainName, string? pageCursor = null, int? pageSize = null)

List collections by NFT owner

List collections by NFT owner account address

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListCollectionsByNFTOwnerExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new CollectionsApi(config);
            var accountAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string | Account address
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List collections by NFT owner
                ListCollectionsResult result = apiInstance.ListCollectionsByNFTOwner(accountAddress, chainName, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling CollectionsApi.ListCollectionsByNFTOwner: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListCollectionsByNFTOwnerWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List collections by NFT owner
    ApiResponse<ListCollectionsResult> response = apiInstance.ListCollectionsByNFTOwnerWithHttpInfo(accountAddress, chainName, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling CollectionsApi.ListCollectionsByNFTOwnerWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **accountAddress** | **string** | Account address |  |
| **chainName** | **string** | The name of chain |  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListCollectionsResult**](ListCollectionsResult.md)

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

<a id="refreshcollectionmetadata"></a>
# **RefreshCollectionMetadata**
> RefreshCollectionMetadataResult RefreshCollectionMetadata (string contractAddress, string chainName, RefreshCollectionMetadataRequest refreshCollectionMetadataRequest)

Refresh collection metadata

Refresh collection metadata

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class RefreshCollectionMetadataExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure Bearer token for authorization: BearerAuthWithClient
            config.AccessToken = "YOUR_BEARER_TOKEN";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");
            // Configure Bearer token for authorization: BearerAuth
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new CollectionsApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address contract
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var refreshCollectionMetadataRequest = new RefreshCollectionMetadataRequest(); // RefreshCollectionMetadataRequest | The request body

            try
            {
                // Refresh collection metadata
                RefreshCollectionMetadataResult result = apiInstance.RefreshCollectionMetadata(contractAddress, chainName, refreshCollectionMetadataRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling CollectionsApi.RefreshCollectionMetadata: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RefreshCollectionMetadataWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Refresh collection metadata
    ApiResponse<RefreshCollectionMetadataResult> response = apiInstance.RefreshCollectionMetadataWithHttpInfo(contractAddress, chainName, refreshCollectionMetadataRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling CollectionsApi.RefreshCollectionMetadataWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address contract |  |
| **chainName** | **string** | The name of chain |  |
| **refreshCollectionMetadataRequest** | [**RefreshCollectionMetadataRequest**](RefreshCollectionMetadataRequest.md) | The request body |  |

### Return type

[**RefreshCollectionMetadataResult**](RefreshCollectionMetadataResult.md)

### Authorization

[BearerAuthWithClient](../README.md#BearerAuthWithClient), [ImmutableApiKey](../README.md#ImmutableApiKey), [BearerAuth](../README.md#BearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | 200 response |  -  |
| **400** | Bad Request (400) |  -  |
| **401** | Unauthorised Request (401) |  -  |
| **403** | Forbidden Request (403) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

