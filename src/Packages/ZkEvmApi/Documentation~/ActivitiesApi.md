# Immutable.Api.ZkEvm.Api.ActivitiesApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetActivity**](ActivitiesApi.md#getactivity) | **GET** /v1/chains/{chain_name}/activities/{activity_id} | Get a single activity by ID |
| [**ListActivities**](ActivitiesApi.md#listactivities) | **GET** /v1/chains/{chain_name}/activities | List all activities |
| [**ListActivityHistory**](ActivitiesApi.md#listactivityhistory) | **GET** /v1/chains/{chain_name}/activity-history | List history of activities |

<a id="getactivity"></a>
# **GetActivity**
> GetActivityResult GetActivity (string chainName, Guid activityId)

Get a single activity by ID

Get a single activity by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetActivityExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new ActivitiesApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var activityId = "activityId_example";  // Guid | The id of activity

            try
            {
                // Get a single activity by ID
                GetActivityResult result = apiInstance.GetActivity(chainName, activityId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ActivitiesApi.GetActivity: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetActivityWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a single activity by ID
    ApiResponse<GetActivityResult> response = apiInstance.GetActivityWithHttpInfo(chainName, activityId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ActivitiesApi.GetActivityWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **activityId** | **Guid** | The id of activity |  |

### Return type

[**GetActivityResult**](GetActivityResult.md)

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

<a id="listactivities"></a>
# **ListActivities**
> ListActivitiesResult ListActivities (string chainName, string? contractAddress = null, string? tokenId = null, string? accountAddress = null, ActivityType? activityType = null, string? transactionHash = null, string? pageCursor = null, int? pageSize = null)

List all activities

List all activities

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListActivitiesExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new ActivitiesApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string? | The contract address of NFT or ERC20 Token (optional) 
            var tokenId = 1;  // string? | An `uint256` token id as string (optional) 
            var accountAddress = 0xe9b00a87700f660e46b6f5deaa1232836bcc07d3;  // string? | The account address activity contains (optional) 
            var activityType = new ActivityType?(); // ActivityType? | The activity type (optional) 
            var transactionHash = 0x68d9eac5e3b3c3580404989a4030c948a78e1b07b2b5ea5688d8c38a6c61c93e;  // string? | The transaction hash of activity (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List all activities
                ListActivitiesResult result = apiInstance.ListActivities(chainName, contractAddress, tokenId, accountAddress, activityType, transactionHash, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ActivitiesApi.ListActivities: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListActivitiesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all activities
    ApiResponse<ListActivitiesResult> response = apiInstance.ListActivitiesWithHttpInfo(chainName, contractAddress, tokenId, accountAddress, activityType, transactionHash, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ActivitiesApi.ListActivitiesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **contractAddress** | **string?** | The contract address of NFT or ERC20 Token | [optional]  |
| **tokenId** | **string?** | An &#x60;uint256&#x60; token id as string | [optional]  |
| **accountAddress** | **string?** | The account address activity contains | [optional]  |
| **activityType** | [**ActivityType?**](ActivityType?.md) | The activity type | [optional]  |
| **transactionHash** | **string?** | The transaction hash of activity | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListActivitiesResult**](ListActivitiesResult.md)

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

<a id="listactivityhistory"></a>
# **ListActivityHistory**
> ListActivitiesResult ListActivityHistory (string chainName, DateTime fromUpdatedAt, DateTime? toUpdatedAt = null, string? contractAddress = null, ActivityType? activityType = null, string? pageCursor = null, int? pageSize = null)

List history of activities

List activities sorted by updated_at timestamp ascending, useful for time based data replication

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListActivityHistoryExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new ActivitiesApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var fromUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime | From indexed at including given date
            var toUpdatedAt = 2022-08-16T17:43:26.991388Z;  // DateTime? | To indexed at including given date (optional) 
            var contractAddress = 0x8a90cab2b38dba80c64b7734e58ee1db38b8992e;  // string? | The contract address of the collection (optional) 
            var activityType = new ActivityType?(); // ActivityType? | The activity type (optional) 
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List history of activities
                ListActivitiesResult result = apiInstance.ListActivityHistory(chainName, fromUpdatedAt, toUpdatedAt, contractAddress, activityType, pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ActivitiesApi.ListActivityHistory: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListActivityHistoryWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List history of activities
    ApiResponse<ListActivitiesResult> response = apiInstance.ListActivityHistoryWithHttpInfo(chainName, fromUpdatedAt, toUpdatedAt, contractAddress, activityType, pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ActivitiesApi.ListActivityHistoryWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **fromUpdatedAt** | **DateTime** | From indexed at including given date |  |
| **toUpdatedAt** | **DateTime?** | To indexed at including given date | [optional]  |
| **contractAddress** | **string?** | The contract address of the collection | [optional]  |
| **activityType** | [**ActivityType?**](ActivityType?.md) | The activity type | [optional]  |
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListActivitiesResult**](ListActivitiesResult.md)

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

