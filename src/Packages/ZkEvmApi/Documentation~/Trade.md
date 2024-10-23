# Immutable.Api.ZkEvm.Model.Trade

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Buy** | [**List&lt;Item&gt;**](Item.md) | Buy items are transferred from the taker to the maker. | 
**BuyerAddress** | **string** | Deprecated. Use maker and taker addresses instead of buyer and seller addresses. | 
**BuyerFees** | [**List&lt;Fee&gt;**](Fee.md) | Deprecated. Use fees instead. The taker always pays the fees. | 
**Fees** | [**List&lt;Fee&gt;**](Fee.md) |  | 
**Chain** | [**Chain**](Chain.md) |  | 
**OrderId** | **string** |  | 
**BlockchainMetadata** | [**TradeBlockchainMetadata**](TradeBlockchainMetadata.md) |  | 
**IndexedAt** | **DateTime** | Time the on-chain trade event is indexed by the order book system | 
**Id** | **string** | Global Trade identifier | 
**Sell** | [**List&lt;Item&gt;**](Item.md) | Sell items are transferred from the maker to the taker. | 
**SellerAddress** | **string** | Deprecated. Use maker and taker addresses instead of buyer and seller addresses. | 
**MakerAddress** | **string** |  | 
**TakerAddress** | **string** |  | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

