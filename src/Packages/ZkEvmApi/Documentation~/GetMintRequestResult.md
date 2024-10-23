# Immutable.Api.ZkEvm.Model.GetMintRequestResult

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Chain** | [**Chain**](Chain.md) |  | 
**CollectionAddress** | **string** | The address of the contract | 
**ReferenceId** | **string** | The reference id of this mint request | 
**OwnerAddress** | **string** | The address of the owner of the NFT | 
**TokenId** | **string** | An &#x60;uint256&#x60; token id as string. Only available when the mint request succeeds | 
**Amount** | **string** | An &#x60;uint256&#x60; amount as string. Only relevant for mint requests on ERC1155 contracts | [optional] 
**ActivityId** | **Guid?** | The id of the mint activity associated with this mint request | [optional] 
**TransactionHash** | **string** | The transaction hash of the activity | 
**CreatedAt** | **DateTime** | When the mint request was created | 
**UpdatedAt** | **DateTime** | When the mint request was last updated | 
**Error** | [**MintRequestErrorMessage**](MintRequestErrorMessage.md) |  | 
**Status** | **MintRequestStatus** |  | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

