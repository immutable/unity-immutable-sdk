using NUnit.Framework;
using Immutable.Passport.Model;
using UnityEngine;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class BrowserMessageCodecTests
    {
        private const string FUNCTION_NAME = "someFunction";
        private const string REQUEST_ID = "test-request-id";

        [Test]
        public void BuildJsCall_ProducesCallFunctionInvocation()
        {
            var request = new BrowserRequest(FUNCTION_NAME, REQUEST_ID, null);

            var js = BrowserMessageCodec.BuildJsCall(request);

            Assert.IsTrue(js.StartsWith("callFunction(\""));
            Assert.IsTrue(js.EndsWith("\")"));
        }

        [Test]
        public void BuildJsCall_ContainsFunctionNameAndRequestId()
        {
            var request = new BrowserRequest(FUNCTION_NAME, REQUEST_ID, null);

            var js = BrowserMessageCodec.BuildJsCall(request);

            Assert.IsTrue(js.Contains(FUNCTION_NAME));
            Assert.IsTrue(js.Contains(REQUEST_ID));
        }

        [Test]
        public void BuildJsCall_EscapesQuotesAndBackslashes()
        {
            // data containing quotes and backslashes that must survive JSON-in-JS embedding
            var request = new BrowserRequest(FUNCTION_NAME, REQUEST_ID, "{\"key\":\"value\"}");

            var js = BrowserMessageCodec.BuildJsCall(request);

            // The raw JS string must not contain unescaped quotes that would break callFunction("...")
            // Verify it round-trips cleanly through the mock browser client extraction logic
            var json = ExtractJson(js);
            var roundTripped = Immutable.Passport.Helpers.JsonExtensions.OptDeserializeObject<BrowserRequest>(json);
            Assert.AreEqual(FUNCTION_NAME, roundTripped.fxName);
            Assert.AreEqual("{\"key\":\"value\"}", roundTripped.data);
        }

        [Test]
        public void ParseAndValidateResponse_ValidMessage_ReturnsResponse()
        {
            var response = new BrowserResponse
            {
                responseFor = FUNCTION_NAME,
                requestId = REQUEST_ID,
                success = true
            };
            var json = JsonUtility.ToJson(response);

            var result = BrowserMessageCodec.ParseAndValidateResponse(json);

            Assert.AreEqual(FUNCTION_NAME, result.responseFor);
            Assert.AreEqual(REQUEST_ID, result.requestId);
        }

        [Test]
        public void ParseAndValidateResponse_MissingResponseFor_Throws()
        {
            var response = new BrowserResponse { requestId = REQUEST_ID };
            var json = JsonUtility.ToJson(response);

            var ex = Assert.Throws<PassportException>(
                () => BrowserMessageCodec.ParseAndValidateResponse(json)
            );

            Assert.IsTrue(ex.Message.Contains("Response from game bridge is incorrect"));
        }

        [Test]
        public void ParseAndValidateResponse_MissingRequestId_Throws()
        {
            var response = new BrowserResponse { responseFor = FUNCTION_NAME };
            var json = JsonUtility.ToJson(response);

            var ex = Assert.Throws<PassportException>(
                () => BrowserMessageCodec.ParseAndValidateResponse(json)
            );

            Assert.IsTrue(ex.Message.Contains("Response from game bridge is incorrect"));
        }

        [Test]
        public void ParseAndValidateResponse_InvalidJson_Throws()
        {
            var ex = Assert.Throws<PassportException>(
                () => BrowserMessageCodec.ParseAndValidateResponse("not valid json")
            );

            Assert.IsTrue(ex.Message.Contains("Response from game bridge is incorrect"));
        }

        // Mirrors the extraction logic in MockBrowserClient to round-trip test BuildJsCall output
        private static string ExtractJson(string js)
        {
            const string prefix = "callFunction(\"";
            const string suffix = "\")";
            int start = js.IndexOf(prefix) + prefix.Length;
            int end = js.LastIndexOf(suffix);
            return js.Substring(start, end - start).Replace("\\\\", "\\").Replace("\\\"", "\"");
        }
    }
}
