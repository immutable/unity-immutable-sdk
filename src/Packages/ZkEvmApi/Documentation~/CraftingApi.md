# Immutable.Api.ZkEvm.Api.CraftingApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**SignCraftingPayload**](CraftingApi.md#signcraftingpayload) | **POST** /v1/chains/{chain_name}/crafting/sign | Sign a crafting payload |

<a id="signcraftingpayload"></a>
# **SignCraftingPayload**
> SignCraftingResult SignCraftingPayload (string chainName, SignCraftingRequest signCraftingRequest)

Sign a crafting payload

Sign a crafting payload

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class SignCraftingPayloadExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure API key authorization: ImmutableApiKey
            config.AddApiKey("x-immutable-api-key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("x-immutable-api-key", "Bearer");

            var apiInstance = new CraftingApi(config);
            var chainName = imtbl-zkevm-testnet;  // string | The name of chain
            var signCraftingRequest = new SignCraftingRequest(); // SignCraftingRequest | The request body

            try
            {
                // Sign a crafting payload
                SignCraftingResult result = apiInstance.SignCraftingPayload(chainName, signCraftingRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling CraftingApi.SignCraftingPayload: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SignCraftingPayloadWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Sign a crafting payload
    ApiResponse<SignCraftingResult> response = apiInstance.SignCraftingPayloadWithHttpInfo(chainName, signCraftingRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling CraftingApi.SignCraftingPayloadWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** | The name of chain |  |
| **signCraftingRequest** | [**SignCraftingRequest**](SignCraftingRequest.md) | The request body |  |

### Return type

[**SignCraftingResult**](SignCraftingResult.md)

### Authorization

[ImmutableApiKey](../README.md#ImmutableApiKey)

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

