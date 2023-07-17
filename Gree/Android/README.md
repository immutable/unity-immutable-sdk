# Gree Android plugin

This is the library for the `WebViewPlugin.aar` that is used for Gree in Unity.

To build and use a new version: 
1. Run the `assemble` gradle command. 
2. Rename the generated aar to `WebViewPlugin.aar` 
3. Replace the old version inand replace the old version in `src/packages/Passport/Runtime/ThirdParty/Gree/Assets/Plugins/Android`
4. Update `src/packages/Passport/Runtime/ThirdParty/Gree/Assets/PluginsWebViewObject.cs` if any references were changed