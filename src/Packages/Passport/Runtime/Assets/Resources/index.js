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
    try {
        console.log(`Call function ${jsonData}`);
        let json = JSON.parse(jsonData);
        let fxName = json[keyFunctionName];
        let requestId = json[keyRequestId];
        let data = JSON.parse(json[keyData]);
        try {
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
                    let provider = await passportClient?.getImxProvider(data);
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
            callbackToUnity({
                responseFor: fxName,
                requestId: requestId,
                success: false,
                error: error.message
            });
        }
    } catch (error) {
        window.console.log(error);
        callbackToUnity({
            success: false,
            error: error.message
         });
    }
}