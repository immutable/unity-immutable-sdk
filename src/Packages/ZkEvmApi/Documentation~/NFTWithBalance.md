# Immutable.Api.ZkEvm.Model.NFTWithBalance

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Chain** | [**Chain**](Chain.md) |  | 
**TokenId** | **string** | An &#x60;uint256&#x60; token id as string | 
**ContractAddress** | **string** | The contract address of the NFT | 
**ContractType** | **NFTContractType** |  | 
**IndexedAt** | **DateTime** | When the NFT was first indexed | 
**UpdatedAt** | **DateTime** | When the NFT owner was last updated | 
**MetadataSyncedAt** | **DateTime?** | When NFT metadata was last synced | 
**MetadataId** | **Guid?** | The id of the metadata of this NFT | [optional] 
**Name** | **string** | The name of the NFT | 
**Description** | **string** | The description of the NFT | 
**Image** | **string** | The image url of the NFT | 
**ExternalLink** | **string** | The external website link of NFT | 
**AnimationUrl** | **string** | The animation url of the NFT | 
**YoutubeUrl** | **string** | The youtube URL of NFT | 
**Attributes** | [**List&lt;NFTMetadataAttribute&gt;**](NFTMetadataAttribute.md) | List of Metadata attributes | 
**Balance** | **string** | The amount of this NFT this account owns | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)
