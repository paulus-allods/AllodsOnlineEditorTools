namespace AllodsOnlineEditorTools.ClientResources.Texture.DDS;

public partial class DDSTexture
{
    public DDSHeader Header { get; set; }
    public byte[][] MipMaps { get; set; }

    public DDSTexture()
    {
        Header = new DDSHeader();
        MipMaps = [];
    }

    public uint Width => Header.Width;
    public uint Height => Header.Height;
    public uint MipMapCount => Header.MipMapCount;
    public bool HasMipMaps => MipMapCount > 1;
    public bool IsCubeMap => (Header.Caps2 & DDSCaps2.DDSCAPS2_CUBEMAP) != 0;
    public bool IsVolumeTexture => (Header.Caps2 & DDSCaps2.DDSCAPS2_VOLUME) != 0;

    public static uint GetMipMapSize(uint width, uint height, uint fourCC)
    {
        var blockSize = GetBlockSize(fourCC);
        if (blockSize == 0)
        {
            throw new NotSupportedException($"Unsupported compressed texture format (FourCC 0x{fourCC:X8})");
        }

        var blocksWide = Math.Max(1, (width + 3) / 4);
        var blocksHigh = Math.Max(1, (height + 3) / 4);
        return blocksWide * blocksHigh * blockSize;
    }

    public static uint GetBlockSize(uint fourCC)
    {
        return fourCC switch
        {
            FourCC.DXT1 or FourCC.BC4U or FourCC.BC4S or FourCC.ATI1 => 8,
            FourCC.DXT2 or FourCC.DXT3 or FourCC.DXT4 or FourCC.DXT5 or FourCC.BC5U or FourCC.BC5S or FourCC.ATI2 => 16,
            _ => 0
        };
    }
}
