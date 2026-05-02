using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class LogTests
    {
        // Inputs to Log.Debug / Log.Warn used across the fixture.
        private const string SilentDebugInput = "silent";
        private const string EnabledDebugInput = "hello";
        private const string WarnInput = "something off";

        // Substring marker that Log.Warn injects between the prefix and the user message.
        private const string WarnMarker = "WARN";

        private List<string> _captured;

        [SetUp]
        public void SetUp()
        {
            _captured = new List<string>();
            Log.Writer = line => _captured.Add(line);
            Log.Enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            Log.Writer = null;
            Log.Enabled = false;
        }

        [Test]
        public void Debug_WhenDisabled_EmitsNothing()
        {
            Log.Enabled = false;

            Log.Debug(SilentDebugInput);

            Assert.AreEqual(0, _captured.Count);
        }

        [Test]
        public void Debug_WhenEnabled_EmitsWithPrefix()
        {
            Log.Enabled = true;

            Log.Debug(EnabledDebugInput);

            Assert.AreEqual(1, _captured.Count);
            StringAssert.StartsWith(Log.Prefix, _captured[0]);
            StringAssert.Contains(EnabledDebugInput, _captured[0]);
        }

        [Test]
        public void Warn_AlwaysEmits_EvenWhenDisabled()
        {
            Log.Enabled = false;

            Log.Warn(WarnInput);

            Assert.AreEqual(1, _captured.Count);
            StringAssert.Contains(WarnMarker, _captured[0]);
            StringAssert.Contains(WarnInput, _captured[0]);
        }
    }
}
