# Immutable.Api.ZkEvm.Model.MarketPriceDetails
Market Price details

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Token** | [**MarketPriceDetailsToken**](MarketPriceDetailsToken.md) |  | 
**Amount** | **string** | The token amount value. This value is provided in the smallest unit of the token (e.g. wei for ETH) | 
**FeeInclusiveAmount** | **string** | The token amount value. This value is provided in the smallest unit of the token (e.g. wei for ETH) | 
**Fees** | [**List&lt;MarketPriceFees&gt;**](MarketPriceFees.md) |  | 
**ConvertedPrices** | **Dictionary&lt;string, string&gt;** | A mapping of converted prices for major currencies such as ETH, USD. All converted prices are fee-inclusive. | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

