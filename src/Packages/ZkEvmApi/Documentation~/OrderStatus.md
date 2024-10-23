# Immutable.Api.ZkEvm.Model.OrderStatus
The Order status

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Name** | **string** | A terminal order status indicating that an order cannot be fulfilled due to expiry. | 
**Pending** | **bool** | Whether the cancellation of the order is pending | 
**CancellationType** | **string** | Whether the cancellation was done on-chain or off-chain or as a result of an underfunded account | 
**Evaluated** | **bool** | Whether the order has been evaluated after its creation | 
**Started** | **bool** | Whether the order has reached its specified start time | 
**SufficientApprovals** | **bool** | Whether the order offerer has sufficient approvals | 
**SufficientBalances** | **bool** | Whether the order offerer still has sufficient balance to complete the order | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

