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
        internal static string DEVICE_CODE = "deviceCode";
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
        internal static string CODE = "IMX";
        internal static string URL = "https://auth.immutable.com/device";
        internal static string LOGOUT_URL = "https://auth.immutable.com/logout";
        internal static int INTERVAL = 5000;
        private const string REQUEST_ID = "50";

#pragma warning disable CS8618
        private MockBrowserCommsManager communicationsManager;
        private TestPassportImpl passport;
#pragma warning restore CS8618

        private List<string> urlsOpened;
        private List<PassportAuthEvent> authEvents;

        [SetUp]
        public void Init()
        {
            communicationsManager = new MockBrowserCommsManager();
            urlsOpened = new List<string>();
            authEvents = new List<PassportAuthEvent>();
            passport = new TestPassportImpl(communicationsManager, urlsOpened);
            passport.OnAuthEvent += OnPassportAuthEvent;
            communicationsManager.responses.Clear();
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
        public async Task Login_Logout_Success()
        {
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);
            var confirmCodeResponse = new BrowserResponse
            {
                success = true
            };
            communicationsManager.AddMockResponse(confirmCodeResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Login
            bool success = await passport.Login();
            Assert.True(success);

            // Logout
            await passport.Logout();

            Assert.AreEqual(2, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            Assert.AreEqual(LOGOUT_URL, urlsOpened[1]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingIn,
                    PassportAuthEvent.LoginOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLogin,
                    PassportAuthEvent.LoginSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Login_Soft_Logout_Success()
        {
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);
            var confirmCodeResponse = new BrowserResponse
            {
                success = true
            };
            communicationsManager.AddMockResponse(confirmCodeResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Login
            bool success = await passport.Login();
            Assert.True(success);

            // Logout
            await passport.Logout(hardLogout: false);

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingIn,
                    PassportAuthEvent.LoginOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLogin,
                    PassportAuthEvent.LoginSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Login_InitialiseDeviceCodeAuth_Failed()
        {
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);

            PassportException e = null;
            try
            {
                await passport.Login();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingIn,
                    PassportAuthEvent.LoginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Login_InitialiseDeviceCodeAuth_NullResponse_Failed()
        {
            PassportException e = null;
            try
            {
                await passport.Login();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingIn,
                    PassportAuthEvent.LoginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Login_ConfirmCode_Failed()
        {
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);
            var confirmCodeResponse = new BrowserResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(confirmCodeResponse);

            PassportException e = null;
            try
            {
                await passport.Login();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingIn,
                    PassportAuthEvent.LoginOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLogin,
                    PassportAuthEvent.LoginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Login_ConfirmCode_NullResponse_Failed()
        {
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);

            PassportException e = null;
            try
            {
                await passport.Login();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingIn,
                    PassportAuthEvent.LoginOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLogin,
                    PassportAuthEvent.LoginFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Relogin_Success()
        {
            var reloginResponse = new BoolResponse
            {
                success = true,
                result = true
            };
            communicationsManager.AddMockResponse(reloginResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Relogin
            bool success = await passport.Login(useCachedSession: true);
            Assert.True(success);

            // Logout
            await passport.Logout();

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(LOGOUT_URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.ReloggingIn,
                    PassportAuthEvent.ReloginSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
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
            communicationsManager.throwExceptionOnCall = true;

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
        public async Task ConnectImx_Logout_Success()
        {
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);
            var confirmCodeResponse = new BrowserResponse
            {
                success = true
            };
            communicationsManager.AddMockResponse(confirmCodeResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Connect
            bool success = await passport.ConnectImx();
            Assert.True(success);

            // Logout
            await passport.Logout();

            Assert.AreEqual(2, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            Assert.AreEqual(LOGOUT_URL, urlsOpened[1]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed,
                    PassportAuthEvent.ConnectingImx,
                    PassportAuthEvent.ConnectImxOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLoginAndProviderSetup,
                    PassportAuthEvent.ConnectImxSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task ConnectImx_InitialiseDeviceCodeAuth_Failed()
        {
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);

            PassportException e = null;
            try
            {
                await passport.ConnectImx();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed,
                    PassportAuthEvent.ConnectingImx,
                    PassportAuthEvent.ConnectImxFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task ConnectImx_InitialiseDeviceCodeAuth_NullResponse_Failed()
        {
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);

            PassportException e = null;
            try
            {
                await passport.ConnectImx();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed,
                    PassportAuthEvent.ConnectingImx,
                    PassportAuthEvent.ConnectImxFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task ConnectImx_ConfirmCode_Failed()
        {
            communicationsManager.AddMockResponse(new StringResponse
            {
                success = true
            });
            communicationsManager.AddMockResponse(new StringResponse
            {
                success = true
            });
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);
            var confirmCodeResponse = new BrowserResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(confirmCodeResponse);

            PassportException e = null;
            try
            {
                await passport.ConnectImx();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed,
                    PassportAuthEvent.ConnectingImx,
                    PassportAuthEvent.ConnectImxOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLoginAndProviderSetup,
                    PassportAuthEvent.ConnectImxFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task ConnectImx_ConfirmCode_NullResponse_Failed()
        {
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true
            }));
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);

            PassportException e = null;
            try
            {
                await passport.ConnectImx();
            }
            catch (PassportException exception)
            {
                e = exception;
            }

            Assert.NotNull(e);

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed,
                    PassportAuthEvent.ConnectingImx,
                    PassportAuthEvent.ConnectImxOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLoginAndProviderSetup,
                    PassportAuthEvent.ConnectImxFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task ConnectImx_HasCredentialsSaved_CannotReconnect_Logout_Success()
        {
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true,
                result = ACCESS_TOKEN
            }));
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true,
                result = ID_TOKEN
            }));
            var reconnectResponse = new BoolResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var deviceConnectResponse = new DeviceConnectResponse
            {
                success = true,
                code = CODE,
                deviceCode = DEVICE_CODE,
                url = URL
            };
            communicationsManager.AddMockResponse(deviceConnectResponse);
            var confirmCodeResponse = new BrowserResponse
            {
                success = true
            };
            communicationsManager.AddMockResponse(confirmCodeResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Connect
            bool success = await passport.ConnectImx();
            Assert.True(success);

            // Logout
            await passport.Logout();

            Assert.AreEqual(2, urlsOpened.Count);
            Assert.AreEqual(URL, urlsOpened[0]);
            Assert.AreEqual(LOGOUT_URL, urlsOpened[1]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectFailed,
                    PassportAuthEvent.ConnectingImx,
                    PassportAuthEvent.ConnectImxOpeningBrowser,
                    PassportAuthEvent.PendingBrowserLoginAndProviderSetup,
                    PassportAuthEvent.ConnectImxSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task ConnectImx_HasCredentialsSaved_Reconnect_Logout_Success()
        {
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true,
                result = ACCESS_TOKEN
            }));
            communicationsManager.responses.Enqueue(JsonUtility.ToJson(new StringResponse
            {
                success = true,
                result = ID_TOKEN
            }));
            var reconnectResponse = new BoolResponse
            {
                success = true
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Login
            bool success = await passport.ConnectImx();
            Assert.True(success);

            // Logout
            await passport.Logout();

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(LOGOUT_URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.CheckingForSavedCredentials,
                    PassportAuthEvent.CheckForSavedCredentialsSuccess,
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Reconnect_Success()
        {
            var reconnectResponse = new BoolResponse
            {
                success = true
            };
            communicationsManager.AddMockResponse(reconnectResponse);
            var logoutResponse = new StringResponse
            {
                success = true,
                result = LOGOUT_URL
            };
            communicationsManager.AddMockResponse(logoutResponse);

            // Reconnect
            bool success = await passport.ConnectImx(useCachedSession: true);
            Assert.True(success);

            // Logout
            await passport.Logout();

            Assert.AreEqual(1, urlsOpened.Count);
            Assert.AreEqual(LOGOUT_URL, urlsOpened[0]);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.Reconnecting,
                    PassportAuthEvent.ReconnectSuccess,
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
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
        public async Task Reconnect_CallFailed()
        {
            communicationsManager.throwExceptionOnCall = true;

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
            Assert.AreEqual(PassportFunction.IMX.GET_ADDRESS, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }

        [Test]
        public async Task GetAddress_Failed()
        {
            communicationsManager.throwExceptionOnCall = true;

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
            Assert.AreEqual(PassportFunction.GET_EMAIL, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }

        [Test]
        public async Task GetEmail_Failed()
        {
            communicationsManager.throwExceptionOnCall = true;

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
            Assert.AreEqual(PassportFunction.GET_PASSPORT_ID, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }

        [Test]
        public async Task GetPassportId_Failed()
        {
            communicationsManager.throwExceptionOnCall = true;

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
        public async Task Logout_Success()
        {
            var response = new StringResponse
            {
                success = true,
                result = URL
            };
            communicationsManager.AddMockResponse(response);

            PassportException e = null;
            try
            {
                await passport.Logout();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.Null(e);

            Assert.AreEqual(1, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutSuccess
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Logout_FailedGetLogoutUrl()
        {
            var response = new StringResponse
            {
                success = false
            };
            communicationsManager.AddMockResponse(response);

            PassportException e = null;
            try
            {
                await passport.Logout();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
        }

        [Test]
        public async Task Logout_EmptyGetLogoutUrl()
        {
            var response = new StringResponse
            {
                success = true,
                result = ""
            };
            communicationsManager.AddMockResponse(response);

            PassportException e = null;
            try
            {
                await passport.Logout();
            }
            catch (PassportException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);

            Assert.AreEqual(0, urlsOpened.Count);
            List<PassportAuthEvent> expectedEvents = new List<PassportAuthEvent>{
                    PassportAuthEvent.LoggingOut,
                    PassportAuthEvent.LogoutFailed
                };
            Assert.AreEqual(expectedEvents.Count, authEvents.Count);
            Assert.AreEqual(expectedEvents, authEvents);
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
            communicationsManager.throwExceptionOnCall = true;

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

    internal class MockBrowserCommsManager : IBrowserCommunicationsManager
    {
        public Queue<string> responses = new Queue<string>();
        public bool throwExceptionOnCall = false;
        public string fxName = "";
        public string data = "";
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;

        public void AddMockResponse(object response)
        {
            responses.Enqueue(JsonUtility.ToJson(response));
        }

        public UniTask<string> Call(string fxName, string data = null, bool ignoreTimeout = false, Nullable<long> timeoutMs = null)
        {
            if (throwExceptionOnCall)
            {
                Debug.Log("Error on call");
                throw new PassportException("Error on call!");
            }
            else
            {
                this.fxName = fxName;
                this.data = data;
                string result = responses.Count > 0 ? responses.Dequeue() : "";
                BrowserResponse response = result.OptDeserializeObject<BrowserResponse>();
                if (response == null || response?.success == false || !String.IsNullOrEmpty(response?.error))
                {
                    Debug.Log("No response");
                    throw new PassportException("Response is invalid!");
                }
                else
                {
                    return UniTask.FromResult(result);
                }
            }
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
            throw new NotImplementedException();
        }

        public void SetCallTimeout(int ms)
        {
        }
    }
}