using System;
using NUnit.Framework;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Immutable.Passport.Helpers;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class BrowserCommunicationsManagerTests
    {
        private const string FUNCTION_NAME = "someFunction";
        private const string ERROR = "some error";

#pragma warning disable CS8618
        private BrowserCommunicationsManager manager;
        private MockBrowserClient mockClient;
#pragma warning restore CS8618

        [SetUp]
        public void Init()
        {
            mockClient = new MockBrowserClient();
            manager = new BrowserCommunicationsManager(mockClient);
        }

        [Test]
        public async Task CallAndResponse_Success_WithData()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                success = true
            };
            _ = await manager.Call(FUNCTION_NAME, "{\"someKey\":\"someData\"}");

            Assert.NotNull(mockClient.request);
            Assert.True(mockClient.request.fxName == FUNCTION_NAME);
            Assert.True(mockClient.request.data == "{\"someKey\":\"someData\"}");
        }

        [Test]
        public async Task CallAndResponse_Success_NoData()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                success = true
            };
            _ = await manager.Call(FUNCTION_NAME);

            Assert.NotNull(mockClient.request);
            Assert.True(mockClient.request.fxName == FUNCTION_NAME);
            Assert.True(String.IsNullOrEmpty(mockClient.request.data));
        }

        [Test]
        public async Task CallAndResponse_Failed_NoRequestId()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                success = true
            };
            mockClient.setRequestId = false;

            Exception e = null;
            try
            {
                string response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);
            Assert.IsTrue(e.Message.Contains("Response from game bridge is incorrect") == true);
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_WithType()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                errorType = "WALLET_CONNECTION_ERROR",
                error = ERROR,
                success = false
            };

            PassportException e = null;
            try
            {
                string response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);
            Assert.AreEqual(PassportErrorType.WALLET_CONNECTION_ERROR, e.Type);
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_NoType()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                error = ERROR,
                success = false
            };

            PassportException e = null;
            try
            {
                string response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);
            Assert.Null(e.Type);
            Assert.AreEqual(ERROR, e.Message);
        }


        [Test]
        public void CallAndResponse_Success_BrowserReady()
        {
            BrowserResponse browserResponse = new BrowserResponse()
            {
                responseFor = BrowserCommunicationsManager.INIT,
                requestId = BrowserCommunicationsManager.INIT_REQUEST_ID,
                success = true
            };

            bool onReadyCalled = false;
            manager.OnReady += () => onReadyCalled = true;

            mockClient.InvokeUnityPostMessage(JsonUtility.ToJson(browserResponse));

            Assert.True(onReadyCalled);
        }

        [Test]
        public void SetCallTimeout_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => manager.SetCallTimeout(5000));
        }

        [Test]
        public void LaunchAuthURL_ForwardsUrlAndRedirectUri()
        {
            manager.LaunchAuthURL("https://auth.example.com", "myapp://callback");

            Assert.AreEqual("https://auth.example.com", mockClient.lastLaunchedUrl);
            Assert.AreEqual("myapp://callback", mockClient.lastLaunchedRedirectUri);
        }

        [Test]
        public async Task CallAndResponse_Failed_ErrorFieldSet_SuccessTrue_ThrowsException()
        {
            // success=true but an error field is present — should still be treated as failure
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                error = ERROR,
                success = true
            };

            PassportException e = null;
            try
            {
                await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
            Assert.AreEqual(ERROR, e.Message);
        }

        [Test]
        public void HandleResponse_UnknownRequestId_Throws()
        {
            // A well-formed response whose requestId was never registered via Call()
            var response = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                requestId = "unknown-request-id",
                success = true
            };

            LogAssert.Expect(LogType.Error, new Regex("No TaskCompletionSource for request id"));

            var ex = Assert.Throws<PassportException>(
                () => mockClient.InvokeUnityPostMessage(JsonUtility.ToJson(response))
            );

            Assert.IsTrue(ex.Message.Contains("No TaskCompletionSource for request id"));
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_NoErrorField()
        {
            // success=false but no error or errorType - should get "Unknown error"
            mockClient.browserResponse = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                success = false
            };

            PassportException e = null;
            try
            {
                await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
            Assert.Null(e.Type);
            Assert.AreEqual("Unknown error", e.Message);
        }
    }

    internal class MockBrowserClient : IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate OnUnityPostMessage;
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;

        public BrowserRequest request = null;
        public BrowserResponse browserResponse = null;
        public bool setRequestId = true;

        public void ExecuteJs(string js)
        {
            var json = Between(js, "callFunction(\"", "\")").Replace("\\\\", "\\").Replace("\\\"", "\"");
            request = json.OptDeserializeObject<BrowserRequest>();
            if (setRequestId && browserResponse != null)
            {
                browserResponse.requestId = request.requestId;
            }
            InvokeUnityPostMessage(JsonUtility.ToJson(browserResponse));
        }

        internal void InvokeUnityPostMessage(string message)
        {
            if (OnUnityPostMessage != null)
            {
                OnUnityPostMessage.Invoke(message);
            }
        }

        private string Between(string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            int posB = value.LastIndexOf(b);
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        public string lastLaunchedUrl;
        public string lastLaunchedRedirectUri;

        public void LaunchAuthURL(string url, string redirectUri)
        {
            lastLaunchedUrl = url;
            lastLaunchedRedirectUri = redirectUri;
        }

        public void Dispose()
        {
        }
    }
}