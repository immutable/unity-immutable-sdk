# Immutable.Orderbook.Model.OrderStatus

The Order status

## Properties

 Name                    | Type                 | Description                                                                           | Notes      
-------------------------|----------------------|---------------------------------------------------------------------------------------|------------
 **Name**                | **string**           | The order status that indicates the order is yet to be active due to various reasons. | [optional] 
 **CancellationType**    | **CancellationType** |                                                                                       | [optional] 
 **Pending**             | **bool**             | Whether the cancellation of the order is pending                                      | [optional] 
 **SufficientApprovals** | **bool**             | Whether the order offerer has sufficient approvals                                    | [optional] 
 **SufficientBalances**  | **bool**             | Whether the order offerer still has sufficient balance to complete the order          | [optional] 
 **Evaluated**           | **bool**             | Whether the order has been evaluated after its creation                               | [optional] 
 **Started**             | **bool**             | Whether the order has reached its specified start time                                | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

