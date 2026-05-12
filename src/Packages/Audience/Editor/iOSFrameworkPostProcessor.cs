#nullable enable

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Immutable.Audience.Editor
{
    /// <summary>
    /// Links the Apple frameworks the runtime mobile-attribution code needs
    /// into the generated iOS Xcode project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>AdSupport.framework</c> hosts <c>ASIdentifierManager</c> (the IDFA
    /// accessor). Without it linked, the runtime <c>NSClassFromString</c>
    /// lookup returns nil and the IDFA is silently dropped from
    /// <c>game_launch</c>. The Info.plist key alone does not load this
    /// framework. Linked as a required dep; the framework has shipped on
    /// every iOS release since 6.0.
    /// </para>
    /// <para>
    /// <c>AppTrackingTransparency.framework</c> hosts <c>ATTrackingManager</c>.
    /// Unity often auto-links this when <c>NSUserTrackingUsageDescription</c>
    /// is present, but the auto-link heuristic is undocumented and
    /// version-sensitive. An explicit weak link is immune to that drift and
    /// keeps the binary loadable on iOS &lt; 14 (the runtime code already
    /// guards via <c>NSClassFromString</c>).
    /// </para>
    /// <para>
    /// Gated on the same <c>AUDIENCE_MOBILE_ATTRIBUTION</c> scripting define
    /// as the Info.plist post-processor so studios who haven't opted into
    /// attribution ship a clean Frameworks list.
    /// </para>
    /// </remarks>
    internal static class iOSFrameworkPostProcessor
    {
        // Runs just after the Info.plist post-processor (9050). Order is
        // independent in practice (both edit different files), but keeping
        // them adjacent makes the build log obvious.
        internal const int CallbackOrder = 9051;

        [PostProcessBuild(CallbackOrder)]
        internal static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

#if UNITY_IOS
            if (!AttributionDefineEnabled()) return;

            var pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            if (!File.Exists(pbxPath))
            {
                Debug.LogWarning(
                    $"[ImmutableAudience] iOS framework post-processor: project.pbxproj not found at {pbxPath}. Skipping.");
                return;
            }

            var pbx = new PBXProject();
            pbx.ReadFromFile(pbxPath);

            // Native plugin code under Runtime/Plugins/iOS compiles into the
            // UnityFramework target on Unity 2019.3+. Linking against the
            // main target instead leaves the symbols unresolved at runtime
            // and the IDFA / ATT calls silently no-op.
            var frameworkTarget = pbx.GetUnityFrameworkTargetGuid();

            pbx.AddFrameworkToProject(frameworkTarget, "AdSupport.framework", weak: false);
            pbx.AddFrameworkToProject(frameworkTarget, "AppTrackingTransparency.framework", weak: true);

            pbx.WriteToFile(pbxPath);
#endif
        }

        // Reads the iOS-target define list specifically. The post-processor
        // mutates iOS build output regardless of which target the editor is
        // currently focused on.
        private static bool AttributionDefineEnabled()
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS) ?? string.Empty;
            foreach (var define in defines.Split(';'))
            {
                if (define.Trim() == iOSInfoPlistPostProcessor.AttributionDefine) return true;
            }
            return false;
        }
    }
}
