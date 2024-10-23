# Immutable.Api.ZkEvm.Api.NftsApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CreateMintRequest**](NftsApi.md#createmintrequest) | **POST** /v1/chains/{chain_name}/collections/{contract_address}/nfts/mint-requests | Mint NFTs |
| [**GetMintRequest**](NftsApi.md#getmintrequest) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/nfts/mint-requests/{reference_id} | Get mint request by reference ID |
| [**GetNFT**](NftsApi.md#getnft) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/nfts/{token_id} | Get NFT by token ID |
| [**ListAllNFTs**](NftsApi.md#listallnfts) | **GET** /v1/chains/{chain_name}/nfts | List all NFTs |
| [**ListMintRequests**](NftsApi.md#listmintrequests) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/nfts/mint-requests | List mint requests |
| [**ListNFTs**](NftsApi.md#listnfts) | **GET** /v1/chains/{chain_name}/collections/{contract_address}/nfts | List NFTs by contract address |
| [**ListNFTsByAccountAddress**](NftsApi.md#listnftsbyaccountaddress) | **GET** /v1/chains/{chain_name}/accounts/{account_address}/nfts | List NFTs by account address |

<a id="createmintrequest"></a>
# **CreateMintRequest**
> CreateMintRequestResult CreateMintRequest (string contractAddress, string chainName, CreateMintRequestRequest createMintRequestRequest)

Mint NFTs

Create a mint request to mint a set of NFTs for a given collection

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class CreateMintRequestExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");

            var apiInstance = new NftsApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var chainName = "chainName_example";  // string | The name of chain
            var createMintRequestRequest = new CreateMintRequestRequest(); // CreateMintRequestRequest | Create Mint Request Body

            try
            {
                // Mint NFTs
                CreateMintRequestResult result = apiInstance.CreateMintRequest(contractAddress, chainName, createMintRequestRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.CreateMintRequest: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateMintRequestWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Mint NFTs
    ApiResponse<CreateMintRequestResult> response = apiInstance.CreateMintRequestWithHttpInfo(contractAddress, chainName, createMintRequestRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.CreateMintRequestWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **chainName** | **string** | The name of chain |  |
| **createMintRequestRequest** | [**CreateMintRequestRequest**](CreateMintRequestRequest.md) | Create Mint Request Body |  |

### Return type

[**CreateMintRequestResult**](CreateMintRequestResult.md)

### Authorization

[ImmutableApiKey](../README.md#ImmutableApiKey)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Accepted |  * imx-mint-requests-limit -  <br>  * imx-mint-requests-limit-reset -  <br>  * imx-remaining-mint-requests -  <br>  * imx-mint-requests-retry-after -  <br>  |
| **400** | Bad Request (400) |  -  |
| **401** | Unauthorised Request (401) |  -  |
| **403** | Forbidden Request (403) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **409** | Conflict (409) |  -  |
| **429** | Too Many mint requests (429) |  * imx-mint-requests-limit -  <br>  * imx-mint-requests-limit-reset -  <br>  * imx-remaining-mint-requests -  <br>  * imx-mint-requests-retry-after -  <br>  * Retry-After -  <br>  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getmintrequest"></a>
# **GetMintRequest**
> ListMintRequestsResult GetMintRequest (string contractAddress, string chainName, string referenceId)

Get mint request by reference ID

Retrieve the status of a mint request identified by its reference_id

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetMintRequestExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");

            var apiInstance = new NftsApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var chainName = "chainName_example";  // string | The name of chain
            var referenceId = 67f7d464-b8f0-4f6a-9a3b-8d3cb4a21af0;  // string | The id of the mint request

            try
            {
                // Get mint request by reference ID
                ListMintRequestsResult result = apiInstance.GetMintRequest(contractAddress, chainName, referenceId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.GetMintRequest: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMintRequestWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get mint request by reference ID
    ApiResponse<ListMintRequestsResult> response = apiInstance.GetMintRequestWithHttpInfo(contractAddress, chainName, referenceId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.GetMintRequestWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **chainName** | **string** | The name of chain |  |
| **referenceId** | **string** | The id of the mint request |  |

### Return type

[**ListMintRequestsResult**](ListMintRequestsResult.md)

### Authorization

[ImmutableApiKey](../README.md#ImmutableApiKey)

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
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getnft"></a>
# **GetNFT**
> GetNFTResult GetNFT (string contractAddress, string tokenId, string chainName)

Get NFT by token ID

Get NFT by token ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetNFTExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftsApi(config);
            var contractAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string | The address of NFT contract
            var tokenId = 1;  // string | An `uint256` token id as string
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain

            try
            {
                // Get NFT by token ID
                GetNFTResult result = apiInstance.GetNFT(contractAddress, tokenId, chainName);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.GetNFT: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetNFTWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get NFT by token ID
    ApiResponse<GetNFTResult> response = apiInstance.GetNFTWithHttpInfo(contractAddress, tokenId, chainName);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.GetNFTWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of NFT contract |  |
| **tokenId** | **string** | An &#x60;uint256&#x60; token id as string |  |
| **chainName** | **string** | The name of chain |  |

### Return type

[**GetNFTResult**](GetNFTResult.md)

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

<a id="listallnfts"></a>
# **ListAllNFTs**
> ListNFTsResult ListAllNFTs (string chainName, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

List all NFTs

List all NFTs on a chain

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListAllNFTsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftsApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List all NFTs
                ListNFTsResult result = apiInstance.ListAllNFTs(chainName, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.ListAllNFTs: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListAllNFTsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all NFTs
    ApiResponse<ListNFTsResult> response = apiInstance.ListAllNFTsWithHttpInfo(chainName, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.ListAllNFTsWithHttpInfo: " + e.Message);
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

[**ListNFTsResult**](ListNFTsResult.md)

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

<a id="listmintrequests"></a>
# **ListMintRequests**
> ListMintRequestsResult ListMintRequests (string contractAddress, string chainName, string? pageCursor = null, int? pageSize = null, MintRequestStatus? status = null)

List mint requests

Retrieve the status of all mints for a given contract address

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListMintRequestsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");

            var apiInstance = new NftsApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | The address of contract
            var chainName = "chainName_example";  // string | The name of chain
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)
            var status = new MintRequestStatus?(); // MintRequestStatus? | The status of the mint request (optional) 

            try
            {
                // List mint requests
                ListMintRequestsResult result = apiInstance.ListMintRequests(contractAddress, chainName, pageCursor, pageSize, status);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.ListMintRequests: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListMintRequestsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List mint requests
    ApiResponse<ListMintRequestsResult> response = apiInstance.ListMintRequestsWithHttpInfo(contractAddress, chainName, pageCursor, pageSize, status);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.ListMintRequestsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | The address of contract |  |
| **chainName** | **string** | The name of chain |  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |
| **status** | [**MintRequestStatus?**](MintRequestStatus?.md) | The status of the mint request | [optional]  |

### Return type

[**ListMintRequestsResult**](ListMintRequestsResult.md)

### Authorization

[ImmutableApiKey](../README.md#ImmutableApiKey)

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
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listnfts"></a>
# **ListNFTs**
> ListNFTsResult ListNFTs (string contractAddress, string chainName, List<string>? tokenId = null, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

List NFTs by contract address

List NFTs by contract address

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListNFTsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftsApi(config);
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string | Contract address
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var tokenId = new List<string>?(); // List<string>? | List of token IDs to filter by (optional) 
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List NFTs by contract address
                ListNFTsResult result = apiInstance.ListNFTs(contractAddress, chainName, tokenId, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.ListNFTs: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListNFTsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List NFTs by contract address
    ApiResponse<ListNFTsResult> response = apiInstance.ListNFTsWithHttpInfo(contractAddress, chainName, tokenId, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.ListNFTsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractAddress** | **string** | Contract address |  |
| **chainName** | **string** | The name of chain |  |
| **tokenId** | [**List&lt;string&gt;?**](string.md) | List of token IDs to filter by | [optional]  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListNFTsResult**](ListNFTsResult.md)

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

<a id="listnftsbyaccountaddress"></a>
# **ListNFTsByAccountAddress**
> ListNFTsByOwnerResult ListNFTsByAccountAddress (string accountAddress, string chainName, string? contractAddress = null, List<string>? tokenId = null, DateTime? fromUpdatedAt = null, string? pageCursor = null, int? pageSize = null)

List NFTs by account address

List NFTs by account address

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListNFTsByAccountAddressExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new NftsApi(config);
            var accountAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string | Account address
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string? | The address of contract (optional) 
            var tokenId = new List<string>?(); // List<string>? | List of token IDs to filter by (optional) 
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | Datetime to use as the oldest updated timestamp (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List NFTs by account address
                ListNFTsByOwnerResult result = apiInstance.ListNFTsByAccountAddress(accountAddress, chainName, contractAddress, tokenId, fromUpdatedAt, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling NftsApi.ListNFTsByAccountAddress: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListNFTsByAccountAddressWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List NFTs by account address
    ApiResponse<ListNFTsByOwnerResult> response = apiInstance.ListNFTsByAccountAddressWithHttpInfo(accountAddress, chainName, contractAddress, tokenId, fromUpdatedAt, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling NftsApi.ListNFTsByAccountAddressWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **accountAddress** | **string** | Account address |  |
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string?** | The address of contract | [optional]  |
| **tokenId** | [**List&lt;string&gt;?**](string.md) | List of token IDs to filter by | [optional]  |
| **fromUpdatedAt** | **DateTime?** | Datetime to use as the oldest updated timestamp | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListNFTsByOwnerResult**](ListNFTsByOwnerResult.md)

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

