using System.Text;

namespace AllodsOnlineEditorTools.ClientResources.Texture.DDS;

public partial class DDSTexture
{
    public static DDSTexture LoadDDS(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return LoadDDS(fs);
    }
    
    public static DDSTexture LoadDDS(Stream stream)
    {
        using var br = new BinaryReader(stream);
        var texture = new DDSTexture();

        var magic = br.ReadUInt32();
        if (magic != 0x20534444)
        {
            throw new InvalidDataException("Invalid DDS file - magic number mismatch");
        }

        texture.Header = ReadHeader(br);

        if (texture.Header.Size != 124)
        {
            throw new InvalidDataException($"Invalid DDS header size: {texture.Header.Size}");
        }

        var mipCount = texture.HasMipMaps ? texture.MipMapCount : 1;
        texture.MipMaps = new byte[mipCount][];

        var width = texture.Width;
        var height = texture.Height;
        var fourCC = texture.Header.PixelFormat.FourCC;

        for (var i = 0; i < mipCount; i++)
        {
            uint mipSize;
                
            if ((texture.Header.PixelFormat.Flags & DDSPixelFormatFlags.DDPF_FOURCC) != 0)
            {
                // Compressed format
                mipSize = GetMipMapSize(width, height, fourCC);
            }
            else
            {
                // Uncompressed format
                var bytesPerPixel = texture.Header.PixelFormat.RGBBitCount / 8;
                mipSize = width * height * bytesPerPixel;
            }

            texture.MipMaps[i] = br.ReadBytes((int)mipSize);

            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
        }

        return texture;
    }

    private static DDSHeader ReadHeader(BinaryReader br)
    {
        var header = new DDSHeader
        {
            Size = br.ReadUInt32(),
            Flags = (DDSFlags)br.ReadUInt32(),
            Height = br.ReadUInt32(),
            Width = br.ReadUInt32(),
            PitchOrLinearSize = br.ReadUInt32(),
            Depth = br.ReadUInt32(),
            MipMapCount = br.ReadUInt32(),
            Reserved1 = br.ReadBytes(44),
            PixelFormat = new DDSPixelFormat
            {
                Size = br.ReadUInt32(),
                Flags = (DDSPixelFormatFlags)br.ReadUInt32(),
                FourCC = br.ReadUInt32(),
                RGBBitCount = br.ReadUInt32(),
                RBitMask = br.ReadUInt32(),
                GBitMask = br.ReadUInt32(),
                BBitMask = br.ReadUInt32(),
                ABitMask = br.ReadUInt32()
            },
            Caps = (DDSCaps)br.ReadUInt32(),
            Caps2 = (DDSCaps2)br.ReadUInt32(),
            Caps3 = br.ReadUInt32(),
            Caps4 = br.ReadUInt32(),
            Reserved2 = br.ReadUInt32()
        };

        return header;
    }
    
    public void SaveAsDDS(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        SaveAsDDS(fs);
    }
    
    public void SaveAsDDS(Stream stream)
    {
        using var bw = new BinaryWriter(stream, Encoding.ASCII, true);
        // "DDS " magic number
        bw.Write(0x20534444u);

        WriteHeader(bw, Header);

        foreach (var mipData in MipMaps)
        {
            bw.Write(mipData);
        }
    }

    private static void WriteHeader(BinaryWriter bw, DDSHeader header)
    {
        bw.Write(header.Size);
        bw.Write((uint)header.Flags);
        bw.Write(header.Height);
        bw.Write(header.Width);
        bw.Write(header.PitchOrLinearSize);
        bw.Write(header.Depth);
        bw.Write(header.MipMapCount);
        bw.Write(header.Reserved1);

        bw.Write(header.PixelFormat.Size);
        bw.Write((uint)header.PixelFormat.Flags);
        bw.Write(header.PixelFormat.FourCC);
        bw.Write(header.PixelFormat.RGBBitCount);
        bw.Write(header.PixelFormat.RBitMask);
        bw.Write(header.PixelFormat.GBitMask);
        bw.Write(header.PixelFormat.BBitMask);
        bw.Write(header.PixelFormat.ABitMask);

        bw.Write((uint)header.Caps);
        bw.Write((uint)header.Caps2);
        bw.Write(header.Caps3);
        bw.Write(header.Caps4);
        bw.Write(header.Reserved2);
    }
}