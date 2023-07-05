using System;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Immutable.Passport.Storage;
using Immutable.Passport.Utility.Tests;
using Immutable.Passport.Model;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Immutable.Passport.Auth
{
    [TestFixture]
    public class AuthManagerTests
    {
        private const string KEY_DEVICE_CODE = "device_code";
        private const string KEY_REFRESH_TOKEN = "refresh_token";
        private const string USER_CODE = "userCode";
        private const string DEVICE_CODE_URL = "deviceCodeAuthUrl";
        private const string DEVICE_CODE = "deviceCode";
        internal const string ACCESS_TOKEN = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjNhYVl5dGR3d2UwMzJzMXIzVElyOSJ9." +
            "eyJlbWFpbCI6ImRvbWluaWMubXVycmF5QGltbXV0YWJsZS5jb20iLCJvcmciOiJhNTdiMWYzZC1mYTU3LTRiNzgtODZkYy05ZDEyZDM1YjlhNj" +
            "giLCJldGhlcl9rZXkiOiIweGRlMDYzYmViNmNmNDhlNGMxOTcxYzc3N2M0OGY0NTU3MTA1MjU5ZWMiLCJzdGFya19rZXkiOiIweGM1NTYxZGU3" +
            "Nzg4NTUxOTY0ZWQxMjI0Yzc2ZjQ5ZDk5ZmVjODkyOGQ1OWVkNTcwZTExZGIwYzk3ZGYwMTFmIiwidXNlcl9hZG1pbl9rZXkiOiIweGY4MjY4OT" +
            "I0MWU3NTM1YjYyNTgyMmI4M2I1OWMxZDM3ZWUwOTBiZGMiLCJpc3MiOiJodHRwczovL2F1dGguaW1tdXRhYmxlLmNvbS8iLCJzdWIiOiJlbWFp" +
            "bHw2NDJiN2Q0YWI5N2IzYWMyNDg3NzBlNTEiLCJhdWQiOlsicGxhdGZvcm1fYXBpIiwiaHR0cHM6Ly9wcm9kLmltbXV0YWJsZS5hdXRoMGFwcC" +
            "5jb20vdXNlcmluZm8iXSwiaWF0IjoxNjg2ODg5MDgwLCJleHAiOjE2ODY5NzU0ODAsImF6cCI6IlpKTDdKdmV0Y0RGQk5EbGdSczVvSm94dUFV" +
            "VWw2dVFqIiwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCB0cmFuc2FjdCBvZmZsaW5lX2FjY2VzcyJ9.aqFem4Pp0k91YWdYuxBo1wfCr" +
            "YAHy1Y1zIqof2GMQXd_mwhGBkHotUlgFAsOIQmvO6lQG5m3cHLR9zlEsFJ_2AJuJg3NTNnF0dEx12H5hx24_aN4qDga8Q9KNDdGc3x4LAxv48P" +
            "H-P6OcdMnpWekCUPAI3zZ9qWC1YWU9HaouQuBJNbUV8ujyooFGOP4YzejQf2Uyxz9wmzDiMS-e70BSmY8IPRR2A3QjOEo7oI3enqM_jylfbDo8" +
            "BsDRouDbwbZrMeX-rcJ_HBY5iZEdagVpcvOmfZCaad55MI_WJcXHrDMzmLqck1fd15Oklo7fajQtiG0ByINxTmm9_0YnEy02w";
        internal const string REFRESH_TOKEN = "v1.NOQCr0kkmy0Cky_Doia8VgJdclSKOOOrkicjZ4NFeabS5J7xNct-oRwO2H65ua0mPOhzWfIf8lhxlM1sIUBqLoc";
        internal const string ID_TOKEN = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjNhYVl5dGR3d2UwMzJzMXIzVElyOSJ9.eyJk" +
            "ZXZlbG9wZXJfaHViIjp7ImFjdGl2YXRlZCI6dHJ1ZSwib3JnYW5pemF0aW9uIjp7ImlkIjoiYTU3YjFmM2QtZmE1Ny00Yjc4LTg2ZGMtOWQxMm" +
            "QzNWI5YTY4In0sInJlc3BvbnNlSWQiOiI4MzhiangzOG1maG15cWs3bjgzOGJqeGZrMjdhZXQxMCIsInJlc3BvbnNlcyI6eyJmdW5kaW5nIjoi" +
            "UHJlZmVyIG5vdCB0byBzYXkiLCJoYXZlTWludGVkTmZ0c0JlZm9yZSI6Ik5vIiwicGVvcGxlIjoiSnVzdCBtZSIsInByb2plY3RTdGFnZSI6Ik" +
            "NvbmNlcHQiLCJwcm9qZWN0VHlwZSI6Ik90aGVyIiwicm9sZSI6Ik90aGVyIn0sInJvbGVzIjpbXSwidXNlck1ldGFkYXRhIjp7InJlY2VpdmVN" +
            "YXJrZXRpbmdFbWFpbHNDb25zZW50Ijp7ImNvbnNlbnRlZCI6ZmFsc2UsImRhdGUiOiIyMDIzLTA0LTIwVDAxOjA2OjI2LjgwOFoiLCJ2ZXJzaW" +
            "9uIjoxfX19LCJwYXNzcG9ydCI6eyJldGhlcl9rZXkiOiIweGRlMDYzYmViNmNmNDhlNGMxOTcxYzc3N2M0OGY0NTU3MTA1MjU5ZWMiLCJzdGFy" +
            "a19rZXkiOiIweGM1NTYxZGU3Nzg4NTUxOTY0ZWQxMjI0Yzc2ZjQ5ZDk5ZmVjODkyOGQ1OWVkNTcwZTExZGIwYzk3ZGYwMTFmIiwidXNlcl9hZG" +
            "1pbl9rZXkiOiIweGY4MjY4OTI0MWU3NTM1YjYyNTgyMmI4M2I1OWMxZDM3ZWUwOTBiZGMifSwibmlja25hbWUiOiJkb21pbmljLm11cnJheSIs" +
            "Im5hbWUiOiJkb21pbmljLm11cnJheUBpbW11dGFibGUuY29tIiwicGljdHVyZSI6Imh0dHBzOi8vcy5ncmF2YXRhci5jb20vYXZhdGFyL2NkOT" +
            "kwYWNiYzYwNWQ4ZDdiYWMwNjExMWFkZTljNWI1P3M9NDgwJnI9cGcmZD1odHRwcyUzQSUyRiUyRmNkbi5hdXRoMC5jb20lMkZhdmF0YXJzJTJG" +
            "ZG8ucG5nIiwidXBkYXRlZF9hdCI6IjIwMjMtMDYtMTNUMDY6MTE6MjQuOTgxWiIsImVtYWlsIjoiZG9taW5pYy5tdXJyYXlAaW1tdXRhYmxlLm" +
            "NvbSIsImVtYWlsX3ZlcmlmaWVkIjp0cnVlLCJpc3MiOiJodHRwczovL2F1dGguaW1tdXRhYmxlLmNvbS8iLCJhdWQiOiJaSkw3SnZldGNERkJO" +
            "RGxnUnM1b0pveHVBVVVsNnVRaiIsImlhdCI6MTY4Njg4OTA4MCwiZXhwIjoxNjg2OTI1MDgwLCJzdWIiOiJlbWFpbHw2NDJiN2Q0YWI5N2IzYW" +
            "MyNDg3NzBlNTEifQ.X1G-59ay7yKCHUdq01Kntx4JLgNv5KjQSeWjDULuG1CNDS8eUrlWvZS1bok77AQd683GflTjoM50T6KuDanW0pcsDGcZD" +
            "nGQ-yj8-Eb381zrWybkYok3AkJbzRJ2M9AQsFdv6wjjQg78xAFgutRev4XtCsEApD7wIfCFi8EkqLwJMDbkjzNjyRjVHP8g_h6XfkI23iRhjz0" +
            "qSySV3ogmLxxkPse9uOILfXvB5SczJWe6RPRZKoMk-uoqAbDPNgSAa7G_PoPAQd404vYrkltVV3tT8bxiyasO_Uhk9Djre0_dnzJ3Cqfmmlvk7" +
            "kS9g22ELFvIJBJ0zgcCzqa6fIFDZQ";
        internal const string TOKEN_TYPE = "Bearer";
        internal const int EXPIRES_IN = 86400;
        internal static string VALID_TOKEN_RESPONSE = @$"{{""access_token"":""{ACCESS_TOKEN}"",""{KEY_REFRESH_TOKEN}"":""{REFRESH_TOKEN}"",""id_token"":""{ID_TOKEN}"",""token_type"":""{TOKEN_TYPE}"",""expires_in"":{EXPIRES_IN}}}";
        private static readonly string TOKEN_ENDPOINT = $"{AuthManager.DOMAIN}{AuthManager.PATH_TOKEN}";
        private static readonly string AUTH_CODE_ENDPOINT = $"{AuthManager.DOMAIN}{AuthManager.PATH_AUTH_CODE}";

        private static readonly string EMAIL = "armin@immutable.com";

#pragma warning disable CS8618
        private AuthManager manager;
        private MockHttpMessageHandler httpMock;
        private MockCredentialsManager credentialsManager;
#pragma warning restore CS8618

        [SetUp]
        public void Init()
        {
            httpMock = new MockHttpMessageHandler();
            credentialsManager = new MockCredentialsManager();
            manager = new AuthManager(httpMock.ToHttpClient(), credentialsManager);
        }

        private async void PrepareForConfirmCode()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            AddDeviceCodeResponse();
            _ = await manager.Login();
        }

        private void InitialiseWithRefreshToken()
        {
            credentialsManager.token = new TokenResponse
            {
                refresh_token = REFRESH_TOKEN
            };
            httpMock.Responses.Add(CreateMockResponse(VALID_TOKEN_RESPONSE));
        }

        private void AddDeviceCodeResponse()
        {
            httpMock.Responses.Add(CreateMockResponse(@$"{{""{KEY_DEVICE_CODE}"": ""{DEVICE_CODE}"",""user_code"": ""{USER_CODE}""," +
                @$"""verification_uri"": ""verificationUri"",""expires_in"": 3600000,""interval"": 1,""verification_uri_complete"": ""{DEVICE_CODE_URL}""}}"));
        }

        private async void VerifyRefreshRequest(HttpRequestMessage request)
        {
            Assert.AreEqual(request.RequestUri, TOKEN_ENDPOINT);
            Assert.AreEqual(request.Method, HttpMethod.Post);
            string stringContent = await request.Content.ReadAsStringAsync();
            Assert.True(stringContent.Contains($"{KEY_REFRESH_TOKEN}={REFRESH_TOKEN}"));
        }

        [Test]
        public async Task Login_Success_FreshLogin()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            AddDeviceCodeResponse();
            var response = await manager.Login();
            var request = httpMock.Requests[0];

            Assert.AreEqual(request.RequestUri, AUTH_CODE_ENDPOINT);
            Assert.AreEqual(request.Method, HttpMethod.Post);
            Assert.AreEqual(response?.code, USER_CODE);
            Assert.AreEqual(response?.url, DEVICE_CODE_URL);
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
            InitialiseWithRefreshToken();

            Assert.Null(manager.GetUser());

            var code = await manager.Login();
            Assert.Null(code);
            Assert.NotNull(manager.GetUser());

            VerifyRefreshRequest(httpMock.Requests[0]);
        }

        [Test]
        public async Task Login_Failed_GetDeviceCode()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            var deviceCodeResponse = new HttpResponseMessage(HttpStatusCode.NotAcceptable);
            httpMock.Responses.Add(deviceCodeResponse);
            Exception? e = null;
            try
            {
                var result = await manager.Login();
            }
            catch (Exception exception)
            {
                e = exception;
            }
            Assert.NotNull(e);
            var request = httpMock.Requests[0];
            Assert.AreEqual(request.RequestUri, AUTH_CODE_ENDPOINT);
            Assert.AreEqual(request.Method, HttpMethod.Post);
        }

        [Test]
        public async Task Login_Success_RefreshFailed_DeviceCodeFallback()
        {
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = new TokenResponse
            {
                refresh_token = REFRESH_TOKEN
            };
            httpMock.Responses.Add(CreateMockResponse("{}"));

            AddDeviceCodeResponse();

            var response = await manager.Login();
            Assert.AreEqual(USER_CODE, response?.code);
            Assert.AreEqual(DEVICE_CODE_URL, response?.url);

            var request = httpMock.Requests[0];
            Assert.AreEqual(request.RequestUri, TOKEN_ENDPOINT);

            var deviceCodeRequest = httpMock.Requests[1];
            Assert.AreEqual(deviceCodeRequest.RequestUri, AUTH_CODE_ENDPOINT);
        }

        [Test]
        public async Task Login_Cancelled_OnRefresh()
        {
            httpMock.responseDelay = 2000;
            credentialsManager.hasValidCredentials = false;
            InitialiseWithRefreshToken();

            Assert.Null(manager.GetUser());

            await ExpectError(() =>
            {
                return manager.Login(GetTokenCancelledInASecond());
            }, typeof(OperationCanceledException));

            Assert.Null(manager.GetUser());

            VerifyRefreshRequest(httpMock.Requests[0]);
        }

        [Test]
        public async Task Login_Cancelled_OnDeviceCode()
        {
            httpMock.responseDelay = 2000;
            credentialsManager.hasValidCredentials = false;
            credentialsManager.token = null;
            AddDeviceCodeResponse();

            await ExpectError(() =>
            {
                return manager.Login(GetTokenCancelledInASecond());
            }, typeof(OperationCanceledException));

            Assert.Null(manager.GetUser());

            var request = httpMock.Requests[0];
            Assert.AreEqual(request.RequestUri, AUTH_CODE_ENDPOINT);
            Assert.AreEqual(request.Method, HttpMethod.Post);
        }

        private CancellationToken GetTokenCancelledInASecond()
        {
            CancellationTokenSource cancelTokenSource = new();
            new Task(() =>
            {
                Thread.Sleep(100);
                cancelTokenSource.Cancel();
            }).Start();
            return cancelTokenSource.Token;
        }

        [Test]
        public async Task ConfirmCode_Success()
        {
            PrepareForConfirmCode();
            httpMock.Responses.Add(CreateMockResponse(VALID_TOKEN_RESPONSE));

            Assert.Null(manager.GetUser());

            var user = await manager.ConfirmCode();

            Assert.NotNull(user);
            Assert.AreEqual(user, manager.GetUser());

            Assert.AreEqual(2, httpMock.Requests.Count);
            var request = httpMock.Requests[1];
            Assert.AreEqual(request.RequestUri, TOKEN_ENDPOINT);
            Assert.AreEqual(request.Method, HttpMethod.Post);
            var stringContent = await request.Content.ReadAsStringAsync();
            Assert.True(stringContent.Contains($"{KEY_DEVICE_CODE}={DEVICE_CODE}"));
        }

        [Test]
        public async Task ConfirmCode_Cancelled()
        {
            PrepareForConfirmCode();

            httpMock.Responses.Add(CreateMockResponse(CreateErrorJsonString("authorization_pending")));

            await ExpectError(() =>
            {
                Assert.Null(manager.GetUser());
                return manager.ConfirmCode(GetTokenCancelledInASecond());
            }, typeof(OperationCanceledException));

            Assert.Null(manager.GetUser());
        }

        [Test]
        public async Task ConfirmCode_Failed_PendingAndExpired()
        {
            PrepareForConfirmCode();

            httpMock.Responses.Add(CreateMockResponse(CreateErrorJsonString("authorization_pending")));
            httpMock.Responses.Add(CreateMockResponse(CreateErrorJsonString("expired_token")));

            await ExpectError(() =>
            {
                Assert.Null(manager.GetUser());
                return manager.ConfirmCode();
            }, typeof(InvalidOperationException));

            Assert.AreEqual(3, httpMock.Requests.Count);
        }

        [Test]
        public async Task ConfirmCode_Failed_SlowDownAndAccessDenied()
        {
            PrepareForConfirmCode();
            httpMock.Responses.Add(CreateMockResponse(CreateErrorJsonString("slow_down")));
            httpMock.Responses.Add(CreateMockResponse(CreateErrorJsonString("access_denied")));

            await ExpectError(() =>
            {
                Assert.Null(manager.GetUser());
                return manager.ConfirmCode();
            }, typeof(UnauthorizedAccessException));

            Assert.AreEqual(3, httpMock.Requests.Count);
        }

        // jscpd:ignore-start
        [Test]
        public async Task ConfirmCode_Failed_UnexpectedErrorCode()
        {
            PrepareForConfirmCode();
            httpMock.Responses.Add(CreateMockResponse(CreateErrorJsonString("whats_this")));

            await ExpectError(() =>
            {
                Assert.Null(manager.GetUser());
                return manager.ConfirmCode();
            }, typeof(PassportException));

            Assert.AreEqual(2, httpMock.Requests.Count);
        }

        [Test]
        public async Task ConfirmCode_Failed_UnexpectedResponse()
        {
            PrepareForConfirmCode();
            httpMock.Responses.Add(CreateMockResponse("{}"));

            await ExpectError(() =>
            {
                Assert.Null(manager.GetUser());
                return manager.ConfirmCode();
            }, typeof(PassportException));

            Assert.AreEqual(2, httpMock.Requests.Count);
        }
        // jscpd:ignore-end

        private async Task ExpectError(Func<UniTask> action, Type errorType)
        {
            Exception? e = null;
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                Debug.Log($"Exception caught {exception.GetType()}");
                e = exception;
            }
            Assert.NotNull(e);
            Assert.AreEqual(e?.GetType(), errorType);
        }

        [Test]
        public void HasCredentialsSavedTest()
        {
            Assert.False(manager.HasCredentialsSaved());

            credentialsManager.token = new TokenResponse()
            {
                access_token = AuthManagerTests.ACCESS_TOKEN,
                refresh_token = AuthManagerTests.REFRESH_TOKEN,
                id_token = AuthManagerTests.ID_TOKEN,
                token_type = AuthManagerTests.TOKEN_TYPE,
                expires_in = AuthManagerTests.EXPIRES_IN
            };
            Assert.True(manager.HasCredentialsSaved());
        }

        private string CreateErrorJsonString(string error)
        {
            return @$"{{""error"":""{error}"",""error_description"":""description""}}";
        }

        private HttpResponseMessage CreateMockResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
        }

        [Test]
        public void GetEmailTest()
        {
            Assert.Null(manager.GetEmail());
            credentialsManager.email = EMAIL;
            Assert.AreEqual(EMAIL, credentialsManager.GetEmail());
        }
    }

    internal class MockCredentialsManager : ICredentialsManager
    {
        public bool hasValidCredentials = false;
        public TokenResponse? token = null;
        public string? email = null;

        public void SaveCredentials(TokenResponse tokenResponse)
        {
            token = tokenResponse;
        }

        public TokenResponse? GetCredentials()
        {
            return token;
        }

        public bool HasValidCredentials()
        {
            return hasValidCredentials;
        }

        public string? GetEmail()
        {
            return email;
        }

        public void ClearCredentials()
        {

        }
    }
}