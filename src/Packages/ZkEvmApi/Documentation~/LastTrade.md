# Immutable.Api.ZkEvm.Model.LastTrade
Most recent trade

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**TradeId** | **Guid** | Trade ID | 
**ContractAddress** | **string** | ETH Address of collection that the asset belongs to | 
**TokenId** | **string** | Token id of the traded asset (uint256 as string) | 
**PriceDetails** | [**List&lt;MarketPriceDetails&gt;**](MarketPriceDetails.md) | Price details, list of payments involved in this trade | 
**Amount** | **string** | Amount of the trade (uint256 as string) | 
**CreatedAt** | **DateTime** | When the trade was created | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

