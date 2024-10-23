# Immutable.Api.ZkEvm.Api.ChainsApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ListChains**](ChainsApi.md#listchains) | **GET** /v1/chains | List supported chains |

<a id="listchains"></a>
# **ListChains**
> ListChainsResult ListChains (string? pageCursor = null, int? pageSize = null)

List supported chains

List supported chains

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListChainsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new ChainsApi(config);
            var pageCursor = "pageCursor_example";  // string? | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 
            var pageSize = 100;  // int? | Maximum number of items to return (optional)  (default to 100)

            try
            {
                // List supported chains
                ListChainsResult result = apiInstance.ListChains(pageCursor, pageSize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ChainsApi.ListChains: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListChainsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List supported chains
    ApiResponse<ListChainsResult> response = apiInstance.ListChainsWithHttpInfo(pageCursor, pageSize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ChainsApi.ListChainsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageCursor** | **string?** | Encoded page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |
| **pageSize** | **int?** | Maximum number of items to return | [optional] [default to 100] |

### Return type

[**ListChainsResult**](ListChainsResult.md)

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

