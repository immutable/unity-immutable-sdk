#nullable enable

using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace Immutable.Audience.Editor
{
    // Injects android.permission.INTERNET into the generated unityLibrary manifest.
    //
    // The SDK sends events via System.Net.Http.HttpClient, not UnityWebRequest, so
    // Unity does not auto-add INTERNET. This post-processor ensures the permission
    // is always present regardless of how the package is installed (file:, git, or
    // UPM registry), without requiring the studio to set ForceInternetPermission.
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
                    node.Attributes?["android:name"]?.Value == InternetPermission)
                    return;
            }

            var elem = doc.CreateElement("uses-permission");
            elem.SetAttribute("name", AndroidNs, InternetPermission);
            root.AppendChild(elem);
            doc.Save(manifestPath);
        }
    }
}
