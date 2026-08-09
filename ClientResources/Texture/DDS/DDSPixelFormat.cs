using System.Text;
// ReSharper disable InconsistentNaming

namespace AllodsOnlineEditorTools.ClientResources.Texture.DDS;

public class DDSPixelFormat
{
    public uint Size { get; set; } = 32;
    public DDSPixelFormatFlags Flags { get; set; }
    public uint FourCC { get; set; }
    public uint RGBBitCount { get; set; }
    public uint RBitMask { get; set; }
    public uint GBitMask { get; set; }
    public uint BBitMask { get; set; }
    public uint ABitMask { get; set; }

    public string FourCCString
    {
        get => AllodsOnlineEditorTools.ClientResources.Texture.DDS.FourCC.ToString(FourCC);
        set => FourCC = AllodsOnlineEditorTools.ClientResources.Texture.DDS.FourCC.FromString(value);
    }
}

public static class FourCC
{
    public const uint DXT1 = 0x31545844;
    public const uint DXT2 = 0x32545844;
    public const uint DXT3 = 0x33545844;
    public const uint DXT4 = 0x34545844;
    public const uint DXT5 = 0x35545844;
    public const uint DX10 = 0x30315844;
    public const uint ATI1 = 0x31495441;
    public const uint ATI2 = 0x32495441;
    public const uint BC4U = 0x55344342;
    public const uint BC4S = 0x53344342;
    public const uint BC5U = 0x55354342;
    public const uint BC5S = 0x53354342;

    public static string ToString(uint fourCC)
    {
        return Encoding.ASCII.GetString(new byte[]
        {
            (byte)(fourCC & 0xFF),
            (byte)((fourCC >> 8) & 0xFF),
            (byte)((fourCC >> 16) & 0xFF),
            (byte)((fourCC >> 24) & 0xFF)
        });
    }

    public static uint FromString(string fourCC)
    {
        if (string.IsNullOrEmpty(fourCC) || fourCC.Length > 4)
            throw new ArgumentException("FourCC must be 1-4 characters");

        var bytes = Encoding.ASCII.GetBytes(fourCC.PadRight(4, '\0'));
        return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
    }
}