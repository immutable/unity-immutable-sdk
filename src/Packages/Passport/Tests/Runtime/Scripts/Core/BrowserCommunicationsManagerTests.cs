using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Storage;
using Immutable.Passport.Auth;
using Immutable.Passport.Model;
using Immutable.Passport.Utility.Tests;
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
        private BrowserCommunicationsManager manager;
        private MockBrowserClient mockClient;
        
        [SetUp] 
        public void Init()
        { 
            mockClient = new MockBrowserClient();
            manager = new BrowserCommunicationsManager(mockClient);
        }

        [Test]
        public async Task CallAndResponse_Success_WithData()
        {
            mockClient.browserResponse = new Response();
            mockClient.browserResponse.responseFor = "someFunction";
            mockClient.browserResponse.success = true;
            string response = await manager.Call("someFunction", "{\"someKey\":\"someData\"}");

            Assert.True(mockClient.request.fxName == "someFunction");
            Assert.True(mockClient.request.data == "{\"someKey\":\"someData\"}");
        }

        [Test]
        public async Task CallAndResponse_Success_NoData()
        {
            mockClient.browserResponse = new Response();
            mockClient.browserResponse.responseFor = "someFunction";
            mockClient.browserResponse.success = true;
            string response = await manager.Call("someFunction");

            Assert.True(mockClient.request.fxName == "someFunction");
            Assert.True(String.IsNullOrEmpty(mockClient.request.data));
        }

        [Test]
        public async Task CallAndResponse_Failed_NoRequestId()
        {
            mockClient.browserResponse = new Response();
            mockClient.browserResponse.responseFor = "someFunction";
            mockClient.browserResponse.success = true;
            mockClient.setRequestId = false;

            Exception? e = null;
            try {
                string response = await manager.Call("someFunction");
            } catch (PassportException exception) {
                e = exception;
            }

            Assert.NotNull(e);
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_WithType()
        {
            mockClient.browserResponse = new Response();
            mockClient.browserResponse.responseFor = "someFunction";
            mockClient.browserResponse.errorType = "WALLET_CONNECTION_ERROR";
            mockClient.browserResponse.error = "some error";
            mockClient.browserResponse.success = false;
            
            PassportException? e = null;
            try {
                string response = await manager.Call("someFunction");
            } catch (PassportException exception) {
                e = exception;
            }
            
            Assert.AreEqual(PassportErrorType.WALLET_CONNECTION_ERROR, e.Type);
        }

        [Test]
        public async Task CallAndResponse_Failed_ClientError_NoType()
        {
            mockClient.browserResponse = new Response();
            mockClient.browserResponse.responseFor = "someFunction";
            mockClient.browserResponse.error = "some error";
            mockClient.browserResponse.success = false;
            
            PassportException? e = null;
            try {
                string response = await manager.Call("someFunction");
            } catch (PassportException exception) {
                e = exception;
            }
            
            Assert.Null(e.Type);
            Assert.AreEqual("some error", e.Message);
        }
    }

    internal class MockBrowserClient : IWebBrowserClient {
        public event OnUnityPostMessageDelegate OnUnityPostMessage;
        public Request? request = null;
        public Response? browserResponse = null;
        public bool setRequestId = true;

        public void ExecuteJs(string js)
        {
            var json = Between(js, "callFunction(\"", "\")").Replace("\\\\", "\\").Replace("\\\"", "\"");
            Debug.Log("ExecuteJs: " + js + "\nJSON: " + json);
            Debug.Log(json);
            request = JsonUtility.FromJson<Request>(json);
            if (setRequestId)
                browserResponse.requestId = request.requestId;
            OnUnityPostMessage?.Invoke(JsonConvert.SerializeObject(browserResponse));
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
    }
}