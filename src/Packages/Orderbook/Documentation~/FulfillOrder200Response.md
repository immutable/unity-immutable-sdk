# Immutable.Orderbook.Model.FulfillOrder200Response

Response schema for the fulfillOrder endpoint

## Properties

 Name           | Type                                                      | Description                                                                                                                         | Notes      
----------------|-----------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|------------
 **Actions**    | [**List&lt;TransactionAction&gt;**](TransactionAction.md) |                                                                                                                                     | [optional] 
 **Expiration** | **string**                                                | User MUST submit the fulfillment transaction before the expiration Submitting after the expiration will result in a on chain revert | [optional] 
 **Order**      | [**Order**](Order.md)                                     |                                                                                                                                     | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

