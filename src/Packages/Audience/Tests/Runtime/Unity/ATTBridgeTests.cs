#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Immutable.Audience.Unity.Mobile;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ATTBridgeTests
    {
        private Func<Task<int>>? _originalRequest;
        private Func<int>? _originalGetStatus;
        private Func<string?>? _originalGetIDFA;

        [SetUp]
        public void SetUp()
        {
            _originalRequest = ATTBridge.RequestImpl;
            _originalGetStatus = ATTBridge.GetStatusImpl;
            _originalGetIDFA = ATTBridge.GetIDFAImpl;
        }

        [TearDown]
        public void TearDown()
        {
            ATTBridge.RequestImpl = _originalRequest!;
            ATTBridge.GetStatusImpl = _originalGetStatus!;
            ATTBridge.GetIDFAImpl = _originalGetIDFA!;
        }

        [Test]
        public async Task RequestAsync_DelegatesToImpl()
        {
            ATTBridge.RequestImpl = () => Task.FromResult(3);

            var status = await ATTBridge.RequestAsync();

            Assert.AreEqual(3, status, "must surface the value the impl resolves to");
        }

        [Test]
        public void GetStatus_DelegatesToImpl()
        {
            ATTBridge.GetStatusImpl = () => 2;
            Assert.AreEqual(2, ATTBridge.GetStatus());
        }

        [Test]
        public void GetIDFA_DelegatesToImpl()
        {
            ATTBridge.GetIDFAImpl = () => "deadbeef-0000-0000-0000-000000000000";
            Assert.AreEqual("deadbeef-0000-0000-0000-000000000000", ATTBridge.GetIDFA());
        }

        [Test]
        public void GetIDFA_ImplCanReturnNull()
        {
            ATTBridge.GetIDFAImpl = () => null;
            Assert.IsNull(ATTBridge.GetIDFA(),
                "null surfaces to callers when ATT denied / IDFA unavailable");
        }
    }

    [TestFixture]
    internal class AttributionContextTests
    {
        private Func<int>? _originalGetStatus;
        private Func<string?>? _originalGetIDFA;

        [SetUp]
        public void SetUp()
        {
            _originalGetStatus = ATTBridge.GetStatusImpl;
            _originalGetIDFA = ATTBridge.GetIDFAImpl;
        }

        [TearDown]
        public void TearDown()
        {
            ATTBridge.GetStatusImpl = _originalGetStatus!;
            ATTBridge.GetIDFAImpl = _originalGetIDFA!;
        }

        [Test]
        public void Capture_StatusAuthorized_IncludesIdfa()
        {
            ATTBridge.GetStatusImpl = () => 3;
            ATTBridge.GetIDFAImpl = () => "11111111-2222-3333-4444-555555555555";

            var ctx = AttributionContext.Capture();

            Assert.AreEqual("authorized", ctx["att_status"]);
            Assert.AreEqual("11111111-2222-3333-4444-555555555555", ctx["idfa"]);
        }

        [Test]
        public void Capture_StatusDenied_OmitsIdfa()
        {
            ATTBridge.GetStatusImpl = () => 2;
            // Real native bridge returns null for the all-zeros UUID; we still
            // gate on status to make the contract explicit.
            ATTBridge.GetIDFAImpl = () =>
                throw new InvalidOperationException("must not be called");

            var ctx = AttributionContext.Capture();

            Assert.AreEqual("denied", ctx["att_status"]);
            Assert.IsFalse(ctx.ContainsKey("idfa"),
                "idfa must be omitted when ATT is not authorized");
        }

        [Test]
        public void Capture_StatusNotDetermined_OmitsIdfa()
        {
            ATTBridge.GetStatusImpl = () => 0;
            ATTBridge.GetIDFAImpl = () =>
                throw new InvalidOperationException("must not be called");

            var ctx = AttributionContext.Capture();

            Assert.AreEqual("notDetermined", ctx["att_status"]);
            Assert.IsFalse(ctx.ContainsKey("idfa"));
        }

        [Test]
        public void Capture_StatusRestricted_OmitsIdfa()
        {
            ATTBridge.GetStatusImpl = () => 1;
            ATTBridge.GetIDFAImpl = () =>
                throw new InvalidOperationException("must not be called");

            var ctx = AttributionContext.Capture();

            Assert.AreEqual("restricted", ctx["att_status"]);
            Assert.IsFalse(ctx.ContainsKey("idfa"));
        }

        [Test]
        public void Capture_StatusAuthorizedButIdfaNull_OmitsIdfa()
        {
            // Defensive: Apple has been known to return nil under some
            // configurations even when status reports authorized.
            ATTBridge.GetStatusImpl = () => 3;
            ATTBridge.GetIDFAImpl = () => null;

            var ctx = AttributionContext.Capture();

            Assert.AreEqual("authorized", ctx["att_status"]);
            Assert.IsFalse(ctx.ContainsKey("idfa"));
        }

        [Test]
        public void AttStatusToString_UnknownValue_ReturnsUnknown()
        {
            Assert.AreEqual("unknown", AttributionContext.AttStatusToString(99));
            Assert.AreEqual("unknown", AttributionContext.AttStatusToString(-1));
        }

        [Test]
        public void AttStatusToString_KnownValues()
        {
            Assert.AreEqual("notDetermined", AttributionContext.AttStatusToString(0));
            Assert.AreEqual("restricted", AttributionContext.AttStatusToString(1));
            Assert.AreEqual("denied", AttributionContext.AttStatusToString(2));
            Assert.AreEqual("authorized", AttributionContext.AttStatusToString(3));
        }

        [Test]
        public void EmitGaidProps_LimitAdTrackingTrue_OmitsRawGaid()
        {
            var props = new Dictionary<string, object>();
            AttributionContext.EmitGaidProps(new GAIDInfo("aaaa-bbbb", limitAdTracking: true), props);

            Assert.IsFalse(props.ContainsKey("gaid"),
                "must never ship the raw GAID when the user has opted out via isLimitAdTrackingEnabled");
            Assert.AreEqual(true, props["gaid_limit_ad_tracking"]);
        }

        [Test]
        public void EmitGaidProps_LimitAdTrackingFalse_ShipsGaid()
        {
            var props = new Dictionary<string, object>();
            AttributionContext.EmitGaidProps(new GAIDInfo("aaaa-bbbb", limitAdTracking: false), props);

            Assert.AreEqual("aaaa-bbbb", props["gaid"]);
            Assert.AreEqual(false, props["gaid_limit_ad_tracking"]);
        }

        [Test]
        public void EmitGaidProps_EmptyGaidLimitFalse_OmitsGaidKeepsFlag()
        {
            var props = new Dictionary<string, object>();
            AttributionContext.EmitGaidProps(new GAIDInfo(string.Empty, limitAdTracking: false), props);

            Assert.IsFalse(props.ContainsKey("gaid"));
            Assert.AreEqual(false, props["gaid_limit_ad_tracking"]);
        }
    }
}
