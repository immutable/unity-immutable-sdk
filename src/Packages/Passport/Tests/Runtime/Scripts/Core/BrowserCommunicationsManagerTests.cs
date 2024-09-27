using System;
using System.Threading.Tasks;
using Immutable.Browser.Core;
using Immutable.Passport.Helpers;
using Immutable.Passport.Model;
using NUnit.Framework;
using UnityEngine;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class BrowserCommunicationsManagerTests
    {
        [SetUp]
        public void Init()
        {
            mockClient = new MockBrowserClient();
            manager = new BrowserCommunicationsManager(mockClient);
        }

        private const string FUNCTION_NAME = "someFunction";
        private const string ERROR = "some error";

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
            Assert.True(string.IsNullOrEmpty(mockClient.request.data));
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
                var response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);
            Assert.IsTrue(e.Message.Contains("Response from browser is incorrect"));
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
                var response = await manager.Call(FUNCTION_NAME);
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
                var response = await manager.Call(FUNCTION_NAME);
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
            var browserResponse = new BrowserResponse
            {
                responseFor = BrowserCommunicationsManager.INIT,
                requestId = BrowserCommunicationsManager.INIT_REQUEST_ID,
                success = true
            };

            var onReadyCalled = false;
            manager.OnReady += () => onReadyCalled = true;

            mockClient.InvokeUnityPostMessage(JsonUtility.ToJson(browserResponse));

            Assert.True(onReadyCalled);
        }

#pragma warning disable CS8618
        private BrowserCommunicationsManager manager;
        private MockBrowserClient mockClient;
#pragma warning restore CS8618
    }

    internal class MockBrowserClient : IWebBrowserClient
    {
        public BrowserResponse browserResponse;

        public BrowserRequest request;
        public bool setRequestId = true;
        public event OnUnityPostMessageDelegate OnUnityPostMessage;
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;

        public void ExecuteJs(string js)
        {
            var json = Between(js, "callFunction(\"", "\")").Replace("\\\\", "\\").Replace("\\\"", "\"");
            request = json.OptDeserializeObject<BrowserRequest>();
            if (setRequestId && browserResponse != null) browserResponse.requestId = request.requestId;
            InvokeUnityPostMessage(JsonUtility.ToJson(browserResponse));
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
            throw new NotImplementedException();
        }

        internal void InvokeUnityPostMessage(string message)
        {
            if (OnUnityPostMessage != null) OnUnityPostMessage.Invoke(message);
        }

        private string Between(string value, string a, string b)
        {
            var posA = value.IndexOf(a);
            var posB = value.LastIndexOf(b);
            if (posA == -1) return "";
            if (posB == -1) return "";
            var adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB) return "";
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        public void Dispose()
        {
        }
    }
}