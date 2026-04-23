using System.IO;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConstantsTests
    {
        // -----------------------------------------------------------------
        // Environment resolution
        // -----------------------------------------------------------------

        [Test]
        public void ResolveEnvironment_AutoTestKey_ReturnsSandbox()
        {
            Assert.AreEqual(AudienceEnvironment.Sandbox,
                Constants.ResolveEnvironment("pk_imapik-test-abc", AudienceEnvironment.Auto));
        }

        [Test]
        public void ResolveEnvironment_AutoNonTestKey_ReturnsProduction()
        {
            Assert.AreEqual(AudienceEnvironment.Production,
                Constants.ResolveEnvironment("pk_imapik-prodkey", AudienceEnvironment.Auto));
        }

        [Test]
        public void ResolveEnvironment_AutoNullKey_ReturnsProduction()
        {
            // Null key is the misconfiguration path; default to Production
            // so the request hits the env most likely to surface a 401 fast
            // rather than silently sending to sandbox.
            Assert.AreEqual(AudienceEnvironment.Production,
                Constants.ResolveEnvironment(null, AudienceEnvironment.Auto));
        }

        [Test]
        public void ResolveEnvironment_ExplicitDev_PassesThrough()
        {
            // Explicit env overrides the key prefix. Studios can stage a
            // test key against any backend without renaming the key.
            Assert.AreEqual(AudienceEnvironment.Dev,
                Constants.ResolveEnvironment("pk_imapik-test-abc", AudienceEnvironment.Dev));
        }

        [Test]
        public void ResolveEnvironment_ExplicitSandbox_PassesThrough()
        {
            Assert.AreEqual(AudienceEnvironment.Sandbox,
                Constants.ResolveEnvironment("pk_imapik-prodkey", AudienceEnvironment.Sandbox));
        }

        [Test]
        public void ResolveEnvironment_ExplicitProduction_PassesThrough()
        {
            // Explicit Production overrides a test-key prefix — useful for
            // QA running tests against prod infra.
            Assert.AreEqual(AudienceEnvironment.Production,
                Constants.ResolveEnvironment("pk_imapik-test-abc", AudienceEnvironment.Production));
        }

        [Test]
        public void BaseUrl_Dev_ReturnsDevHost()
        {
            Assert.AreEqual(Constants.DevBaseUrl,
                Constants.BaseUrl("pk_imapik-test-abc", AudienceEnvironment.Dev));
        }

        [Test]
        public void BaseUrl_Sandbox_ReturnsSandboxHost()
        {
            Assert.AreEqual(Constants.SandboxBaseUrl,
                Constants.BaseUrl("pk_imapik-test-abc", AudienceEnvironment.Sandbox));
        }

        [Test]
        public void BaseUrl_Production_ReturnsProductionHost()
        {
            Assert.AreEqual(Constants.ProductionBaseUrl,
                Constants.BaseUrl("pk_imapik-test-abc", AudienceEnvironment.Production));
        }

        [Test]
        public void BaseUrl_AutoTestKey_ReturnsSandboxHost()
        {
            Assert.AreEqual(Constants.SandboxBaseUrl,
                Constants.BaseUrl("pk_imapik-test-abc", AudienceEnvironment.Auto));
        }

        [Test]
        public void BaseUrl_AutoProdKey_ReturnsProductionHost()
        {
            Assert.AreEqual(Constants.ProductionBaseUrl,
                Constants.BaseUrl("pk_imapik-prodkey", AudienceEnvironment.Auto));
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
            // dotnet build outputs don't leak into Unity's scan path — the
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

            Assert.Fail($"package.json not found by walking up from {TestContext.CurrentContext.TestDirectory}");
            return null;
        }
    }
}
