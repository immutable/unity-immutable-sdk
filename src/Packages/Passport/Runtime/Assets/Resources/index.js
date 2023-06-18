const clientId = "mjtCL8mt06BtbxSkp2vbrYStKWnXVZfo" // not required
const scope = "openid offline_access profile email transact"
const audience = "platform_api"
const redirectUri = "https://localhost:3000/" // Not required
const logoutRedirectUri = "https://localhost:3000/" // Not required

const keyFunctionName = "fxName";
const keyRequestId = "requestId";
const keyData = "data";

const PassportFunctions = {
    getAddress: "getAddress",
    getImxProvider: "getImxProvider",
    signMessage: "signMessage"
}

const getPassportConfig = () => {
    const sharedConfigurationValues = {
        scope,
        audience,
        redirectUri,
        logoutRedirectUri,
    };

    return {
        baseConfig: new config.ImmutableConfiguration({
            environment: config.Environment.SANDBOX,
        }),
        clientId: clientId,
        ...sharedConfigurationValues,
    };
};

var passportClient = new passport.Passport(getPassportConfig());
var providerInstance;

const callbackToUnity = function (data) {
    const message = JSON.stringify(data);
    console.log(`callbackToUnity: ${message}`);
    console.log(message);
    if (UnityPostMessage !== undefined) {
        UnityPostMessage(message);
    }
}

async function callFunction(jsonData) {
    console.log(`Call function ${jsonData}`);

    var fxName = null;
    var requestId = null;

    try {
        let json = JSON.parse(jsonData);
        fxName = json[keyFunctionName];
        requestId = json[keyRequestId];
        let data = json[keyData];

        switch (fxName) {
            case PassportFunctions.getAddress: {
                const address = await providerInstance?.getAddress();
                callbackToUnity({
                    responseFor: fxName,
                    requestId: requestId,
                    success: true,
                    address: address
                });
                break;
            }
            case PassportFunctions.getImxProvider: {
                let provider = await passportClient?.getImxProvider(JSON.parse(data));
                var success = false;
                if (provider !== null && provider !== undefined) {
                    providerInstance = provider;
                    success = true;
                    console.log("IMX provider set");
                } else {
                    console.log("No IMX provider");
                }
                callbackToUnity({
                    responseFor: fxName,
                    requestId: requestId,
                    success: true
                });
                break;
            }
            case PassportFunctions.signMessage: {
                let signed = await passportClient?.signMessage(data);
                callbackToUnity({
                    responseFor: fxName,
                    requestId: requestId,
                    success: true,
                    result: signed
                });
                break;
            }
        }
    } catch (error) {
        console.log(error);

        var errorType = null;
        if (error instanceof passport.PassportError) {
            errorType = error.type;
        }

        callbackToUnity({
            responseFor: fxName,
            requestId: requestId,
            success: false,
            error: error.message,
            errorType: error instanceof passport.PassportError ? error.type : null
        });
    }
}