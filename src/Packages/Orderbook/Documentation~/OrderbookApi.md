# Immutable.Orderbook.Api.OrderbookApi

All URIs are relative to *https://api.immutable.com*

| Method                                                                     | HTTP request                                            | Description |
|----------------------------------------------------------------------------|---------------------------------------------------------|-------------|
| [**CancelOrders**](OrderbookApi.md#cancelorders)                           | **POST** /v1/ts-sdk/orderbook/cancelOrders              |             |
| [**CancelOrdersOnChain**](OrderbookApi.md#cancelordersonchain)             | **POST** /v1/ts-sdk/orderbook/cancelOrdersOnChain       |             |
| [**CreateListing**](OrderbookApi.md#createlisting)                         | **POST** /v1/ts-sdk/orderbook/createListing             |             |
| [**FulfillOrder**](OrderbookApi.md#fulfillorder)                           | **POST** /v1/ts-sdk/orderbook/fulfillOrder              |             |
| [**PrepareListing**](OrderbookApi.md#preparelisting)                       | **POST** /v1/ts-sdk/orderbook/prepareListing            |             |
| [**PrepareOrderCancellations**](OrderbookApi.md#prepareordercancellations) | **POST** /v1/ts-sdk/orderbook/prepareOrderCancellations |             |
| [**TokenBalance**](OrderbookApi.md#tokenbalance)                           | **GET** /v1/ts-sdk/token/balance                        |             |

<a id="cancelorders"></a>

# **CancelOrders**

> CancelOrders200Response CancelOrders (CancelOrdersRequest? cancelOrdersRequest = null)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class CancelOrdersExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var cancelOrdersRequest = new CancelOrdersRequest?(); // CancelOrdersRequest? |  (optional) 

            try
            {
                CancelOrders200Response result = apiInstance.CancelOrders(cancelOrdersRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.CancelOrders: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CancelOrdersWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<CancelOrders200Response> response = apiInstance.CancelOrdersWithHttpInfo(cancelOrdersRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.CancelOrdersWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                    | Type                                                | Description | Notes      |
|-------------------------|-----------------------------------------------------|-------------|------------|
| **cancelOrdersRequest** | [**CancelOrdersRequest?**](CancelOrdersRequest?.md) |             | [optional] |

### Return type

[**CancelOrders200Response**](CancelOrders200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### HTTP response details

| Status code | Description                                  | Response headers |
|-------------|----------------------------------------------|------------------|
| **200**     | Response schema for the cancelOrder endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="cancelordersonchain"></a>

# **CancelOrdersOnChain**

> CancelOrdersOnChain200Response CancelOrdersOnChain (CancelOrdersOnChainRequest? cancelOrdersOnChainRequest = null)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class CancelOrdersOnChainExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var cancelOrdersOnChainRequest = new CancelOrdersOnChainRequest?(); // CancelOrdersOnChainRequest? |  (optional) 

            try
            {
                CancelOrdersOnChain200Response result = apiInstance.CancelOrdersOnChain(cancelOrdersOnChainRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.CancelOrdersOnChain: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CancelOrdersOnChainWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<CancelOrdersOnChain200Response> response = apiInstance.CancelOrdersOnChainWithHttpInfo(cancelOrdersOnChainRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.CancelOrdersOnChainWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                           | Type                                                              | Description | Notes      |
|--------------------------------|-------------------------------------------------------------------|-------------|------------|
| **cancelOrdersOnChainRequest** | [**CancelOrdersOnChainRequest?**](CancelOrdersOnChainRequest?.md) |             | [optional] |

### Return type

[**CancelOrdersOnChain200Response**](CancelOrdersOnChain200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### HTTP response details

| Status code | Description                                  | Response headers |
|-------------|----------------------------------------------|------------------|
| **200**     | Response schema for the cancelOrder endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createlisting"></a>

# **CreateListing**

> CreateListing200Response CreateListing (CreateListingRequest? createListingRequest = null)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class CreateListingExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var createListingRequest = new CreateListingRequest?(); // CreateListingRequest? |  (optional) 

            try
            {
                CreateListing200Response result = apiInstance.CreateListing(createListingRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.CreateListing: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateListingWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<CreateListing200Response> response = apiInstance.CreateListingWithHttpInfo(createListingRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.CreateListingWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                     | Type                                                  | Description | Notes      |
|--------------------------|-------------------------------------------------------|-------------|------------|
| **createListingRequest** | [**CreateListingRequest?**](CreateListingRequest?.md) |             | [optional] |

### Return type

[**CreateListing200Response**](CreateListing200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### HTTP response details

| Status code | Description                                         | Response headers |
|-------------|-----------------------------------------------------|------------------|
| **200**     | The response schema for the create listing endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="fulfillorder"></a>

# **FulfillOrder**

> FulfillOrder200Response FulfillOrder (FulfillOrderRequest? fulfillOrderRequest = null)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class FulfillOrderExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var fulfillOrderRequest = new FulfillOrderRequest?(); // FulfillOrderRequest? |  (optional) 

            try
            {
                FulfillOrder200Response result = apiInstance.FulfillOrder(fulfillOrderRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.FulfillOrder: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the FulfillOrderWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<FulfillOrder200Response> response = apiInstance.FulfillOrderWithHttpInfo(fulfillOrderRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.FulfillOrderWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                    | Type                                                | Description | Notes      |
|-------------------------|-----------------------------------------------------|-------------|------------|
| **fulfillOrderRequest** | [**FulfillOrderRequest?**](FulfillOrderRequest?.md) |             | [optional] |

### Return type

[**FulfillOrder200Response**](FulfillOrder200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### HTTP response details

| Status code | Description                                   | Response headers |
|-------------|-----------------------------------------------|------------------|
| **200**     | Response schema for the fulfillOrder endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="preparelisting"></a>

# **PrepareListing**

> PrepareListing200Response PrepareListing (PrepareListingRequest? prepareListingRequest = null)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class PrepareListingExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var prepareListingRequest = new PrepareListingRequest?(); // PrepareListingRequest? |  (optional) 

            try
            {
                PrepareListing200Response result = apiInstance.PrepareListing(prepareListingRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.PrepareListing: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the PrepareListingWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<PrepareListing200Response> response = apiInstance.PrepareListingWithHttpInfo(prepareListingRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.PrepareListingWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                      | Type                                                    | Description | Notes      |
|---------------------------|---------------------------------------------------------|-------------|------------|
| **prepareListingRequest** | [**PrepareListingRequest?**](PrepareListingRequest?.md) |             | [optional] |

### Return type

[**PrepareListing200Response**](PrepareListing200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### HTTP response details

| Status code | Description                                     | Response headers |
|-------------|-------------------------------------------------|------------------|
| **200**     | Response schema for the prepareListing endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="prepareordercancellations"></a>

# **PrepareOrderCancellations**

> PrepareOrderCancellations200Response PrepareOrderCancellations (PrepareOrderCancellationsRequest?
> prepareOrderCancellationsRequest = null)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class PrepareOrderCancellationsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var prepareOrderCancellationsRequest = new PrepareOrderCancellationsRequest?(); // PrepareOrderCancellationsRequest? |  (optional) 

            try
            {
                PrepareOrderCancellations200Response result = apiInstance.PrepareOrderCancellations(prepareOrderCancellationsRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.PrepareOrderCancellations: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the PrepareOrderCancellationsWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<PrepareOrderCancellations200Response> response = apiInstance.PrepareOrderCancellationsWithHttpInfo(prepareOrderCancellationsRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.PrepareOrderCancellationsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                                 | Type                                                                          | Description | Notes      |
|--------------------------------------|-------------------------------------------------------------------------------|-------------|------------|
| **prepareOrderCancellationsRequest** | [**PrepareOrderCancellationsRequest?**](PrepareOrderCancellationsRequest?.md) |             | [optional] |

### Return type

[**PrepareOrderCancellations200Response**](PrepareOrderCancellations200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### HTTP response details

| Status code | Description                                                | Response headers |
|-------------|------------------------------------------------------------|------------------|
| **200**     | Response schema for the prepareOrderCancellations endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="tokenbalance"></a>

# **TokenBalance**

> TokenBalance200Response TokenBalance (string walletAddress, string contractAddress)

### Example

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Orderbook.Api;
using Immutable.Orderbook.Client;
using Immutable.Orderbook.Model;

namespace Example
{
    public class TokenBalanceExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.immutable.com";
            var apiInstance = new OrderbookApi(config);
            var walletAddress = "walletAddress_example";  // string | 
            var contractAddress = "contractAddress_example";  // string | 

            try
            {
                TokenBalance200Response result = apiInstance.TokenBalance(walletAddress, contractAddress);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrderbookApi.TokenBalance: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the TokenBalanceWithHttpInfo variant

This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    ApiResponse<TokenBalance200Response> response = apiInstance.TokenBalanceWithHttpInfo(walletAddress, contractAddress);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrderbookApi.TokenBalanceWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name                | Type       | Description | Notes |
|---------------------|------------|-------------|-------|
| **walletAddress**   | **string** |             |       |
| **contractAddress** | **string** |             |       |

### Return type

[**TokenBalance200Response**](TokenBalance200Response.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### HTTP response details

| Status code | Description                                          | Response headers |
|-------------|------------------------------------------------------|------------------|
| **200**     | The response body returned from get balance endpoint | -                |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

