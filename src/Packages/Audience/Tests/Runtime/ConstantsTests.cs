using System.IO;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConstantsTests
    {
        // -----------------------------------------------------------------
        // BaseUrl resolution
        // -----------------------------------------------------------------

        [Test]
        public void BaseUrl_TestKey_ResolvesToSandbox()
        {
            Assert.AreEqual(Constants.SandboxBaseUrl,
                Constants.BaseUrl("pk_imapik-test-abc"));
        }

        [Test]
        public void BaseUrl_NonTestKey_ResolvesToProduction()
        {
            Assert.AreEqual(Constants.ProductionBaseUrl,
                Constants.BaseUrl("pk_imapik-prod-abc"));
        }

        [Test]
        public void BaseUrl_NullKey_ResolvesToProduction()
        {
            Assert.AreEqual(Constants.ProductionBaseUrl,
                Constants.BaseUrl(null));
        }

        [Test]
        public void BaseUrl_Override_WinsOverKeyPrefix()
        {
            // Override wins even for a test-prefixed key that would
            // otherwise derive to Sandbox.
            const string custom = "https://api.dev.immutable.com";
            Assert.AreEqual(custom,
                Constants.BaseUrl("pk_imapik-test-abc", custom));
        }

        [Test]
        public void BaseUrl_EmptyOverride_FallsBackToKeyDerivation()
        {
            // Empty-string override is treated as "no override" so the
            // key-prefix fallback still kicks in.
            Assert.AreEqual(Constants.SandboxBaseUrl,
                Constants.BaseUrl("pk_imapik-test-abc", ""));
        }

        // -----------------------------------------------------------------
        // Library version
        // -----------------------------------------------------------------

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
            // Walk up from the test binary location looking for the Audience
            // package directory. Originally this hard-coded four "../" hops
            // which only worked when bin/ sat inside Tests/. Directory.Build
            // .props redirects bin/ to the repo-root /artifacts/ folder so
            // dotnet build outputs don't leak into Unity's scan path; the
            // relative walk no longer resolves to the package. Searching
            // upward is robust against either layout.
            var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "src", "Packages", "Audience", "package.json");
                if (File.Exists(candidate)) return File.ReadAllText(candidate);

                // Also try the direct-inside case (package root itself is
                // the ancestor), which handles consuming-project layouts
                // that embed the package without the src/Packages prefix.
                var direct = Path.Combine(current.FullName, "package.json");
                if (File.Exists(direct) && current.Name == "Audience") return File.ReadAllText(direct);

                current = current.Parent;
            }

            throw new FileNotFoundException(
                $"package.json not found by walking up from {TestContext.CurrentContext.TestDirectory}");
        }
    }
}
