# Immutable.Api.ZkEvm.Model.Metadata

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** | Metadata id in UUIDv4 format | 
**Chain** | [**Chain**](Chain.md) |  | 
**ContractAddress** | **string** | The contract address of the metadata | 
**CreatedAt** | **DateTime** | When the metadata was created | 
**UpdatedAt** | **DateTime?** | When the metadata was last updated | 
**Name** | **string** | The name of the NFT | 
**Description** | **string** | The description of the NFT | 
**Image** | **string** | The image url of the NFT | 
**ExternalUrl** | **string** | The external website link of NFT | [optional] 
**AnimationUrl** | **string** | The animation url of the NFT | 
**YoutubeUrl** | **string** | The youtube URL of NFT | 
**Attributes** | [**List&lt;NFTMetadataAttribute&gt;**](NFTMetadataAttribute.md) | List of Metadata attributes | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

