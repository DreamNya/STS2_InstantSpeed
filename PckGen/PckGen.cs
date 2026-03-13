using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

internal static class PckGen
{
    // Matches the tiny sample PCK the user uploaded:
    // magic=GDPC, format=2, engine version=4.5.1, flags=0, alignment=32.
    private const uint PackHeaderMagic = 0x43504447; // 'GDPC' little-endian
    private const uint PackFormatVersion = 2;
    private const uint GodotMajor = 4;
    private const uint GodotMinor = 5;
    private const uint GodotPatch = 1;
    private const uint PackFlags = 0;
    private const int Alignment = 32;

    public static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            Console.Error.WriteLine("Usage: PckGen <mod_manifest.json> <output.pck> [res://path]");
            Console.Error.WriteLine(
                "Example: PckGen mod_manifest.json InstantSpeed.pck res://mod_manifest.json"
            );
            return 2;
        }

        string manifestPath = Path.GetFullPath(args[0]);
        string outputPath = Path.GetFullPath(args[1]);
        string resourcePath = args.Length >= 3 ? args[2] : "res://mod_manifest.json";

        byte[] fileData = File.ReadAllBytes(manifestPath);
        byte[] md5 = MD5.HashData(fileData);
        byte[] pathBytes = Encoding.UTF8.GetBytes(resourcePath);
        int pathPaddedLen = Align(pathBytes.Length, 4);

        // V2 layout (matches the uploaded sample PCK):
        // header:
        //   u32 magic
        //   u32 format_version
        //   u32 godot_major
        //   u32 godot_minor
        //   u32 godot_patch
        //   u32 flags
        //   u64 file_base_offset
        //   16 * u32 reserved zeros
        // index:
        //   u32 file_count
        //   repeated per-file:
        //     u32 padded_path_len
        //     utf8 path + zero padding to 4 bytes
        //     u64 relative_file_offset (from file_base)
        //     u64 file_size
        //     16-byte md5
        //     u32 flags
        // then zero padding to 32-byte alignment, then raw file payload(s).

        const int headerSize = 24 + 8 + (16 * 4); // 96 bytes
        int indexSize = 4 + 4 + pathPaddedLen + 8 + 8 + 16 + 4;
        int payloadStart = Align(headerSize + indexSize, Alignment);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var fs = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );
        using var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: false);

        // Header.
        bw.Write(PackHeaderMagic);
        bw.Write(PackFormatVersion);
        bw.Write(GodotMajor);
        bw.Write(GodotMinor);
        bw.Write(GodotPatch);
        bw.Write(PackFlags);
        bw.Write((ulong)payloadStart);
        for (int i = 0; i < 16; i++)
        {
            bw.Write(0u);
        }

        // Index.
        bw.Write(1u); // file count
        bw.Write((uint)pathPaddedLen);
        bw.Write(pathBytes);
        for (int i = pathBytes.Length; i < pathPaddedLen; i++)
        {
            bw.Write((byte)0);
        }

        bw.Write((ulong)0); // first file starts exactly at file_base
        bw.Write((ulong)fileData.Length);
        bw.Write(md5);
        bw.Write(0u); // file flags

        // Pad to payload start.
        while (fs.Position < payloadStart)
        {
            bw.Write((byte)0);
        }

        // Payload.
        bw.Write(fileData);

        // Optional trailing alignment. The uploaded sample also pads the final file to 32 bytes.
        while (fs.Position % Alignment != 0)
        {
            bw.Write((byte)0);
        }

        Console.WriteLine($"Wrote {outputPath}");
        Console.WriteLine($"Size: {new FileInfo(outputPath).Length} bytes");
        return 0;
    }

    private static int Align(int value, int alignment)
    {
        int remainder = value % alignment;
        return remainder == 0 ? value : value + (alignment - remainder);
    }
}
