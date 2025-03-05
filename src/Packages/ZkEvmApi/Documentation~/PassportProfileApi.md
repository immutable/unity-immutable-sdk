# Immutable.Api.ZkEvm.Api.PassportProfileApi

All URIs are relative to *https://api.sandbox.immutable.com*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetUserInfo**](PassportProfileApi.md#getuserinfo) | **GET** /passport-profile/v1/user/info | Get all info for a Passport user |
| [**LinkWalletV2**](PassportProfileApi.md#linkwalletv2) | **POST** /passport-profile/v2/linked-wallets | Link wallet v2 |
| [**SendPhoneOtp**](PassportProfileApi.md#sendphoneotp) | **POST** /passport-profile/v1/phone-otp | Send phone OTP code for user supplied phone number |
| [**VerifyPhoneOtp**](PassportProfileApi.md#verifyphoneotp) | **POST** /passport-profile/v1/phone-otp/verify | Verify phone OTP code against user phone number |

<a id="getuserinfo"></a>
# **GetUserInfo**
> UserInfo GetUserInfo ()

Get all info for a Passport user

Get all the info for an authenticated Passport user

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class GetUserInfoExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure Bearer token for authorization: BearerAuth
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new PassportProfileApi(config);

            try
            {
                // Get all info for a Passport user
                UserInfo result = apiInstance.GetUserInfo();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PassportProfileApi.GetUserInfo: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetUserInfoWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all info for a Passport user
    ApiResponse<UserInfo> response = apiInstance.GetUserInfoWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PassportProfileApi.GetUserInfoWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

[**UserInfo**](UserInfo.md)

### Authorization

[BearerAuth](../README.md#BearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Passport user&#39;s info |  -  |
| **401** | UnauthorizedError |  -  |
| **500** | InternalServerError |  -  |
| **0** | unexpected error |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="linkwalletv2"></a>
# **LinkWalletV2**
> Wallet LinkWalletV2 (LinkWalletV2Request? linkWalletV2Request = null)

Link wallet v2

Link an external EOA wallet to an Immutable Passport account by providing an EIP-712 signature.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class LinkWalletV2Example
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure Bearer token for authorization: BearerAuth
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new PassportProfileApi(config);
            var linkWalletV2Request = new LinkWalletV2Request?(); // LinkWalletV2Request? |  (optional) 

            try
            {
                // Link wallet v2
                Wallet result = apiInstance.LinkWalletV2(linkWalletV2Request);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PassportProfileApi.LinkWalletV2: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the LinkWalletV2WithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Link wallet v2
    ApiResponse<Wallet> response = apiInstance.LinkWalletV2WithHttpInfo(linkWalletV2Request);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PassportProfileApi.LinkWalletV2WithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **linkWalletV2Request** | [**LinkWalletV2Request?**](LinkWalletV2Request?.md) |  | [optional]  |

### Return type

[**Wallet**](Wallet.md)

### Authorization

[BearerAuth](../README.md#BearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | BadRequestError |  -  |
| **401** | UnauthorizedError |  -  |
| **403** | ForbiddenError |  -  |
| **500** | InternalServerError |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="sendphoneotp"></a>
# **SendPhoneOtp**
> void SendPhoneOtp (PhoneNumberOTPRequest? phoneNumberOTPRequest = null)

Send phone OTP code for user supplied phone number

Send phone OTP code for user supplied phone number

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class SendPhoneOtpExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure Bearer token for authorization: BearerAuth
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new PassportProfileApi(config);
            var phoneNumberOTPRequest = new PhoneNumberOTPRequest?(); // PhoneNumberOTPRequest? |  (optional) 

            try
            {
                // Send phone OTP code for user supplied phone number
                apiInstance.SendPhoneOtp(phoneNumberOTPRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PassportProfileApi.SendPhoneOtp: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SendPhoneOtpWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Send phone OTP code for user supplied phone number
    apiInstance.SendPhoneOtpWithHttpInfo(phoneNumberOTPRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PassportProfileApi.SendPhoneOtpWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **phoneNumberOTPRequest** | [**PhoneNumberOTPRequest?**](PhoneNumberOTPRequest?.md) |  | [optional]  |

### Return type

void (empty response body)

### Authorization

[BearerAuth](../README.md#BearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | BadRequestError |  -  |
| **401** | UnauthorizedError |  -  |
| **403** | ForbiddenError |  -  |
| **500** | InternalServerError |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="verifyphoneotp"></a>
# **VerifyPhoneOtp**
> void VerifyPhoneOtp (PhoneNumberOTPVerificationRequest? phoneNumberOTPVerificationRequest = null)

Verify phone OTP code against user phone number

Verify phone OTP code for user supplied phone number

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;

namespace Example
{
    public class VerifyPhoneOtpExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.sandbox.immutable.com";
            // Configure Bearer token for authorization: BearerAuth
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new PassportProfileApi(config);
            var phoneNumberOTPVerificationRequest = new PhoneNumberOTPVerificationRequest?(); // PhoneNumberOTPVerificationRequest? |  (optional) 

            try
            {
                // Verify phone OTP code against user phone number
                apiInstance.VerifyPhoneOtp(phoneNumberOTPVerificationRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PassportProfileApi.VerifyPhoneOtp: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the VerifyPhoneOtpWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Verify phone OTP code against user phone number
    apiInstance.VerifyPhoneOtpWithHttpInfo(phoneNumberOTPVerificationRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PassportProfileApi.VerifyPhoneOtpWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **phoneNumberOTPVerificationRequest** | [**PhoneNumberOTPVerificationRequest?**](PhoneNumberOTPVerificationRequest?.md) |  | [optional]  |

### Return type

void (empty response body)

### Authorization

[BearerAuth](../README.md#BearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | BadRequestError |  -  |
| **401** | UnauthorizedError |  -  |
| **403** | ForbiddenError |  -  |
| **500** | InternalServerError |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

