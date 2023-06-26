/* eslint-disable no-undef */
const scope = 'openid offline_access profile email transact'
const audience = 'platform_api'
const redirectUri = 'https://localhost:3000/' // Not required
const logoutRedirectUri = 'https://localhost:3000/' // Not required

const keyFunctionName = 'fxName'
const keyRequestId = 'requestId'
const keyData = 'data'

const PassportFunctions = {
  getAddress: 'getAddress',
  getImxProvider: 'getImxProvider',
  signMessage: 'signMessage'
}

// To notify Unity that this file is loaded
const initRequest = 'init'
const initRequestId = '1'

const getPassportConfig = () => {
  const sharedConfigurationValues = {
    scope,
    audience,
    redirectUri,
    logoutRedirectUri
  }

  return {
    baseConfig: new config.ImmutableConfiguration({
      environment: config.Environment.SANDBOX
    }),
    clientId: 'NOT_REQUIRED',
    ...sharedConfigurationValues
  }
}

const passportClient = new passport.Passport(getPassportConfig())
let providerInstance

const callbackToUnity = function (data) {
  const message = JSON.stringify(data)
  console.log(`callbackToUnity: ${message}`)
  console.log(message)
  if (UnityPostMessage !== undefined) {
    UnityPostMessage(message)
  }
}

async function callFunction (jsonData) { // eslint-disable-line no-unused-vars
  console.log(`Call function ${jsonData}`)

  let fxName = null
  let requestId = null

  try {
    const json = JSON.parse(jsonData)
    fxName = json[keyFunctionName]
    requestId = json[keyRequestId]
    const data = json[keyData]

    switch (fxName) {
      case PassportFunctions.getAddress: {
        const address = await providerInstance?.getAddress()
        callbackToUnity({
          responseFor: fxName,
          requestId,
          success: true,
          address
        })
        break
      }
      case PassportFunctions.getImxProvider: {
        const provider = await passportClient?.getImxProvider(JSON.parse(data))
        if (provider !== null && provider !== undefined) {
          providerInstance = provider
          console.log('IMX provider set')
        } else {
          console.log('No IMX provider')
        }
        callbackToUnity({
          responseFor: fxName,
          requestId,
          success: true
        })
        break
      }
      case PassportFunctions.signMessage: {
        const signed = await passportClient?.signMessage(data)
        callbackToUnity({
          responseFor: fxName,
          requestId,
          success: true,
          result: signed
        })
        break
      }
    }
  } catch (error) {
    console.log(error)
    callbackToUnity({
      responseFor: fxName,
      requestId,
      success: false,
      error: error.message,
      errorType: error instanceof passport.PassportError ? error.type : null
    })
  }
}

console.log('index.js loaded')
// File loaded
// This is to prevent callFunction not defined error
callbackToUnity({
  responseFor: initRequest,
  requestId: initRequestId,
  success: true
})
