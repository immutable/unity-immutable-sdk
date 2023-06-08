var config = (function (exports) {
    'use strict';

    exports.Environment = void 0;
    (function (Environment) {
        Environment["PRODUCTION"] = "production";
        Environment["SANDBOX"] = "sandbox";
    })(exports.Environment || (exports.Environment = {}));
    class ImmutableConfiguration {
        environment;
        apiKey;
        constructor(options) {
            this.environment = options.environment;
            this.apiKey = options.apiKey;
        }
    }

    exports.ImmutableConfiguration = ImmutableConfiguration;

    return exports;

})({});
