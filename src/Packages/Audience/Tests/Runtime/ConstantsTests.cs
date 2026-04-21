using System.IO;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConstantsTests
    {
        [Test]
        public void LibraryVersion_MatchesPackageJson()
        {
            // Fails the build if Constants.LibraryVersion and package.json
            // "version" drift. context.libraryVersion on every outgoing event
            // depends on them agreeing.
            var packageJson = ReadPackageJson();
            var parsed = JsonReader.DeserializeObject(packageJson);

            Assert.IsTrue(parsed.TryGetValue("version", out var versionObj),
                "package.json is missing a \"version\" field");
            Assert.AreEqual(Constants.LibraryVersion, versionObj,
                "Constants.LibraryVersion must match package.json version");
        }

        private static string ReadPackageJson()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            // Tests/bin/Debug/net8.0/ → Tests/ → Audience/package.json
            var packagePath = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "package.json"));
            Assert.IsTrue(File.Exists(packagePath), $"package.json not found at {packagePath}");
            return File.ReadAllText(packagePath);
        }
    }
}
