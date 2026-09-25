#nullable enable

using UnityEditor;

namespace Immutable.Audience.Editor
{
    /// <summary>
    /// Adds the mobile attribution toggles to <see cref="AudienceMobileBuildSettings"/>.
    /// </summary>
    /// <remarks>
    /// The toggles read and write <see cref="MobileAttributionDefine"/> directly.
    /// They are not fields on the asset, so an existing define set manually
    /// (the old way) shows correctly the first time this asset is opened,
    /// and nothing gets reset by updating the package.
    /// </remarks>
    [CustomEditor(typeof(AudienceMobileBuildSettings))]
    internal sealed class AudienceMobileBuildSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Mobile Attribution", EditorStyles.boldLabel);
            DrawToggle(BuildTargetGroup.iOS, "Enable for iOS");
            DrawToggle(BuildTargetGroup.Android, "Enable for Android");
            EditorGUILayout.Space();

            DrawDefaultInspector();
        }

        private static void DrawToggle(BuildTargetGroup group, string label)
        {
            var enabled = MobileAttributionDefine.IsEnabled(group);
            var toggled = EditorGUILayout.Toggle(label, enabled);
            if (toggled != enabled) MobileAttributionDefine.SetEnabled(group, toggled);
        }
    }
}
