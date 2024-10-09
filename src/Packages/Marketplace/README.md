# Immutable.Marketplace - C# library for Immutable X Marketplace

This C# library provides functionality for interacting with the Immutable X Marketplace, including on-ramp services.

## Version Support

This library supports:

- Unity 2020.3 (LTS) and up
- .NET Standard 2.1 / .NET Framework

## Dependencies

- [UniTask](https://github.com/Cysharp/UniTask) - For asynchronous operations
- Unity Engine

## Installation

Add the dependencies to your Unity project. You can do this through the Package Manager or by adding them to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.unity.nuget.newtonsoft-json": "3.0.2"
  }
}
```

## Usage

Here's an example of how to use the OnRamp functionality with Immutable Passport:

```csharp
using Immutable.Marketplace.OnRamp;
using Immutable.Passport;
using Immutable.Passport.Model;
using System.Collections.Generic;
using UnityEngine;

public class MarketplaceExample : MonoBehaviour
{
    async void Start()
    {
        string environment = Environment.SANDBOX;
        string email = await Passport.Instance.GetEmail();
        List<string> walletAddresses = await Passport.Instance.ZkEvmRequestAccounts();
        
        OnRamp onRamp = new OnRamp(environment, email, walletAddresses.FirstOrDefault());
        
        try
        {
            string link = await onRamp.GetLink();
            Debug.Log($"onRamp.GetOnRampLink: {link}");
            
            // Open the generated link in the default browser
            Application.OpenURL(link);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting on-ramp link: {e.Message}");
        }
    }
}
```