# Immutable.Api.ZkEvm.Model.SignCraftingRequest

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**MultiCaller** | [**SignCraftingRequestMultiCaller**](SignCraftingRequestMultiCaller.md) |  | 
**ReferenceId** | **string** | The id of this request in the system that originates the crafting request, specified as a 32 byte hex string | 
**Calls** | [**List&lt;Call&gt;**](Call.md) | The calls to be signed | 
**ExpiresAt** | **DateTime** | The expiration time of the request | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

