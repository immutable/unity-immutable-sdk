using System;
using NUnit.Framework;
using Immutable.Passport;
using Immutable.Passport.Core;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using UnityEngine;

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
        internal static string EMAIL = "unity@immutable.com";
        internal static string SIGNATURE = "0xsignature";
        internal static string MESSAGE = "message";
        private const string REQUEST_ID = "50";

#pragma warning disable CS8618
        private MockBrowserCommsManager communicationsManager;
        private PassportImpl passport;
#pragma warning restore CS8618

        [SetUp]
        public void Init()
        {
            communicationsManager = new MockBrowserCommsManager();
            passport = new PassportImpl(communicationsManager);
        }

        [Test]
        public async Task GetAddress_Success()
        {
            var response = new StringResponse
            {
                success = true,
                result = ADDRESS
            };
            communicationsManager.response = JsonUtility.ToJson(response);
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
        public async Task GetEmail_Success()
        {
            var response = new StringResponse
            {
                success = true,
                result = EMAIL
            };
            communicationsManager.response = JsonUtility.ToJson(response);
            var email = await passport.GetEmail();
            Assert.AreEqual(EMAIL, email);
            Assert.AreEqual(PassportFunction.GET_EMAIL, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }

        [Test]
        public async Task GetEmail_Failed()
        {
            communicationsManager.response = "";
            var email = await passport.GetEmail();
            Assert.Null(email);
            Assert.AreEqual(PassportFunction.GET_EMAIL, communicationsManager.fxName);
            Assert.True(String.IsNullOrEmpty(communicationsManager.data));
        }
    }

    internal class MockBrowserCommsManager : IBrowserCommunicationsManager
    {
        public string response = "";
        public string fxName = "";
        public string data = "";
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;

        public UniTask<string> Call(string fxName, string data = null, bool ignoreTimeout = false)
        {
            this.fxName = fxName;
            this.data = data;
            return UniTask.FromResult(response);
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