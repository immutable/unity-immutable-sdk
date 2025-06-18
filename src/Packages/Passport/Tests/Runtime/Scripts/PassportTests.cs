using System;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Core;
using Immutable.Passport.Model;
using Immutable.Passport.Event;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using UnityEngine;
using Immutable.Passport.Helpers;
using UnityEngine.TestTools;

namespace Immutable.Passport
{
    class TestPassportImpl : PassportImpl
    {
        private List<string> urlsOpened;
        public TestPassportImpl(IBrowserCommunicationsManager communicationsManager, List<string> urlsOpened) : base(communicationsManager)
        {
            this.urlsOpened = urlsOpened;
        }

        protected override void OpenUrl(string url)
        {
            urlsOpened.Add(url);
        }

        protected override void Track(string eventName, bool? success = null, Dictionary<string, object> properties = null)
        {
        }
    }

    [TestFixture]
    public class PassportImplTests
    {
        internal static string ACCESS_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ikp" +
            "vaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjEyM30.kRqQkJudxgI3koJAp9K4ENp6E2ExFQ5VchogaTWx6Fk";
        internal static string ACCESS_TOKEN_KEY = "accessToken";
        internal static string REFRESH_TOKEN = "refreshToken";
        internal static string REFRESH_TOKEN_KEY = "refreshToken";
        internal static string ID_TOKEN = "idToken";
        internal static string ID_TOKEN_KEY = "idToken";
        internal static string ADDRESS = "0xaddress";
        internal static string ADDRESS2 = "0xaddress2";
        internal static string EMAIL = "unity@immutable.com";
        internal static string PASSPORT_ID = "email|123457890";
        internal static string SIGNATURE = "0xsignature";
        internal static string MESSAGE = "message";
        internal static string URL = "https://auth.immutable.com/device";
        internal static string LOGOUT_URL = "https://auth.immutable.com/logout";
        internal static int INTERVAL = 5;

#pragma warning disable CS8618
        private MockBrowserCommunicationsManager communicationsManager;
        private TestPassportImpl passport;
#pragma warning restore CS8618

        private List<string> urlsOpened;
        private List<PassportAuthEvent> authEvents;

        [SetUp]
        public void Init()
        {
            communicationsManager = new MockBrowserCommunicationsManager();
            urlsOpened = new List<string>();
            authEvents = new List<PassportAuthEvent>();
            passport = new TestPassportImpl(communicationsManager, urlsOpened);
            passport.OnAuthEvent += OnPassportAuthEvent;
            communicationsManager.Responses.Clear();
        }

        [TearDown]
        public void Cleanup()
        {
            passport.OnAuthEvent -= OnPassportAuthEvent;
        }

        private void OnPassportAuthEvent(PassportAuthEvent authEvent)
        {
            Debug.Log($"OnPassportAuthEvent {authEvent.ToString()}");
            authEvents.Add(authEvent);
        }

        [Test]
        public async Task Relogin_Failed()
        {
            var reloginResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reloginResponse);

            bool success = await passport.Login(useCachedSession: true);
            Assert.False(success);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.ReloggingIn,
                    PassportAuthEvent.ReloginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Relogin_CallFailed()
        {
            communicationsManager.ThrowExceptionOnCall = true;

            bool success = await passport.Login(useCachedSession: true);
            Assert.False(success);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.ReloggingIn,
                    PassportAuthEvent.ReloginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Relogin_NullResponse_Failed()
        {
            bool success = await passport.Login(useCachedSession: true);
            Assert.False(success);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.ReloggingIn,
                    PassportAuthEvent.ReloginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Reconnect_Failed()
        {
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);

            var success = await passport.ConnectImx(useCachedSession: true);
            Assert.False(success);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Reconnect_CallFailed()
        {
            communicationsManager.ThrowExceptionOnCall = true;

            bool success = await passport.ConnectImx(useCachedSession: true);
            Assert.False(success);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Reconnect_NullResponse_Failed()
        {
            bool success = await passport.ConnectImx(useCachedSession: true);
            Assert.False(success);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task GetAddress_Success()
        {
            var response = new StringResponse
            {
                success = true,
                result = ADDRESS
            };
            communicationsManager.AddMockResponse(response);

            var address = await passport.GetAddress();

            Assert.AreEqual(ADDRESS, address);
            Assert.AreEqual(PassportFunction.IMX.GET_ADDRESS, communicationsManager.FxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.Data));
        }

        [Test]
        public async Task GetAddress_Failed()
        {
            communicationsManager.ThrowExceptionOnCall = true;

            PassportException e = null;
            try
            {
                var address = await passport.GetAddress();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
        }

        [Test]
        public async Task GetEmail_Success()
        {
            var response = new StringResponse
            {
                success = true,
                result = EMAIL
            };
            communicationsManager.AddMockResponse(response);

            var email = await passport.GetEmail();

            Assert.AreEqual(EMAIL, email);
            Assert.AreEqual(PassportFunction.GET_EMAIL, communicationsManager.FxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.Data));
        }

        [Test]
        public async Task GetEmail_Failed()
        {
            communicationsManager.ThrowExceptionOnCall = true;

            PassportException e = null;
            try
            {
                var email = await passport.GetEmail();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
        }

        [Test]
        public async Task GetPassportId_Success()
        {
            var response = new StringResponse
            {
                success = true,
                result = PASSPORT_ID
            };
            communicationsManager.AddMockResponse(response);

            var passportId = await passport.GetPassportId();

            Assert.AreEqual(PASSPORT_ID, passportId);
            Assert.AreEqual(PassportFunction.GET_PASSPORT_ID, communicationsManager.FxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.Data));
        }

        [Test]
        public async Task GetPassportId_Failed()
        {
            communicationsManager.ThrowExceptionOnCall = true;

            PassportException e = null;
            try
            {
                var passportId = await passport.GetPassportId();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
        }

        [Test]
        public async Task GetLinkedAddresses_Success()
        {
            string[] result = { ADDRESS, ADDRESS2 };
            var response = new StringListResponse
            {
                success = true,
                result = result
            };
            communicationsManager.AddMockResponse(response);

            var linkedAddresses = await passport.GetLinkedAddresses();

            Assert.AreEqual(2, linkedAddresses.Count);
            Assert.AreEqual(ADDRESS, linkedAddresses[0]);
            Assert.AreEqual(ADDRESS2, linkedAddresses[1]);
        }

        [Test]
        public async Task GetLinkedAddresses_Failed()
        {
            communicationsManager.ThrowExceptionOnCall = true;

            PassportException e = null;
            try
            {
                var passportId = await passport.GetLinkedAddresses();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
        }

        [Test]
        public async Task GetLinkedAddresses_NullResponse()
        {
            var response = new StringListResponse
            {
                success = true,
                result = null
            };
            communicationsManager.AddMockResponse(response);

            var linkedAddresses = await passport.GetLinkedAddresses();

            Assert.AreEqual(0, linkedAddresses.Count);
        }

        [Test]
        public async Task GetLinkedAddresses_EmptyResponse()
        {
            string[] result = { };
            var response = new StringListResponse
            {
                success = true,
                result = result
            };
            communicationsManager.AddMockResponse(response);

            var linkedAddresses = await passport.GetLinkedAddresses();

            Assert.AreEqual(0, linkedAddresses.Count);
        }
    }

    internal class MockBrowserCommunicationsManager : IBrowserCommunicationsManager
    {
        public Queue<string> Responses = new();
        public bool ThrowExceptionOnCall;
        public string FxName = "";
        public string? Data = "";
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;

        public void AddMockResponse(object response)
        {
            Responses.Enqueue(JsonUtility.ToJson(response));
        }

        public UniTask<string> Call(string fxName, string? data = null, bool ignoreTimeout = false, long? timeoutMs = null)
        {
            if (ThrowExceptionOnCall)
            {
                Debug.Log("Error on call");
                throw new PassportException("Error on call!");
            }

            FxName = fxName;
            Data = data;
            var result = Responses.Count > 0 ? Responses.Dequeue() : "";
            var response = result.OptDeserializeObject<BrowserResponse>();
            if (response == null || response?.success == false || !string.IsNullOrEmpty(response?.error))
            {
                Debug.Log("No response");
                throw new PassportException("Response is invalid!");
            }

            return UniTask.FromResult(result);
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
        }

        public void SetCallTimeout(int ms)
        {
        }
    }
}