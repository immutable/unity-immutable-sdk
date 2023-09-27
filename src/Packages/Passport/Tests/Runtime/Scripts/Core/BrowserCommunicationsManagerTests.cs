using System;
using NUnit.Framework;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Events;
using Newtonsoft.Json;

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
                ResponseFor = FUNCTION_NAME,
                Success = true
            };
            _ = await manager.Call(FUNCTION_NAME, "{\"someKey\":\"someData\"}");

            Assert.True(mockClient.request?.FxName == FUNCTION_NAME);
            Assert.True(mockClient.request?.Data == "{\"someKey\":\"someData\"}");
        }

        [Test]
        public async Task CallAndResponse_Success_NoData()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                ResponseFor = FUNCTION_NAME,
                Success = true
            };
            _ = await manager.Call(FUNCTION_NAME);

            Assert.True(mockClient.request?.FxName == FUNCTION_NAME);
            Assert.True(String.IsNullOrEmpty(mockClient.request?.Data));
        }

        [Test]
        public async Task CallAndResponse_Failed_NoRequestId()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                ResponseFor = FUNCTION_NAME,
                Success = true
            };
            mockClient.setRequestId = false;

            Exception? e = null;
            try
            {
                string response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);
            Assert.IsTrue(e?.Message.Contains("Response from browser is incorrect") == true);
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_WithType()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                ResponseFor = FUNCTION_NAME,
                ErrorType = "WALLET_CONNECTION_ERROR",
                Error = ERROR,
                Success = false
            };

            PassportException? e = null;
            try
            {
                string response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.AreEqual(PassportErrorType.WALLET_CONNECTION_ERROR, e?.Type);
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_NoType()
        {
            mockClient.browserResponse = new BrowserResponse
            {
                ResponseFor = FUNCTION_NAME,
                Error = ERROR,
                Success = false
            };

            PassportException? e = null;
            try
            {
                string response = await manager.Call(FUNCTION_NAME);
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.Null(e?.Type);
            Assert.AreEqual(ERROR, e?.Message);
        }


        [Test]
        public void CallAndResponse_Success_BrowserReady()
        {
            BrowserResponse browserResponse = new()
            {
                ResponseFor = BrowserCommunicationsManager.INIT,
                RequestId = BrowserCommunicationsManager.INIT_REQUEST_ID,
                Success = true
            };

            bool onReadyCalled = false;
            manager.OnReady += () => onReadyCalled = true;

            mockClient.InvokeUnityPostMessage(JsonConvert.SerializeObject(browserResponse));

            Assert.True(onReadyCalled);
        }
    }

    internal class MockBrowserClient : IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate? OnUnityPostMessage;
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;

        public BrowserRequest? request = null;
        public BrowserResponse? browserResponse = null;
        public bool setRequestId = true;

        public void ExecuteJs(string js)
        {
            var json = Between(js, "callFunction(\"", "\")").Replace("\\\\", "\\").Replace("\\\"", "\"");
            request = JsonConvert.DeserializeObject<BrowserRequest>(json);
            if (setRequestId && browserResponse != null)
            {
                browserResponse.RequestId = request.RequestId;
            }
            InvokeUnityPostMessage(JsonConvert.SerializeObject(browserResponse));
        }

        internal void InvokeUnityPostMessage(string message)
        {
            OnUnityPostMessage?.Invoke(message);
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
            return value[adjustedPosA..posB];
        }

        public void LaunchAuthURL(string url)
        {
            throw new NotImplementedException();
        }
    }
}