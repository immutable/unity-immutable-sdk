using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Storage;
using Immutable.Passport.Auth;
using UnityEngine;

namespace Immutable.Passport.Storage
{
    [TestFixture]
    public class CredentialsManagerTests
    {
        internal static string ACCESS_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ikp" +
            "vaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjcyMDB9.zKW_cyLXjQ0Vbc7LsrHGo6fIUfCy9QQhFNdKN5JxlZY";
        internal static string REFRESH_TOKEN = "refreshToken";
        internal static string ID_TOKEN = "idToken";
        internal static string TOKEN_TYPE = "Bearer";
        internal static int EXPIRES_IN = 86400;
        internal static string SET_STRING = "SetString";
        internal static string GET_STRING = "GetString";
        internal static string DELETE_KEY = "DeleteKey";
        internal static string TOKEN_JSON = $@"{{""access_token"":""{ACCESS_TOKEN}"",""refresh_token"":""{REFRESH_TOKEN}""," +
            $@"""id_token"":""{ID_TOKEN}"",""token_type"":""{TOKEN_TYPE}"",""expires_in"":{EXPIRES_IN}}}";

        private TokenResponse tokenResponse = new TokenResponse() {
            access_token = ACCESS_TOKEN,
            refresh_token = REFRESH_TOKEN,
            id_token = ID_TOKEN,
            token_type = TOKEN_TYPE,
            expires_in = EXPIRES_IN
        };
        
        private string playPrefsFunctionCalled = null;
        private string[] playPrefsFunctionValues = null;
        private TestableCredentialsManager manager;

        [SetUp] 
        public void Init()
        { 
            manager = new TestableCredentialsManager(OnPlayerPrefsCalled);
        }
        
        private void OnPlayerPrefsCalled(string function, string[] values) {
            playPrefsFunctionCalled = function;
            playPrefsFunctionValues = values;
        }

        [Test]
        public void SaveCredentialsTest()
        {
            manager.SaveCredentials(tokenResponse);

            Assert.AreEqual(SET_STRING, playPrefsFunctionCalled);
            Assert.AreEqual(new string[]{CredentialsManager.KEY_PREFS_CREDENTIALS, TOKEN_JSON}, playPrefsFunctionValues);
        }

        [Test]
        public void GetCredentialsTest()
        {
            TokenResponse? actualTokenResponse = manager.GetCredentials();

            Assert.AreEqual(GET_STRING, playPrefsFunctionCalled);
            Assert.AreEqual(new string[]{CredentialsManager.KEY_PREFS_CREDENTIALS, ""}, playPrefsFunctionValues);
            Assert.AreEqual(ACCESS_TOKEN, actualTokenResponse.access_token);
            Assert.AreEqual(REFRESH_TOKEN, actualTokenResponse.refresh_token);
            Assert.AreEqual(ID_TOKEN, actualTokenResponse.id_token);
            Assert.AreEqual(TOKEN_TYPE, actualTokenResponse.token_type);
            Assert.AreEqual(EXPIRES_IN, actualTokenResponse.expires_in);
        }

        [Test]
        public void GetCredentialsTest_EmptyJson()
        {
            manager.mockTokenJson = "{}";
            TokenResponse? actualTokenResponse = manager.GetCredentials();
            Assert.Null(actualTokenResponse);

            manager.mockTokenJson = "";
            actualTokenResponse = manager.GetCredentials();
            Assert.Null(actualTokenResponse);
        }

        [Test]
        public void ClearCredentialsTest()
        {
            manager.ClearCredentials();

            Assert.AreEqual(DELETE_KEY, playPrefsFunctionCalled);
            Assert.AreEqual(new string[]{CredentialsManager.KEY_PREFS_CREDENTIALS}, playPrefsFunctionValues);
        }

        [Test]
        public void HasValidCredentialsTest_Valid()
        {
            Assert.True(manager.HasValidCredentials());
        }

        [Test]
        public void HasValidCredentialsTest_Invalid()
        {
            manager.mockCurrentTimeSeconds = 3600;
            Assert.False(manager.HasValidCredentials());

            manager.mockCurrentTimeSeconds = 9999;
            Assert.False(manager.HasValidCredentials());
        }

        [Test]
        public void HasValidCredentialsTest_NoCredentials()
        {
            manager.mockTokenJson = "";
            Assert.False(manager.HasValidCredentials());
        }
    }

    delegate void PlayerPrefsDelegate(string function, string[] values);

    class TestableCredentialsManager : CredentialsManager {

        private PlayerPrefsDelegate onPlayerPrefs;
        public string? mockTokenJson;
        public long mockCurrentTimeSeconds = 0;

        public TestableCredentialsManager(PlayerPrefsDelegate onPlayerPrefs) {
            this.onPlayerPrefs = onPlayerPrefs;
        }

        protected override long GetCurrentTimeSeconds()
        {
            return mockCurrentTimeSeconds;
        }

        protected override TokenResponse DeserializeTokenResponse(string json) {
            return new TokenResponse() {
                access_token = CredentialsManagerTests.ACCESS_TOKEN,
                refresh_token = CredentialsManagerTests.REFRESH_TOKEN,
                id_token = CredentialsManagerTests.ID_TOKEN,
                token_type = CredentialsManagerTests.TOKEN_TYPE,
                expires_in = CredentialsManagerTests.EXPIRES_IN
            };
        } 

        protected override void SetStringToPlayerPrefs(string key, string value) {
            onPlayerPrefs(CredentialsManagerTests.SET_STRING, new string[]{key, value});
        }

        protected override string GetStringFromPlayerPrefs(string key, string defaultValue) {
            onPlayerPrefs(CredentialsManagerTests.GET_STRING, new string[]{key, defaultValue});
            return mockTokenJson ?? CredentialsManagerTests.TOKEN_JSON;
        }

        protected override void DeleteKeyFromPlayerPrefs(string key) {
            onPlayerPrefs(CredentialsManagerTests.DELETE_KEY, new string[]{key});
        }
    }
}