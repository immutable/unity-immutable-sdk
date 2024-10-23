# Immutable.Api.ZkEvm.Api.PassportApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetLinkedAddresses**](PassportApi.md#getlinkedaddresses) | **GET** /v1/chains/{chain_name}/passport/users/{user_id}/linked-addresses | Get Ethereum linked addresses for a user |

<a id="getlinkedaddresses"></a>
# **GetLinkedAddresses**
> GetLinkedAddressesRes GetLinkedAddresses (string userId, string chainName)

Get Ethereum linked addresses for a user

This API has been deprecated, please use https://docs.immutable.com/zkevm/api/reference/#/operations/getUserInfo instead to get a list of linked addresses.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetLinkedAddressesExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure Bearer token for authorization: BearerAuth
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new PassportApi(config);
            var userId = "userId_example";  // string | The user's userId
            var chainName = "chainName_example";  // string | 

            try
            {
                // Get Ethereum linked addresses for a user
                GetLinkedAddressesRes result = apiInstance.GetLinkedAddresses(userId, chainName);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PassportApi.GetLinkedAddresses: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetLinkedAddressesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Ethereum linked addresses for a user
    ApiResponse<GetLinkedAddressesRes> response = apiInstance.GetLinkedAddressesWithHttpInfo(userId, chainName);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PassportApi.GetLinkedAddressesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **userId** | **string** | The user&#39;s userId |  |
| **chainName** | **string** |  |  |

### Return type

[**GetLinkedAddressesRes**](GetLinkedAddressesRes.md)

### Authorization

[BearerAuth](../README.md#BearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | User&#39;s list of linked addresses response |  -  |
| **400** | BadRequestError |  -  |
| **401** | UnauthorizedError |  -  |
| **403** | ForbiddenError |  -  |
| **429** | TooManyRequestsError |  -  |
| **500** | InternalServerError |  -  |
| **0** | unexpected error |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

