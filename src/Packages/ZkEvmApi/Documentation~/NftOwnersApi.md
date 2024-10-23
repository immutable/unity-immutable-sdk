# Immutable.Api.ZkEvm.Api.NftOwnersApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ListAllNFTOwners**](NftOwnersApi.md#listallnftowners) | **GET** /v1/chains/{chain_name}/nft-owners | List all NFT owners |
| [**ListNFTOwners**](NftOwnersApi.md#listnftowners) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/nfts/{token_id}/owners | List NFT owners by token ID |
| [**ListOwnersByContractAddress**](NftOwnersApi.md#listownersbycontractaddress) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/owners | List owners by contract address |

<a id="listallnftowners"></a>
# **ListAllNFTOwners**
> ListNFTOwnersResult ListAllNFTOwners (string chainName, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

List all NFT owners

List all NFT owners on a chain

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListAllNFTOwnersExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftOwnersApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List all NFT owners
                ListNFTOwnersResult result = apiInstance.ListAllNFTOwners(chainName, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftOwnersApi.ListAllNFTOwners: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListAllNFTOwnersWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all NFT owners
    ApiResponse<ListNFTOwnersResult> response = apiInstance.ListAllNFTOwnersWithHttpInfo(chainName, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftOwnersApi.ListAllNFTOwnersWithHttpInfo: " + e.Message);
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

[**ListNFTOwnersResult**](ListNFTOwnersResult.md)

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

<a id="listnftowners"></a>
# **ListNFTOwners**
> ListNFTOwnersResult ListNFTOwners (string contractAddress, string tokenId, string chainName, string? pageCursor = null, int? pageSize = null)

List NFT owners by token ID

List NFT owners by token ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListNFTOwnersExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftOwnersApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var tokenId = 1;  // string | An `uint256` token id as string
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List NFT owners by token ID
                ListNFTOwnersResult result = apiInstance.ListNFTOwners(contractAddress, tokenId, chainName, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftOwnersApi.ListNFTOwners: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListNFTOwnersWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List NFT owners by token ID
    ApiResponse<ListNFTOwnersResult> response = apiInstance.ListNFTOwnersWithHttpInfo(contractAddress, tokenId, chainName, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftOwnersApi.ListNFTOwnersWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **tokenId** | **string** | An &#x60;uint256&#x60; token id as string |  |
| **chainName** | **string** | The name of chain |  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListNFTOwnersResult**](ListNFTOwnersResult.md)

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

<a id="listownersbycontractaddress"></a>
# **ListOwnersByContractAddress**
> ListCollectionOwnersResult ListOwnersByContractAddress (string contractAddress, string chainName, List<string>? accountAddress = null, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

List owners by contract address

List owners by contract address

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListOwnersByContractAddressExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftOwnersApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var accountAddress = new List<string>?(); // List<string>? | List of account addresses to filter by (optional) 
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List owners by contract address
                ListCollectionOwnersResult result = apiInstance.ListOwnersByContractAddress(contractAddress, chainName, accountAddress, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftOwnersApi.ListOwnersByContractAddress: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListOwnersByContractAddressWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List owners by contract address
    ApiResponse<ListCollectionOwnersResult> response = apiInstance.ListOwnersByContractAddressWithHttpInfo(contractAddress, chainName, accountAddress, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftOwnersApi.ListOwnersByContractAddressWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **chainName** | **string** | The name of chain |  |
| **accountAddress** | [**List&lt;string&gt;?**](string.md) | List of account addresses to filter by | [optional]  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListCollectionOwnersResult**](ListCollectionOwnersResult.md)

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

