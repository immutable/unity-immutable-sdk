#if IMMUTABLE_AUDIENCE_GZIP
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Immutable.Audience
{
    // Gzip compression via GZipStream from System.IO.Compression.
    // Available in Unity 2021+ (.NET Standard 2.1). Works on all desktop platforms.
    internal static class Gzip
    {
        internal static byte[] Compress(string text)
        {
            var raw = Encoding.UTF8.GetBytes(text);

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
            {
                gzip.Write(raw, 0, raw.Length);
            }

            return output.ToArray();
        }
    }
}
#endif
