#if IMMUTABLE_AUDIENCE_GZIP
using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class GzipTests
    {
        [Test]
        public void Compress_ProducesValidGzip_ThatDecompressesToOriginal()
        {
            var original = WireFixture.Track((MessageFields.EventName, "test"));

            var compressed = Gzip.Compress(original);

            // Decompress and verify round-trip
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            var decompressed = reader.ReadToEnd();

            Assert.AreEqual(original, decompressed);
        }

        [Test]
        public void Compress_OutputIsSmallerThanInput_ForRealisticPayload()
        {
            // Repeated field names compress well in JSON batches.
            var sb = new StringBuilder("{\"batch\":[");
            for (var i = 0; i < 20; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(WireFixture.Track(
                    (MessageFields.EventName, TestEventNames.LevelComplete),
                    (MessageFields.AnonymousId, $"{TestFixtures.AnonIdPrefix}{i}")));
            }

            sb.Append("]}");
            var payload = sb.ToString();

            var compressed = Gzip.Compress(payload);

            Assert.Less(compressed.Length, Encoding.UTF8.GetByteCount(payload), "gzip should compress a batch of similar JSON events");
        }

        [Test]
        public void Compress_EmptyString_ProducesValidGzip()
        {
            var compressed = Gzip.Compress("");

            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            var decompressed = reader.ReadToEnd();

            Assert.AreEqual("", decompressed);
        }
    }
}
#endif
