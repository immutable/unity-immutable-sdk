using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Model;
using UnityEngine;
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
#if UNITY_EDITOR_WIN
            manager = new BrowserCommunicationsManager(mockClient);
#else
            manager = new BrowserCommunicationsManager();
#endif
        }

        [Test]
        public async Task CallAndResponse_Success_WithData()
        {
            mockClient.browserResponse = new Response
            {
                responseFor = FUNCTION_NAME,
                success = true
            };
            _ = await manager.Call(FUNCTION_NAME, "{\"someKey\":\"someData\"}");

            Assert.True(mockClient.request?.fxName == FUNCTION_NAME);
            Assert.True(mockClient.request?.data == "{\"someKey\":\"someData\"}");
        }

        [Test]
        public async Task CallAndResponse_Success_NoData()
        {
            mockClient.browserResponse = new Response
            {
                responseFor = FUNCTION_NAME,
                success = true
            };
            _ = await manager.Call(FUNCTION_NAME);

            Assert.True(mockClient.request?.fxName == FUNCTION_NAME);
            Assert.True(String.IsNullOrEmpty(mockClient.request?.data));
        }

        [Test]
        public async Task CallAndResponse_Failed_NoRequestId()
        {
            mockClient.browserResponse = new Response
            {
                responseFor = FUNCTION_NAME,
                success = true
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
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_WithType()
        {
            mockClient.browserResponse = new Response
            {
                responseFor = FUNCTION_NAME,
                errorType = "WALLET_CONNECTION_ERROR",
                error = ERROR,
                success = false
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
            mockClient.browserResponse = new Response
            {
                responseFor = FUNCTION_NAME,
                error = ERROR,
                success = false
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
            Response browserResponse = new()
            {
                responseFor = BrowserCommunicationsManager.INIT,
                requestId = BrowserCommunicationsManager.INIT_REQUEST_ID,
                success = true
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
        public Request? request = null;
        public Response? browserResponse = null;
        public bool setRequestId = true;

        public void ExecuteJs(string js)
        {
            var json = Between(js, "callFunction(\"", "\")").Replace("\\\\", "\\").Replace("\\\"", "\"");
            Debug.Log("ExecuteJs: " + js + "\nJSON: " + json);
            request = JsonUtility.FromJson<Request>(json);
            if (setRequestId && browserResponse != null)
            {
                browserResponse.requestId = request.requestId;
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
    }
}