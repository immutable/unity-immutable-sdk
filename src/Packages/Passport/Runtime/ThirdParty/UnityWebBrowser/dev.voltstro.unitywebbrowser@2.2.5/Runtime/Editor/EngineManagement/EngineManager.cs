#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Shared.Core;

namespace VoltstroStudios.UnityWebBrowser.Editor.EngineManagement
{
    public static class EngineManager
    {
        [Obsolete("Fetching of engine paths is now handled by the Engine class.")]
        public static string GetEngineDirectory(Engine engine, Platform platform)
        {
            Engine.EnginePlatformFiles files = engine.EngineFiles.FirstOrDefault(x => x.platform == platform);
            if (files.engineFileLocation == null)
            {
                Debug.LogError(engine.EngineFilesNotFoundError);
                return null;
            }

            return Path.GetFullPath(files.engineFileLocation);
        }

        [Obsolete("Fetching of engine paths is now handled by the Engine class.")]
        public static string GetEngineDirectory(Engine engine)
        {
            return GetEngineDirectory(engine, GetCurrentEditorPlatform());
        }

        [Obsolete("Fetching of engine paths is now handled by the Engine class.")]
        public static string GetEngineProcessFullPath(Engine engine, Platform platform)
        {
            string appPath = Path.Combine(GetEngineDirectory(engine, platform), engine.GetEngineExecutableName());
            if (platform == Platform.Windows64)
                appPath += ".exe";

            return Path.GetFullPath(appPath);
        }

        [Obsolete("Fetching of engine paths is now handled by the Engine class.")]
        public static string GetEngineProcessFullPath(Engine engine)
        {
            return GetEngineProcessFullPath(engine, GetCurrentEditorPlatform());
        }

        public static Platform GetCurrentEditorPlatform()
        {
            return Platform.Windows64;
        }
    }
}

#endif

#endif