using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Storage;
using Immutable.Passport.Auth;
using Immutable.Passport.Core;
using Immutable.Passport.Model;
using Immutable.Passport.Utility.Tests;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Immutable.Passport
{
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
        internal static string SIGNATURE = "0xsignature";
        internal static string MESSAGE = "message";

        private MockAuthManager auth;
        private MockBrowserCommsManager communicationsManager;
        private PassportImpl passport;

        [SetUp] 
        public void Init()
        { 
            communicationsManager = new MockBrowserCommsManager();
            auth = new MockAuthManager();
            passport = new PassportImpl(auth, communicationsManager);
        }

        [Test]
        public async Task Connect_Success_FreshLogin()
        {
            auth.deviceCode = DEVICE_CODE;
            Assert.AreEqual(DEVICE_CODE, await passport.Connect());
        }

        [Test]
        public async Task Connect_Success_ExistingCredentials()
        {
            var response = new Response();
            response.success = true;
            auth.deviceCode = null;
            communicationsManager.response = JsonConvert.SerializeObject(response);
            auth.user = new User(ID_TOKEN, ACCESS_TOKEN, REFRESH_TOKEN);
            Assert.Null(await passport.Connect());
            Assert.AreEqual(PassportFunction.GET_IMX_PROVIDER, communicationsManager.fxName);
            Assert.True(communicationsManager.data.Contains($"\"{ID_TOKEN_KEY}\":\"{auth.user.idToken}\""));
            Assert.True(communicationsManager.data.Contains($"\"{ACCESS_TOKEN_KEY}\":\"{auth.user.accessToken}\""));
            Assert.True(communicationsManager.data.Contains($"\"{REFRESH_TOKEN_KEY}\":\"{auth.user.refreshToken}\""));
        }

        [Test]
        public async Task Connect_Failed()
        {
            auth.deviceCode = null;
            auth.user = null;

            InvalidOperationException? exception = null;
            try
            {
                Assert.Null(await passport.Connect());
            }
            catch (InvalidOperationException e) 
            {
                exception = e;
            }
            Assert.NotNull(exception);
            Assert.True(auth.logoutCalled);
        }

        [Test]
        public async Task ConfirmCode_Success()
        {
            var response = new Response();
            response.success = true;
            communicationsManager.response = JsonConvert.SerializeObject(response);
            auth.user = new User(ID_TOKEN, ACCESS_TOKEN, REFRESH_TOKEN);
            Assert.Null(await passport.Connect());
            Assert.AreEqual(PassportFunction.GET_IMX_PROVIDER, communicationsManager.fxName);
            Assert.True(communicationsManager.data.Contains($"\"{ID_TOKEN_KEY}\":\"{auth.user.idToken}\""));
            Assert.True(communicationsManager.data.Contains($"\"{ACCESS_TOKEN_KEY}\":\"{auth.user.accessToken}\""));
            Assert.True(communicationsManager.data.Contains($"\"{REFRESH_TOKEN_KEY}\":\"{auth.user.refreshToken}\""));
        }

        [Test]
        public async Task ConfirmCode_Failed()
        {
            auth.user = new User(ID_TOKEN, ACCESS_TOKEN, REFRESH_TOKEN);
            PassportException? exception = null;
            try
            {
                await passport.ConfirmCode();
            }
            catch (PassportException e) 
            {
                exception = e;
            }
            Assert.NotNull(exception);
        }

        [Test]
        public async Task GetAddress_Success()
        {
            var response = new AddressResponse();
            response.success = true;
            response.address = ADDRESS;
            communicationsManager.response = JsonConvert.SerializeObject(response);
            var address = await passport.GetAddress();
            Assert.AreEqual(ADDRESS, address);
            Assert.AreEqual(PassportFunction.GET_ADDRESS, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }

        [Test]
        public async Task GetAddress_Failed()
        {
            communicationsManager.response = "";
            var address = await passport.GetAddress();
            Assert.Null(address);
            Assert.AreEqual(PassportFunction.GET_ADDRESS, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }

        [Test]
        
        public async Task Logout_Success()
        {
            Assert.False(auth.logoutCalled);
            passport.Logout();
            Assert.True(auth.logoutCalled);
        }

        [Test]
        public async Task GetAccessToken_Success()
        {
            auth.user = new User(ID_TOKEN, ACCESS_TOKEN, REFRESH_TOKEN);
            var accessToken = passport.GetAccessToken();
            Assert.AreEqual(ACCESS_TOKEN, accessToken);
        }

        [Test]
        public async Task GetAccessToken_Failed()
        {
            auth.user = null;
            var accessToken = passport.GetAccessToken();
            Assert.Null(accessToken);
        }

        [Test]
        public void GetIdToken_Success()
        {
            auth.user = new User(ID_TOKEN, ACCESS_TOKEN, REFRESH_TOKEN);
            var idToken = passport.GetIdToken();
            Assert.AreEqual(ID_TOKEN, idToken);
        }

        [Test]
        public async Task GetIdToken_Failed()
        {
            auth.user = null;
            var idToken = passport.GetIdToken();
            Assert.Null(idToken);
        }

        [Test]
        public async Task SignMessage_Success()
        {
            var response = new StringResponse();
            response.success = true;
            response.result = SIGNATURE;
            communicationsManager.response = JsonConvert.SerializeObject(response);
            var signature = await passport.SignMessage(MESSAGE);
            Assert.AreEqual(SIGNATURE, signature);
            Assert.AreEqual(PassportFunction.SIGN_MESSAGE, communicationsManager.fxName);
            Assert.AreEqual(MESSAGE, communicationsManager.data);
        }

        [Test]
        public async Task SignMessage_Failed()
        {
            communicationsManager.response = "";
            var signature = await passport.SignMessage(MESSAGE);
            Assert.Null(signature);
            Assert.AreEqual(PassportFunction.SIGN_MESSAGE, communicationsManager.fxName);
            Assert.AreEqual(MESSAGE, communicationsManager.data);
        }
    }

    internal class MockAuthManager : IAuthManager
    {
        public User? user = null;
        public string? deviceCode = null;
        public bool logoutCalled = false;
        public bool hasCredentialsSaved = false;

        public async UniTask<string?> Login()
        {
            return deviceCode;
        }

        public void Logout()
        {
            logoutCalled = true;
        }

        public async UniTask<User> ConfirmCode()
        {
            return user;
        }

        public User? GetUser()
        {
            return user;
        }

        public bool HasCredentialsSaved()
        {
            return hasCredentialsSaved;
        }
    }

    internal class MockBrowserCommsManager : IBrowserCommunicationsManager
    {
        public string response = "";
        public string fxName = "";
        public string data = "";
        public async UniTask<string> Call(string fxName, string? data = null)
        {
            this.fxName = fxName;
            this.data = data;
            return response;
        }
    }
}