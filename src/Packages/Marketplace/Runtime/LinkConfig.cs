using System;
using System.Collections.Generic;
using System.Linq;

namespace Immutable.Marketplace
{
    public enum Flow
    {
        OnRamp,
        Swap,
        Bridge
    }

    public enum Environment
    {
        Sandbox,
        Production
    }

    public static class LinkConfig
    {
        private static string _onRampBaseUrlSandbox = "https://global-stg.transak.com";
        private static string _onRampBaseUrlProduction = "https://global.transak.com";

        private static string _swapBaseUrlSandbox = "https://checkout-playground.sandbox.immutable.com/checkout/swap";
        private static string _swapBaseUrlProduction = "https://toolkit.immutable.com/checkout/swap";

        private static string _bridgeBaseUrlSandbox = "https://checkout-playground.sandbox.immutable.com/checkout/squid";
        private static string _bridgeBaseUrlProduction = "https://toolkit.immutable.com/checkout/squid";

        // These can be hardcoded as they are public API keys
        private static string _apiOnRampKeySandbox = "d14b44fb-0f84-4db5-affb-e044040d724b";
        private static string _apiOnRampKeyProduction = "ad1bca70-d917-4628-bb0f-5609537498bc";

        public static string GetBaseUrl(Environment environment, Flow flow)
        {
            return flow switch
            {
                Flow.OnRamp => environment == Environment.Sandbox ? _onRampBaseUrlSandbox : _onRampBaseUrlProduction,
                Flow.Swap => environment == Environment.Sandbox ? _swapBaseUrlSandbox : _swapBaseUrlProduction,
                Flow.Bridge => environment == Environment.Sandbox ? _bridgeBaseUrlSandbox : _bridgeBaseUrlProduction,
                _ => throw new ArgumentException("Invalid flow")
            };
        }

        public static string GetApiKey(Environment environment, Flow flow)
        {
            return flow is Flow.OnRamp ?
                environment == Environment.Sandbox ? _apiOnRampKeySandbox : _apiOnRampKeyProduction :
                string.Empty;
        }
    }
}
