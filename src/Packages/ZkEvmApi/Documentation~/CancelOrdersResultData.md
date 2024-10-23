# Immutable.Api.ZkEvm.Model.CancelOrdersResultData

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**SuccessfulCancellations** | **List&lt;string&gt;** | Orders which were successfully cancelled | 
**PendingCancellations** | **List&lt;string&gt;** | Orders which are marked for cancellation but the cancellation cannot be guaranteed | 
**FailedCancellations** | [**List&lt;FailedOrderCancellation&gt;**](FailedOrderCancellation.md) | Orders which failed to be cancelled | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

