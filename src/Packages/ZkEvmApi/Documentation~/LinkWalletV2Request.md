# Immutable.Api.ZkEvm.Model.LinkWalletV2Request
Link wallet V2 request

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Type** | **string** | This should be the EIP-6963 rdns value, if you&#39;re unable to get the rdns value you can provide \&quot;External\&quot;. If using WalletConnect then provide \&quot;WalletConnect\&quot;. | 
**WalletAddress** | **string** | The address of the external wallet being linked to Passport | 
**Signature** | **string** | The EIP-712 signature | 
**Nonce** | **string** | A unique identifier for the signature | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

