using System.IO;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConsentStoreTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [Test]
        public void Load_NoFile_ReturnsNull()
        {
            Assert.IsNull(ConsentStore.Load(_testDir));
        }

        [Test]
        public void SaveThenLoad_RoundtripsValue([Values] ConsentLevel level)
        {
            ConsentStore.Save(_testDir, level);
            Assert.AreEqual(level, ConsentStore.Load(_testDir));
        }

        [Test]
        public void Save_OverwritesPreviousValue()
        {
            ConsentStore.Save(_testDir, ConsentLevel.Anonymous);
            ConsentStore.Save(_testDir, ConsentLevel.Full);

            Assert.AreEqual(ConsentLevel.Full, ConsentStore.Load(_testDir));
        }

        [Test]
        public void Load_MalformedFile_ReturnsNull()
        {
            // A garbage value that isn't a valid enum int.
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(AudiencePaths.ConsentFile(_testDir), "not-an-int");

            Assert.IsNull(ConsentStore.Load(_testDir));
        }

        [Test]
        public void Load_OutOfRangeIntInFile_ReturnsNull()
        {
            // 999 is parseable as int but not a defined ConsentLevel.
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(AudiencePaths.ConsentFile(_testDir), "999");

            Assert.IsNull(ConsentStore.Load(_testDir));
        }

    }
}
