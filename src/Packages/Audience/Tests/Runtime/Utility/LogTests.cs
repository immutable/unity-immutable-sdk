using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class LogTests
    {
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

            Log.Debug("silent");

            Assert.AreEqual(0, _captured.Count);
        }

        [Test]
        public void Debug_WhenEnabled_EmitsWithPrefix()
        {
            Log.Enabled = true;

            Log.Debug("hello");

            Assert.AreEqual(1, _captured.Count);
            StringAssert.StartsWith("[ImmutableAudience]", _captured[0]);
            StringAssert.Contains("hello", _captured[0]);
        }

        [Test]
        public void Warn_AlwaysEmits_EvenWhenDisabled()
        {
            Log.Enabled = false;

            Log.Warn("something off");

            Assert.AreEqual(1, _captured.Count);
            StringAssert.Contains("WARN", _captured[0]);
            StringAssert.Contains("something off", _captured[0]);
        }
    }
}
