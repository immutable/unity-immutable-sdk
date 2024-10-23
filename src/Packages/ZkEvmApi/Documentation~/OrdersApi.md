# Immutable.Api.ZkEvm.Api.OrdersApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CancelOrders**](OrdersApi.md#cancelorders) | **POST** /v1/chains/{chain_name}/orders/cancel | Cancel one or more orders |
| [**CreateBid**](OrdersApi.md#createbid) | **POST** /v1/chains/{chain_name}/orders/bids | Create a bid |
| [**CreateCollectionBid**](OrdersApi.md#createcollectionbid) | **POST** /v1/chains/{chain_name}/orders/collection-bids | Create a collection bid |
| [**CreateListing**](OrdersApi.md#createlisting) | **POST** /v1/chains/{chain_name}/orders/listings | Create a listing |
| [**FulfillmentData**](OrdersApi.md#fulfillmentdata) | **POST** /v1/chains/{chain_name}/orders/fulfillment-data | Retrieve fulfillment data for orders |
| [**GetBid**](OrdersApi.md#getbid) | **GET** /v1/chains/{chain_name}/orders/bids/{bid_id} | Get a single bid by ID |
| [**GetCollectionBid**](OrdersApi.md#getcollectionbid) | **GET** /v1/chains/{chain_name}/orders/collection-bids/{collection_bid_id} | Get a single collection bid by ID |
| [**GetListing**](OrdersApi.md#getlisting) | **GET** /v1/chains/{chain_name}/orders/listings/{listing_id} | Get a single listing by ID |
| [**GetTrade**](OrdersApi.md#gettrade) | **GET** /v1/chains/{chain_name}/trades/{trade_id} | Get a single trade by ID |
| [**ListBids**](OrdersApi.md#listbids) | **GET** /v1/chains/{chain_name}/orders/bids | List all bids |
| [**ListCollectionBids**](OrdersApi.md#listcollectionbids) | **GET** /v1/chains/{chain_name}/orders/collection-bids | List all collection bids |
| [**ListListings**](OrdersApi.md#listlistings) | **GET** /v1/chains/{chain_name}/orders/listings | List all listings |
| [**ListTrades**](OrdersApi.md#listtrades) | **GET** /v1/chains/{chain_name}/trades | List all trades |

<a id="cancelorders"></a>
# **CancelOrders**
> CancelOrdersResult CancelOrders (string chainName, CancelOrdersRequestBody cancelOrdersRequestBody)

Cancel one or more orders

Cancel one or more orders

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class CancelOrdersExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var cancelOrdersRequestBody = new CancelOrdersRequestBody(); // CancelOrdersRequestBody | 

            try
            {
                // Cancel one or more orders
                CancelOrdersResult result = apiInstance.CancelOrders(chainName, cancelOrdersRequestBody);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.CancelOrders: " + e.Message);
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
    // Cancel one or more orders
    ApiResponse<CancelOrdersResult> response = apiInstance.CancelOrdersWithHttpInfo(chainName, cancelOrdersRequestBody);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.CancelOrdersWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **cancelOrdersRequestBody** | [**CancelOrdersRequestBody**](CancelOrdersRequestBody.md) |  |  |

### Return type

[**CancelOrdersResult**](CancelOrdersResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Orders cancellation response. |  -  |
| **400** | Bad Request (400) |  -  |
| **401** | Unauthorised Request (401) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **429** | Too Many Requests (429) |  * Retry-After -  <br>  |
| **500** | Internal Server Error (500) |  -  |
| **501** | Not Implemented Error (501) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createbid"></a>
# **CreateBid**
> BidResult CreateBid (string chainName, CreateBidRequestBody createBidRequestBody)

Create a bid

Create a bid

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class CreateBidExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var createBidRequestBody = new CreateBidRequestBody(); // CreateBidRequestBody | 

            try
            {
                // Create a bid
                BidResult result = apiInstance.CreateBid(chainName, createBidRequestBody);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.CreateBid: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateBidWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a bid
    ApiResponse<BidResult> response = apiInstance.CreateBidWithHttpInfo(chainName, createBidRequestBody);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.CreateBidWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **createBidRequestBody** | [**CreateBidRequestBody**](CreateBidRequestBody.md) |  |  |

### Return type

[**BidResult**](BidResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Created response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |
| **501** | Not Implemented Error (501) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createcollectionbid"></a>
# **CreateCollectionBid**
> CollectionBidResult CreateCollectionBid (string chainName, CreateCollectionBidRequestBody createCollectionBidRequestBody)

Create a collection bid

Create a collection bid

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class CreateCollectionBidExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var createCollectionBidRequestBody = new CreateCollectionBidRequestBody(); // CreateCollectionBidRequestBody | 

            try
            {
                // Create a collection bid
                CollectionBidResult result = apiInstance.CreateCollectionBid(chainName, createCollectionBidRequestBody);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.CreateCollectionBid: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateCollectionBidWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a collection bid
    ApiResponse<CollectionBidResult> response = apiInstance.CreateCollectionBidWithHttpInfo(chainName, createCollectionBidRequestBody);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.CreateCollectionBidWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **createCollectionBidRequestBody** | [**CreateCollectionBidRequestBody**](CreateCollectionBidRequestBody.md) |  |  |

### Return type

[**CollectionBidResult**](CollectionBidResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Created response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |
| **501** | Not Implemented Error (501) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createlisting"></a>
# **CreateListing**
> ListingResult CreateListing (string chainName, CreateListingRequestBody createListingRequestBody)

Create a listing

Create a listing

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class CreateListingExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var createListingRequestBody = new CreateListingRequestBody(); // CreateListingRequestBody | 

            try
            {
                // Create a listing
                ListingResult result = apiInstance.CreateListing(chainName, createListingRequestBody);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.CreateListing: " + e.Message);
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
    // Create a listing
    ApiResponse<ListingResult> response = apiInstance.CreateListingWithHttpInfo(chainName, createListingRequestBody);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.CreateListingWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **createListingRequestBody** | [**CreateListingRequestBody**](CreateListingRequestBody.md) |  |  |

### Return type

[**ListingResult**](ListingResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Created response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="fulfillmentdata"></a>
# **FulfillmentData**
> FulfillmentData200Response FulfillmentData (string chainName, List<FulfillmentDataRequest> fulfillmentDataRequest)

Retrieve fulfillment data for orders

Retrieve signed fulfillment data based on the list of order IDs and corresponding fees.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class FulfillmentDataExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var fulfillmentDataRequest = new List<FulfillmentDataRequest>(); // List<FulfillmentDataRequest> | 

            try
            {
                // Retrieve fulfillment data for orders
                FulfillmentData200Response result = apiInstance.FulfillmentData(chainName, fulfillmentDataRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.FulfillmentData: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the FulfillmentDataWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Retrieve fulfillment data for orders
    ApiResponse<FulfillmentData200Response> response = apiInstance.FulfillmentDataWithHttpInfo(chainName, fulfillmentDataRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.FulfillmentDataWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **fulfillmentDataRequest** | [**List&lt;FulfillmentDataRequest&gt;**](FulfillmentDataRequest.md) |  |  |

### Return type

[**FulfillmentData200Response**](FulfillmentData200Response.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successful response |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getbid"></a>
# **GetBid**
> BidResult GetBid (string chainName, Guid bidId)

Get a single bid by ID

Get a single bid by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetBidExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var bidId = 018792c9-4ad7-8ec4-4038-9e05c598534a;  // Guid | Global Bid identifier

            try
            {
                // Get a single bid by ID
                BidResult result = apiInstance.GetBid(chainName, bidId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.GetBid: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBidWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a single bid by ID
    ApiResponse<BidResult> response = apiInstance.GetBidWithHttpInfo(chainName, bidId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.GetBidWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **bidId** | **Guid** | Global Bid identifier |  |

### Return type

[**BidResult**](BidResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getcollectionbid"></a>
# **GetCollectionBid**
> CollectionBidResult GetCollectionBid (string chainName, Guid collectionBidId)

Get a single collection bid by ID

Get a single collection bid by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetCollectionBidExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var collectionBidId = 018792c9-4ad7-8ec4-4038-9e05c598534a;  // Guid | Global Collection Bid identifier

            try
            {
                // Get a single collection bid by ID
                CollectionBidResult result = apiInstance.GetCollectionBid(chainName, collectionBidId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.GetCollectionBid: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetCollectionBidWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a single collection bid by ID
    ApiResponse<CollectionBidResult> response = apiInstance.GetCollectionBidWithHttpInfo(chainName, collectionBidId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.GetCollectionBidWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **collectionBidId** | **Guid** | Global Collection Bid identifier |  |

### Return type

[**CollectionBidResult**](CollectionBidResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getlisting"></a>
# **GetListing**
> ListingResult GetListing (string chainName, Guid listingId)

Get a single listing by ID

Get a single listing by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetListingExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var listingId = 018792c9-4ad7-8ec4-4038-9e05c598534a;  // Guid | Global Order identifier

            try
            {
                // Get a single listing by ID
                ListingResult result = apiInstance.GetListing(chainName, listingId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.GetListing: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetListingWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a single listing by ID
    ApiResponse<ListingResult> response = apiInstance.GetListingWithHttpInfo(chainName, listingId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.GetListingWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **listingId** | **Guid** | Global Order identifier |  |

### Return type

[**ListingResult**](ListingResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="gettrade"></a>
# **GetTrade**
> TradeResult GetTrade (string chainName, Guid tradeId)

Get a single trade by ID

Get a single trade by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetTradeExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var tradeId = 018792c9-4ad7-8ec4-4038-9e05c598534a;  // Guid | Global Trade identifier

            try
            {
                // Get a single trade by ID
                TradeResult result = apiInstance.GetTrade(chainName, tradeId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.GetTrade: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetTradeWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get a single trade by ID
    ApiResponse<TradeResult> response = apiInstance.GetTradeWithHttpInfo(chainName, tradeId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.GetTradeWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **tradeId** | **Guid** | Global Trade identifier |  |

### Return type

[**TradeResult**](TradeResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listbids"></a>
# **ListBids**
> ListBidsResult ListBids (string chainName, OrderStatusName? status = null, string? buyItemContractAddress = null, string? sellItemContractAddress = null, string? accountAddress = null, Guid? buyItemMetadataId = null, string? buyItemTokenId = null, DateTime? fromUpdatedAt = null, int? pageSize = null, string? sortBy = null, string? sortDirection = null, string? pageCursor = null)

List all bids

List all bids

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListBidsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var status = new OrderStatusName?(); // OrderStatusName? | Order status to filter by (optional) 
            var buyItemContractAddress = 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | Buy item contract address to filter by (optional) 
            var sellItemContractAddress = 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | Sell item contract address to filter by (optional) 
            var accountAddress = 0xc49Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | The account address of the user who created the bid (optional) 
            var buyItemMetadataId = 020792c9-4ad7-8ec4-4038-9e05c598535b;  // Guid? | The metadata_id of the buy item (optional) 
            var buyItemTokenId = 1;  // string? | buy item token identifier to filter by (optional) 
            var fromUpdatedAt = 2022-03-09T05:00:50.520Z;  // DateTime? | From updated at including given date (optional) 
            var pageSize = 100;  // int? | Maximum number of orders to return per page (optional)  (default to 100)
            var sortBy = created_at;  // string? | Order field to sort by. `sell_item_amount` sorts by per token price, for example if 10eth is offered for 5 ERC1155 items, it’s sorted as 2eth for `sell_item_amount`. (optional) 
            var sortDirection = asc;  // string? | Ascending or descending direction for sort (optional) 
            var pageCursor = "pageCursor_example";  // string? | Page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // List all bids
                ListBidsResult result = apiInstance.ListBids(chainName, status, buyItemContractAddress, sellItemContractAddress, accountAddress, buyItemMetadataId, buyItemTokenId, fromUpdatedAt, pageSize, sortBy, sortDirection, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.ListBids: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListBidsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all bids
    ApiResponse<ListBidsResult> response = apiInstance.ListBidsWithHttpInfo(chainName, status, buyItemContractAddress, sellItemContractAddress, accountAddress, buyItemMetadataId, buyItemTokenId, fromUpdatedAt, pageSize, sortBy, sortDirection, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.ListBidsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **status** | [**OrderStatusName?**](OrderStatusName?.md) | Order status to filter by | [optional]  |
| **buyItemContractAddress** | **string?** | Buy item contract address to filter by | [optional]  |
| **sellItemContractAddress** | **string?** | Sell item contract address to filter by | [optional]  |
| **accountAddress** | **string?** | The account address of the user who created the bid | [optional]  |
| **buyItemMetadataId** | **Guid?** | The metadata_id of the buy item | [optional]  |
| **buyItemTokenId** | **string?** | buy item token identifier to filter by | [optional]  |
| **fromUpdatedAt** | **DateTime?** | From updated at including given date | [optional]  |
| **pageSize** | **int?** | Maximum number of orders to return per page | [optional] [default to 100] |
| **sortBy** | **string?** | Order field to sort by. &#x60;sell_item_amount&#x60; sorts by per token price, for example if 10eth is offered for 5 ERC1155 items, it’s sorted as 2eth for &#x60;sell_item_amount&#x60;. | [optional]  |
| **sortDirection** | **string?** | Ascending or descending direction for sort | [optional]  |
| **pageCursor** | **string?** | Page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**ListBidsResult**](ListBidsResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listcollectionbids"></a>
# **ListCollectionBids**
> ListCollectionBidsResult ListCollectionBids (string chainName, OrderStatusName? status = null, string? buyItemContractAddress = null, string? sellItemContractAddress = null, string? accountAddress = null, DateTime? fromUpdatedAt = null, int? pageSize = null, string? sortBy = null, string? sortDirection = null, string? pageCursor = null)

List all collection bids

List all collection bids

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListCollectionBidsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var status = new OrderStatusName?(); // OrderStatusName? | Order status to filter by (optional) 
            var buyItemContractAddress = 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | Buy item contract address to filter by (optional) 
            var sellItemContractAddress = 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | Sell item contract address to filter by (optional) 
            var accountAddress = 0xc49Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | The account address of the user who created the bid (optional) 
            var fromUpdatedAt = 2022-03-09T05:00:50.520Z;  // DateTime? | From updated at including given date (optional) 
            var pageSize = 100;  // int? | Maximum number of orders to return per page (optional)  (default to 100)
            var sortBy = created_at;  // string? | Order field to sort by. `sell_item_amount` sorts by per token price, for example if 10eth is offered for 5 ERC1155 items, it’s sorted as 2eth for `sell_item_amount`. (optional) 
            var sortDirection = asc;  // string? | Ascending or descending direction for sort (optional) 
            var pageCursor = "pageCursor_example";  // string? | Page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // List all collection bids
                ListCollectionBidsResult result = apiInstance.ListCollectionBids(chainName, status, buyItemContractAddress, sellItemContractAddress, accountAddress, fromUpdatedAt, pageSize, sortBy, sortDirection, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.ListCollectionBids: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListCollectionBidsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all collection bids
    ApiResponse<ListCollectionBidsResult> response = apiInstance.ListCollectionBidsWithHttpInfo(chainName, status, buyItemContractAddress, sellItemContractAddress, accountAddress, fromUpdatedAt, pageSize, sortBy, sortDirection, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.ListCollectionBidsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **status** | [**OrderStatusName?**](OrderStatusName?.md) | Order status to filter by | [optional]  |
| **buyItemContractAddress** | **string?** | Buy item contract address to filter by | [optional]  |
| **sellItemContractAddress** | **string?** | Sell item contract address to filter by | [optional]  |
| **accountAddress** | **string?** | The account address of the user who created the bid | [optional]  |
| **fromUpdatedAt** | **DateTime?** | From updated at including given date | [optional]  |
| **pageSize** | **int?** | Maximum number of orders to return per page | [optional] [default to 100] |
| **sortBy** | **string?** | Order field to sort by. &#x60;sell_item_amount&#x60; sorts by per token price, for example if 10eth is offered for 5 ERC1155 items, it’s sorted as 2eth for &#x60;sell_item_amount&#x60;. | [optional]  |
| **sortDirection** | **string?** | Ascending or descending direction for sort | [optional]  |
| **pageCursor** | **string?** | Page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**ListCollectionBidsResult**](ListCollectionBidsResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listlistings"></a>
# **ListListings**
> ListListingsResult ListListings (string chainName, OrderStatusName? status = null, string? sellItemContractAddress = null, string? buyItemType = null, string? buyItemContractAddress = null, string? accountAddress = null, Guid? sellItemMetadataId = null, string? sellItemTokenId = null, DateTime? fromUpdatedAt = null, int? pageSize = null, string? sortBy = null, string? sortDirection = null, string? pageCursor = null)

List all listings

List all listings

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListListingsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var status = new OrderStatusName?(); // OrderStatusName? | Order status to filter by (optional) 
            var sellItemContractAddress = 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | Sell item contract address to filter by (optional) 
            var buyItemType = NATIVE;  // string? | Buy item type to filter by (optional) 
            var buyItemContractAddress = 0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | Buy item contract address to filter by (optional) 
            var accountAddress = 0xc49Fd6e51aad88F6F4ce6aB8827279cffFb92266;  // string? | The account address of the user who created the listing (optional) 
            var sellItemMetadataId = 020792c9-4ad7-8ec4-4038-9e05c598535b;  // Guid? | The metadata_id of the sell item (optional) 
            var sellItemTokenId = 1;  // string? | Sell item token identifier to filter by (optional) 
            var fromUpdatedAt = 2022-03-09T05:00:50.520Z;  // DateTime? | From updated at including given date (optional) 
            var pageSize = 100;  // int? | Maximum number of orders to return per page (optional)  (default to 100)
            var sortBy = created_at;  // string? | Order field to sort by. `buy_item_amount` sorts by per token price, for example if 5 ERC-1155s are on sale for 10eth, it’s sorted as 2eth for `buy_item_amount`. (optional) 
            var sortDirection = asc;  // string? | Ascending or descending direction for sort (optional) 
            var pageCursor = "pageCursor_example";  // string? | Page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // List all listings
                ListListingsResult result = apiInstance.ListListings(chainName, status, sellItemContractAddress, buyItemType, buyItemContractAddress, accountAddress, sellItemMetadataId, sellItemTokenId, fromUpdatedAt, pageSize, sortBy, sortDirection, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.ListListings: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListListingsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all listings
    ApiResponse<ListListingsResult> response = apiInstance.ListListingsWithHttpInfo(chainName, status, sellItemContractAddress, buyItemType, buyItemContractAddress, accountAddress, sellItemMetadataId, sellItemTokenId, fromUpdatedAt, pageSize, sortBy, sortDirection, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.ListListingsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **status** | [**OrderStatusName?**](OrderStatusName?.md) | Order status to filter by | [optional]  |
| **sellItemContractAddress** | **string?** | Sell item contract address to filter by | [optional]  |
| **buyItemType** | **string?** | Buy item type to filter by | [optional]  |
| **buyItemContractAddress** | **string?** | Buy item contract address to filter by | [optional]  |
| **accountAddress** | **string?** | The account address of the user who created the listing | [optional]  |
| **sellItemMetadataId** | **Guid?** | The metadata_id of the sell item | [optional]  |
| **sellItemTokenId** | **string?** | Sell item token identifier to filter by | [optional]  |
| **fromUpdatedAt** | **DateTime?** | From updated at including given date | [optional]  |
| **pageSize** | **int?** | Maximum number of orders to return per page | [optional] [default to 100] |
| **sortBy** | **string?** | Order field to sort by. &#x60;buy_item_amount&#x60; sorts by per token price, for example if 5 ERC-1155s are on sale for 10eth, it’s sorted as 2eth for &#x60;buy_item_amount&#x60;. | [optional]  |
| **sortDirection** | **string?** | Ascending or descending direction for sort | [optional]  |
| **pageCursor** | **string?** | Page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**ListListingsResult**](ListListingsResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="listtrades"></a>
# **ListTrades**
> ListTradeResult ListTrades (string chainName, string? accountAddress = null, string? sellItemContractAddress = null, DateTime? fromIndexedAt = null, int? pageSize = null, string? sortBy = null, string? sortDirection = null, string? pageCursor = null)

List all trades

List all trades

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class ListTradesExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            var apiInstance = new OrdersApi(config);
            var chainName = "chainName_example";  // string | 
            var accountAddress = 0x784578949A4A50DeA641Fb15dd2B11C72E76919a;  // string? |  (optional) 
            var sellItemContractAddress = 0x784578949A4A50DeA641Fb15dd2B11C72E76919a;  // string? |  (optional) 
            var fromIndexedAt = 2022-03-09T05:00:50.520Z;  // DateTime? | From indexed at including given date (optional) 
            var pageSize = 100;  // int? | Maximum number of trades to return per page (optional)  (default to 100)
            var sortBy = indexed_at;  // string? | Trade field to sort by (optional) 
            var sortDirection = asc;  // string? | Ascending or descending direction for sort (optional) 
            var pageCursor = "pageCursor_example";  // string? | Page cursor to retrieve previous or next page. Use the value returned in the response. (optional) 

            try
            {
                // List all trades
                ListTradeResult result = apiInstance.ListTrades(chainName, accountAddress, sellItemContractAddress, fromIndexedAt, pageSize, sortBy, sortDirection, pageCursor);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling OrdersApi.ListTrades: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ListTradesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List all trades
    ApiResponse<ListTradeResult> response = apiInstance.ListTradesWithHttpInfo(chainName, accountAddress, sellItemContractAddress, fromIndexedAt, pageSize, sortBy, sortDirection, pageCursor);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling OrdersApi.ListTradesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **chainName** | **string** |  |  |
| **accountAddress** | **string?** |  | [optional]  |
| **sellItemContractAddress** | **string?** |  | [optional]  |
| **fromIndexedAt** | **DateTime?** | From indexed at including given date | [optional]  |
| **pageSize** | **int?** | Maximum number of trades to return per page | [optional] [default to 100] |
| **sortBy** | **string?** | Trade field to sort by | [optional]  |
| **sortDirection** | **string?** | Ascending or descending direction for sort | [optional]  |
| **pageCursor** | **string?** | Page cursor to retrieve previous or next page. Use the value returned in the response. | [optional]  |

### Return type

[**ListTradeResult**](ListTradeResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK response. |  -  |
| **400** | Bad Request (400) |  -  |
| **404** | The specified resource was not found (404) |  -  |
| **500** | Internal Server Error (500) |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

