# Gree Android plugin

This is the library for the `ImmutableWebViewPlugin.aar` that is used for Gree in Unity.

To build and use a new version: 
1. Run the `assemble` gradle command (A custom task will copy the generated aar to `src/Packages/Passport/Runtime/ThirdParty/Gree/Assets/Plugins/Android` and rename it to `ImmutableWebViewPlugin.aar`)
2. Update `src/packages/Passport/Runtime/ThirdParty/Gree/Assets/PluginsWebViewObject.cs` if any references were changed
