using System.IO;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class IdentityTests
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
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [Test]
        public void NewDirectory_GeneratesNonEmptyId_AndWritesFile()
        {
            var id = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(id);
            Assert.IsNotEmpty(id);

            var filePath = Path.Combine(_testDir, "imtbl_audience", "identity");
            Assert.IsTrue(File.Exists(filePath), "identity file should exist on disk");
        }

        [Test]
        public void ExistingFile_ReturnsPreviousId_WithoutGeneratingNew()
        {
            // Simulate a returning player by pre-writing an identity file (as a previous launch would have done).
            var expectedId = "pre-existing-id-from-last-launch";
            var dir = Path.Combine(_testDir, "imtbl_audience");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "identity"), expectedId);

            var result = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(expectedId, result);
        }

        [Test]
        public void SecondCall_ReturnsSameId()
        {
            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void Reset_NextCallReturnsDifferentId()
        {
            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            Identity.Reset(_testDir);
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(id2);
            Assert.AreNotEqual(id1, id2);
        }

        [Test]
        public void ConsentNone_ReturnsNull_AndNoFileWritten()
        {
            var id = Identity.GetOrCreate(_testDir, ConsentLevel.None);

            Assert.IsNull(id);

            var filePath = Path.Combine(_testDir, "imtbl_audience", "identity");
            Assert.IsFalse(File.Exists(filePath), "identity file must not be written when consent is None");
        }
    }
}
