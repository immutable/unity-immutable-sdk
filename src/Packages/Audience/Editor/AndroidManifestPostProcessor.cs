#nullable enable

using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace Immutable.Audience.Editor
{
    // Injects android.permission.INTERNET into the generated unityLibrary
    // manifest. The SDK sends events via System.Net.Http.HttpClient (not
    // UnityWebRequest), so Unity does not auto-add INTERNET.
    //
    // AD_ID is intentionally NOT injected here. It comes from the
    // play-services-ads-identifier AAR's own manifest via AGP merging when
    // the studio adds the Maven dependency. Injecting it ourselves would
    // declare the permission for studios who never pull in the AAR (and so
    // can never collect GAID), creating a Play Store Data Safety mismatch.
    internal sealed class AndroidManifestPostProcessor : IPostGenerateGradleAndroidProject
    {
        private const string InternetPermission = "android.permission.INTERNET";
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
                return;

            var doc = new XmlDocument();
            doc.Load(manifestPath);

            var root = doc.DocumentElement;
            if (root == null)
                return;

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.Name == "uses-permission" &&
                    node.Attributes?.GetNamedItem("name", AndroidNs)?.Value == InternetPermission)
                    return;
            }

            var elem = doc.CreateElement("uses-permission");
            elem.SetAttribute("name", AndroidNs, InternetPermission);
            root.AppendChild(elem);
            doc.Save(manifestPath);
        }
    }
}
