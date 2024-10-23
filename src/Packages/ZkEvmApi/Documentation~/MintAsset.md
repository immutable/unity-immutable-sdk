# Immutable.Api.ZkEvm.Model.MintAsset

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**ReferenceId** | **string** | The id of this asset in the system that originates the mint request | 
**OwnerAddress** | **string** | The address of the receiver | 
**TokenId** | **string** | An optional &#x60;uint256&#x60; token id as string. Required for ERC1155 collections. | [optional] 
**Amount** | **string** | Optional mount of tokens to mint. Required for ERC1155 collections. ERC712 collections can omit this field or set it to 1 | [optional] 
**Metadata** | [**NFTMetadataRequest**](NFTMetadataRequest.md) |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

