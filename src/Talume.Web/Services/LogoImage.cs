using System.Buffers.Binary;
using System.IO.Compression;
namespace Talume.Web.Services;

// Browser uploads are normalized to a non-interlaced RGB/RGBA PNG before sending.
// Validate the actual bytes and bounded raster, not a client-supplied MIME type.
public static class LogoImage
{
    public const int MaxBytes = 512 * 1024;
    public static bool Valid(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        const string prefix = "data:image/png;base64,";
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || value.Length > prefix.Length + ((MaxBytes + 2) / 3) * 4) return false;
        try {
            var png = Convert.FromBase64String(value[prefix.Length..]);
            if (png.Length > MaxBytes || png.Length < 57 || !png.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return false;
            var offset = 8; var width = 0; var height = 0; var channels = 0; var ended = false; var hasData = false;
            using var compressed = new MemoryStream();
            while (offset <= png.Length - 12) {
                var len = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset,4));
                if (len > (uint)(png.Length - offset - 12)) return false;
                int count = (int)len;
                var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
                if (Crc(png.AsSpan(offset + 4, count + 4)) != BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + 8 + count,4))) return false;
                if (offset == 8 && type != "IHDR") return false;
                var data = png.AsSpan(offset + 8,count);
                if (type == "IHDR") {
                    if (offset != 8 || count != 13) return false;
                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(data[..4]); height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4,4));
                    if (width is < 1 or > 2000 || height is < 1 or > 2000 || data[8] != 8 || (data[9] != 2 && data[9] != 6) || data[10] != 0 || data[11] != 0 || data[12] != 0) return false;
                    channels = data[9] == 6 ? 4 : 3;
                } else if (type == "IDAT") { compressed.Write(data); hasData = true; }
                else if (type == "IEND") { if (count != 0) return false; ended = true; offset += 12; break; }
                else if (type.Length != 4 || (type[0] is >= 'A' and <= 'Z' && type != "PLTE")) return false;
                offset += count + 12;
            }
            if (!ended || !hasData || offset != png.Length) return false;
            compressed.Position = 0;
            using var raster = new ZLibStream(compressed, CompressionMode.Decompress);
            var row = new byte[width * channels];
            for (int y = 0; y < height; y++) {
                int filter = raster.ReadByte(); if (filter is < 0 or > 4) return false;
                raster.ReadExactly(row);
            }
            return raster.ReadByte() == -1;
        } catch (Exception e) when (e is FormatException or InvalidDataException or IOException or ArgumentException or OverflowException) { return false; }
    }
    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xffffffff;
        foreach (var value in bytes) { crc ^= value; for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 1 ? 0xedb88320u : 0); }
        return ~crc;
    }
}
