using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Storage;
using Immutable.Passport.Auth;
using Immutable.Passport.Utility.Tests;
using UnityEngine;
using System.Threading.Tasks;

namespace Immutable.Passport.Auth
{
    [TestFixture]
    public class AuthManagerTests
    {
        internal static string VALID_TOKEN_RESPONSE = "{\"access_token\":\"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjNhYVl5dGR3d2UwMzJzMXIzVElyOSJ9.eyJlbWFpbCI6ImRvbWluaWMubXVycmF5QGltbXV0YWJsZS5jb20iLCJvcmciOiJhNTdiMWYzZC1mYTU3LTRiNzgtODZkYy05ZDEyZDM1YjlhNjgiLCJldGhlcl9rZXkiOiIweGRlMDYzYmViNmNmNDhlNGMxOTcxYzc3N2M0OGY0NTU3MTA1MjU5ZWMiLCJzdGFya19rZXkiOiIweGM1NTYxZGU3Nzg4NTUxOTY0ZWQxMjI0Yzc2ZjQ5ZDk5ZmVjODkyOGQ1OWVkNTcwZTExZGIwYzk3ZGYwMTFmIiwidXNlcl9hZG1pbl9rZXkiOiIweGY4MjY4OTI0MWU3NTM1YjYyNTgyMmI4M2I1OWMxZDM3ZWUwOTBiZGMiLCJpc3MiOiJodHRwczovL2F1dGguaW1tdXRhYmxlLmNvbS8iLCJzdWIiOiJlbWFpbHw2NDJiN2Q0YWI5N2IzYWMyNDg3NzBlNTEiLCJhdWQiOlsicGxhdGZvcm1fYXBpIiwiaHR0cHM6Ly9wcm9kLmltbXV0YWJsZS5hdXRoMGFwcC5jb20vdXNlcmluZm8iXSwiaWF0IjoxNjg2ODg5MDgwLCJleHAiOjE2ODY5NzU0ODAsImF6cCI6IlpKTDdKdmV0Y0RGQk5EbGdSczVvSm94dUFVVWw2dVFqIiwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCB0cmFuc2FjdCBvZmZsaW5lX2FjY2VzcyJ9.aqFem4Pp0k91YWdYuxBo1wfCrYAHy1Y1zIqof2GMQXd_mwhGBkHotUlgFAsOIQmvO6lQG5m3cHLR9zlEsFJ_2AJuJg3NTNnF0dEx12H5hx24_aN4qDga8Q9KNDdGc3x4LAxv48PH-P6OcdMnpWekCUPAI3zZ9qWC1YWU9HaouQuBJNbUV8ujyooFGOP4YzejQf2Uyxz9wmzDiMS-e70BSmY8IPRR2A3QjOEo7oI3enqM_jylfbDo8BsDRouDbwbZrMeX-rcJ_HBY5iZEdagVpcvOmfZCaad55MI_WJcXHrDMzmLqck1fd15Oklo7fajQtiG0ByINxTmm9_0YnEy02w\",\"refresh_token\":\"v1.NOQCr0kkmy0Cky_Doia8VgJdclSKOOOrkicjZ4NFeabS5J7xNct-oRwO2H65ua0mPOhzWfIf8lhxlM1sIUBqLoc\",\"id_token\":\"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjNhYVl5dGR3d2UwMzJzMXIzVElyOSJ9.eyJkZXZlbG9wZXJfaHViIjp7ImFjdGl2YXRlZCI6dHJ1ZSwib3JnYW5pemF0aW9uIjp7ImlkIjoiYTU3YjFmM2QtZmE1Ny00Yjc4LTg2ZGMtOWQxMmQzNWI5YTY4In0sInJlc3BvbnNlSWQiOiI4MzhiangzOG1maG15cWs3bjgzOGJqeGZrMjdhZXQxMCIsInJlc3BvbnNlcyI6eyJmdW5kaW5nIjoiUHJlZmVyIG5vdCB0byBzYXkiLCJoYXZlTWludGVkTmZ0c0JlZm9yZSI6Ik5vIiwicGVvcGxlIjoiSnVzdCBtZSIsInByb2plY3RTdGFnZSI6IkNvbmNlcHQiLCJwcm9qZWN0VHlwZSI6Ik90aGVyIiwicm9sZSI6Ik90aGVyIn0sInJvbGVzIjpbXSwidXNlck1ldGFkYXRhIjp7InJlY2VpdmVNYXJrZXRpbmdFbWFpbHNDb25zZW50Ijp7ImNvbnNlbnRlZCI6ZmFsc2UsImRhdGUiOiIyMDIzLTA0LTIwVDAxOjA2OjI2LjgwOFoiLCJ2ZXJzaW9uIjoxfX19LCJwYXNzcG9ydCI6eyJldGhlcl9rZXkiOiIweGRlMDYzYmViNmNmNDhlNGMxOTcxYzc3N2M0OGY0NTU3MTA1MjU5ZWMiLCJzdGFya19rZXkiOiIweGM1NTYxZGU3Nzg4NTUxOTY0ZWQxMjI0Yzc2ZjQ5ZDk5ZmVjODkyOGQ1OWVkNTcwZTExZGIwYzk3ZGYwMTFmIiwidXNlcl9hZG1pbl9rZXkiOiIweGY4MjY4OTI0MWU3NTM1YjYyNTgyMmI4M2I1OWMxZDM3ZWUwOTBiZGMifSwibmlja25hbWUiOiJkb21pbmljLm11cnJheSIsIm5hbWUiOiJkb21pbmljLm11cnJheUBpbW11dGFibGUuY29tIiwicGljdHVyZSI6Imh0dHBzOi8vcy5ncmF2YXRhci5jb20vYXZhdGFyL2NkOTkwYWNiYzYwNWQ4ZDdiYWMwNjExMWFkZTljNWI1P3M9NDgwJnI9cGcmZD1odHRwcyUzQSUyRiUyRmNkbi5hdXRoMC5jb20lMkZhdmF0YXJzJTJGZG8ucG5nIiwidXBkYXRlZF9hdCI6IjIwMjMtMDYtMTNUMDY6MTE6MjQuOTgxWiIsImVtYWlsIjoiZG9taW5pYy5tdXJyYXlAaW1tdXRhYmxlLmNvbSIsImVtYWlsX3ZlcmlmaWVkIjp0cnVlLCJpc3MiOiJodHRwczovL2F1dGguaW1tdXRhYmxlLmNvbS8iLCJhdWQiOiJaSkw3SnZldGNERkJORGxnUnM1b0pveHVBVVVsNnVRaiIsImlhdCI6MTY4Njg4OTA4MCwiZXhwIjoxNjg2OTI1MDgwLCJzdWIiOiJlbWFpbHw2NDJiN2Q0YWI5N2IzYWMyNDg3NzBlNTEifQ.X1G-59ay7yKCHUdq01Kntx4JLgNv5KjQSeWjDULuG1CNDS8eUrlWvZS1bok77AQd683GflTjoM50T6KuDanW0pcsDGcZDnGQ-yj8-Eb381zrWybkYok3AkJbzRJ2M9AQsFdv6wjjQg78xAFgutRev4XtCsEApD7wIfCFi8EkqLwJMDbkjzNjyRjVHP8g_h6XfkI23iRhjz0qSySV3ogmLxxkPse9uOILfXvB5SczJWe6RPRZKoMk-uoqAbDPNgSAa7G_PoPAQd404vYrkltVV3tT8bxiyasO_Uhk9Djre0_dnzJ3Cqfmmlvk7kS9g22ELFvIJBJ0zgcCzqa6fIFDZQ\",\"token_type\":\"Bearer\",\"expires_in\":86400}";

        private AuthManager manager;
        private MockHttpMessageHandler httpMock;
        private MockCredentialsManager credentialsManager;
        
        [SetUp] 
        public void Init()
        { 
            httpMock = new MockHttpMessageHandler();
            credentialsManager = new MockCredentialsManager();
            manager = new AuthManager(httpMock.ToHttpClient(), credentialsManager);
        }

        private async void PrepareForConfirmCode() {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            AddDeviceCodeResponse();
            var code = await manager.Login();
        }

        private void AddDeviceCodeResponse() {
            var deviceCodeResponse = new HttpResponseMessage(HttpStatusCode.OK);
            deviceCodeResponse.Content = new StringContent("{\"device_code\": \"deviceCode\",\"user_code\": \"userCode\",\"verification_uri\": \"verificationUri\",\"expires_in\": 3600000,\"interval\": 1,\"verification_uri_complete\": \"verificationUriComplete\"}");
            httpMock.responses.Add(deviceCodeResponse);
        }

        [Test]
        public async Task Login_Success_FreshLogin()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            AddDeviceCodeResponse();
            var code = await manager.Login();
            var request = httpMock.requests[0];

            Assert.AreEqual(request.RequestUri, "https://auth.immutable.com/oauth/device/code");
            Assert.AreEqual(request.Method, HttpMethod.Post);
            Assert.AreEqual(code, "userCode");
        }

        [Test]
        public async Task Login_Success_ExistingCredentials()
        {
            credentialsManager.hasValidCredentials = true;
            credentialsManager.token = new TokenResponse();
            var code = await manager.Login();
            Assert.Null(code);
            Assert.NotNull(manager.GetUser());
        }

        [Test]
        public async Task Login_Success_UsingRefreshToken()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = new TokenResponse();
            credentialsManager.token.refresh_token = "thisIsTheRefreshToken";
            var refreshTokenResponse = new HttpResponseMessage(HttpStatusCode.OK);
            refreshTokenResponse.Content = new StringContent(VALID_TOKEN_RESPONSE);
            httpMock.responses.Add(refreshTokenResponse);

            Assert.Null(manager.GetUser());

            var code = await manager.Login();
            Assert.Null(code);
            Assert.NotNull(manager.GetUser());

            var request = httpMock.requests[0];
            Assert.AreEqual(request.RequestUri, "https://auth.immutable.com/oauth/token");
            Assert.AreEqual(request.Method, HttpMethod.Post);
            string stringContent = await request.Content.ReadAsStringAsync();
            Assert.True(stringContent.Contains("refresh_token=thisIsTheRefreshToken"));
        }
        
        [Test]
        public async Task Login_Failed_GetDeviceCode()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            var deviceCodeResponse = new HttpResponseMessage(HttpStatusCode.NotAcceptable);
            httpMock.responses.Add(deviceCodeResponse);
            Exception? e = null;
            try {
                var result = await manager.Login();
            } catch (Exception exception) {
                e = exception;
            }
            Assert.NotNull(e);
            var request = httpMock.requests[0];
            Assert.AreEqual(request.RequestUri, "https://auth.immutable.com/oauth/device/code");
            Assert.AreEqual(request.Method, HttpMethod.Post);
        }

        [Test]
        public async Task Login_Success_RefreshFailed_DeviceCodeFallback()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = new TokenResponse();
            credentialsManager.token.refresh_token = "thisIsTheRefreshToken";
            var refreshTokenResponse = new HttpResponseMessage(HttpStatusCode.OK);
            refreshTokenResponse.Content = new StringContent("{}");
            httpMock.responses.Add(refreshTokenResponse);

            AddDeviceCodeResponse();

            var code = await manager.Login();
            Assert.AreEqual(code, "userCode");

            var request = httpMock.requests[0];
            Assert.AreEqual(request.RequestUri, "https://auth.immutable.com/oauth/token");

            var deviceCodeRequest = httpMock.requests[1];
            Assert.AreEqual(deviceCodeRequest.RequestUri, "https://auth.immutable.com/oauth/device/code");
        }

        [Test]
        public async Task ConfirmCode_Success()
        {
            PrepareForConfirmCode();
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK);
            tokenResponse.Content = new StringContent(VALID_TOKEN_RESPONSE);
            httpMock.responses.Add(tokenResponse);

            Assert.Null(manager.GetUser());

            var user = await manager.ConfirmCode();

            Assert.NotNull(user);
            Assert.AreEqual(user, manager.GetUser());

            Assert.AreEqual(2, httpMock.requests.Count);
            var request = httpMock.requests[1];
            Assert.AreEqual(request.RequestUri, "https://auth.immutable.com/oauth/token");
            Assert.AreEqual(request.Method, HttpMethod.Post);
            var stringContent = await request.Content.ReadAsStringAsync();
            Assert.True(stringContent.Contains("device_code=deviceCode"));
        }

        [Test]
        public async Task ConfirmCode_Failed_PendingAndExpired()
        {
            PrepareForConfirmCode();
            var slowDownResponse = new HttpResponseMessage(HttpStatusCode.OK);
            slowDownResponse.Content = new StringContent("{\"error\":\"authorization_pending\",\"error_description\":\"description\"}");
            httpMock.responses.Add(slowDownResponse);

            var expiredResponse = new HttpResponseMessage(HttpStatusCode.OK);
            expiredResponse.Content = new StringContent("{\"error\":\"expired_token\",\"error_description\":\"description\"}");
            httpMock.responses.Add(expiredResponse);

            Assert.Null(manager.GetUser());

            Exception? e = null;
            try {
                var result = await manager.ConfirmCode();
            } catch (Exception exception) {
                e = exception;
                Debug.Log("Exception: " + e);
            }
            Assert.NotNull(e);
            Assert.AreEqual(e.GetType(), typeof(InvalidOperationException));
            Assert.AreEqual(3, httpMock.requests.Count);
        }

        [Test]
        public async Task ConfirmCode_Failed_SlowDownAndAccessDenied()
        {
            PrepareForConfirmCode();
            var slowDownResponse = new HttpResponseMessage(HttpStatusCode.OK);
            slowDownResponse.Content = new StringContent("{\"error\":\"slow_down\",\"error_description\":\"description\"}");
            httpMock.responses.Add(slowDownResponse);

            var expiredResponse = new HttpResponseMessage(HttpStatusCode.OK);
            expiredResponse.Content = new StringContent("{\"error\":\"access_denied\",\"error_description\":\"description\"}");
            httpMock.responses.Add(expiredResponse);

            Assert.Null(manager.GetUser());

            Exception? e = null;
            try {
                var result = await manager.ConfirmCode();
            } catch (Exception exception) {
                e = exception;
                Debug.Log("Exception: " + e);
            }
            Assert.NotNull(e);
            Assert.AreEqual(e.GetType(), typeof(UnauthorizedAccessException));
            Assert.AreEqual(3, httpMock.requests.Count);
        }

        [Test]
        public async Task ConfirmCode_Failed_UnexpectedErrorCode()
        {
            PrepareForConfirmCode();
            var unexpectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            unexpectedResponse.Content = new StringContent("{\"error\":\"whats_this\",\"error_description\":\"description\"}");
            httpMock.responses.Add(unexpectedResponse);

            Assert.Null(manager.GetUser());

            Exception? e = null;
            try {
                var result = await manager.ConfirmCode();
            } catch (Exception exception) {
                e = exception;
                Debug.Log("Exception: " + e);
            }
            Assert.NotNull(e);
            Assert.AreEqual(2, httpMock.requests.Count);
        }

        [Test]
        public async Task ConfirmCode_Failed_UnexpectedResponse()
        {
            PrepareForConfirmCode();
            var unexpectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            unexpectedResponse.Content = new StringContent("{}");
            httpMock.responses.Add(unexpectedResponse);

            Assert.Null(manager.GetUser());

            Exception? e = null;
            try {
                var result = await manager.ConfirmCode();
            } catch (Exception exception) {
                e = exception;
                Debug.Log("Exception: " + e);
            }
            Assert.NotNull(e);
            Assert.AreEqual(2, httpMock.requests.Count);
        }
    }

    internal class MockCredentialsManager : ICredentialsManager {
        public bool hasValidCredentials = false;
        public TokenResponse? token = null;

        public void SaveCredentials(TokenResponse tokenResponse) {
            token = tokenResponse;
        }

        public TokenResponse? GetCredentials() {
            return token;
        }

        public bool HasValidCredentials() {
            return hasValidCredentials;
        }

        public void ClearCredentials() {

        }
    }
}