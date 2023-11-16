using System;
using NUnit.Framework;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Events;
#endif
using Immutable.Passport.Json;

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
            Assert.IsTrue(e.Message.Contains("Response from browser is incorrect") == true);
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

        public void LaunchAuthURL(string url, string redirectUri)
        {
            throw new NotImplementedException();
        }
    }
}