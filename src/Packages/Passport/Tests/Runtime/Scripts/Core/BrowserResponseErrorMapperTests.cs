using NUnit.Framework;
using Immutable.Passport.Model;
using UnityEngine;
using UnityEngine.TestTools;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class BrowserResponseErrorMapperTests
    {
        private const string ERROR_MESSAGE = "something went wrong";

        [Test]
        public void MapToException_ErrorWithValidType_ReturnsTypedException()
        {
            var response = new BrowserResponse
            {
                error = ERROR_MESSAGE,
                errorType = "WALLET_CONNECTION_ERROR"
            };

            var ex = BrowserResponseErrorMapper.MapToException(response);

            Assert.AreEqual(ERROR_MESSAGE, ex.Message);
            Assert.AreEqual(PassportErrorType.WALLET_CONNECTION_ERROR, ex.Type);
        }

        [Test]
        public void MapToException_ErrorWithInvalidType_FallsBackToErrorMessage()
        {
            // An unrecognised errorType string should not throw — falls back gracefully
            var response = new BrowserResponse
            {
                error = ERROR_MESSAGE,
                errorType = "NOT_A_REAL_ERROR_TYPE"
            };

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Parse passport type error"));

            var ex = BrowserResponseErrorMapper.MapToException(response);

            Assert.AreEqual(ERROR_MESSAGE, ex.Message);
            Assert.IsNull(ex.Type);
        }

        [Test]
        public void MapToException_ErrorWithoutType_ReturnsExceptionWithMessage()
        {
            var response = new BrowserResponse
            {
                error = ERROR_MESSAGE
            };

            var ex = BrowserResponseErrorMapper.MapToException(response);

            Assert.AreEqual(ERROR_MESSAGE, ex.Message);
            Assert.IsNull(ex.Type);
        }

        [Test]
        public void MapToException_NoErrorOrType_ReturnsUnknownError()
        {
            var response = new BrowserResponse();

            var ex = BrowserResponseErrorMapper.MapToException(response);

            Assert.AreEqual("Unknown error", ex.Message);
            Assert.IsNull(ex.Type);
        }

        [Test]
        public void MapToException_ErrorTypeSetButNoError_ReturnsUnknownError()
        {
            // errorType alone is not enough — the typed path requires both fields
            var response = new BrowserResponse { errorType = "WALLET_CONNECTION_ERROR" };

            var ex = BrowserResponseErrorMapper.MapToException(response);

            Assert.AreEqual("Unknown error", ex.Message);
            Assert.IsNull(ex.Type);
        }
    }
}
