using System.IO.Compression;
using System.Reflection;
using AllodsOnlineEditorTools.ClientResources.Structs;

namespace AllodsOnlineEditorTools.ClientResources.Texture.DDS;

public partial class DDSTexture
{
    public static DDSTexture LoadBin(Stream binFile, ITexture metadata)
    {
        var mipmaps = ReadMipmaps(binFile);

        var mipMapCount = (uint)mipmaps.Count;
        var hasMipMaps = mipMapCount > 1;
        var dataSize = (uint)mipmaps[0].Length;

        var texture = new DDSTexture
        {
            Header = { Flags = DDSFlags.DDSD_CAPS | DDSFlags.DDSD_HEIGHT | DDSFlags.DDSD_WIDTH | DDSFlags.DDSD_PIXELFORMAT | DDSFlags.DDSD_LINEARSIZE }
        };

        if (hasMipMaps)
        {
            texture.Header.Flags |= DDSFlags.DDSD_MIPMAPCOUNT;
        }

        texture.Header.Width = (uint)metadata.GetWidth();
        texture.Header.Height = (uint)metadata.GetHeight();
        texture.Header.PitchOrLinearSize = dataSize;
        texture.Header.Depth = 0;
        texture.Header.MipMapCount = hasMipMaps ? mipMapCount : 0;
        texture.Header.SetCustomMetadata("AllodsOnlineEditorTools", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "", "");

        texture.Header.PixelFormat.Size = 32;
        texture.Header.PixelFormat.Flags = DDSPixelFormatFlags.DDPF_FOURCC;
        texture.Header.PixelFormat.FourCCString = metadata.GetTextureType().ToString();
        texture.Header.PixelFormat.RGBBitCount = 0;
        texture.Header.PixelFormat.RBitMask = 0;
        texture.Header.PixelFormat.GBitMask = 0;
        texture.Header.PixelFormat.BBitMask = 0;
        texture.Header.PixelFormat.ABitMask = 0;

        texture.Header.Caps = DDSCaps.DDSCAPS_TEXTURE;

        if (hasMipMaps)
        {
            texture.Header.Caps |= DDSCaps.DDSCAPS_COMPLEX | DDSCaps.DDSCAPS_MIPMAP;
        }

        texture.Header.Caps2 = 0;
        texture.Header.Caps3 = 0;
        texture.Header.Caps4 = 0;
        texture.Header.Reserved2 = 0;

        texture.MipMaps = mipmaps.ToArray();

        return texture;
    }

    private static List<byte[]> ReadMipmaps(Stream compressedBinFile)
    {
        using var decompressedFile = new MemoryStream();
        using var inflater = new ZLibStream(compressedBinFile, CompressionMode.Decompress);
        inflater.CopyTo(decompressedFile);
        decompressedFile.Seek(0, SeekOrigin.Begin);
        using var reader = new BinaryReader(decompressedFile);
        var mipmaps = new List<byte[]>();
        int mipmapId;
        do
        {
            mipmapId = reader.ReadInt32();
            var mipmapSize = reader.ReadInt32();
            mipmaps.Add(reader.ReadBytes(mipmapSize));
        } while (mipmapId != 0);

        mipmaps.Reverse();
        return mipmaps;
    }

    public void SaveAsBin(Stream stream)
    {
        throw new NotImplementedException();
    }

    public ITexture GenerateMetadata(GameVersion version)
    {
        var typeName = $"{version.FullNamespace}.Texture";
        var textureType = Assembly.GetExecutingAssembly().GetType(typeName);

        if (textureType == null)
        {
            throw new InvalidOperationException($"Texture type not found for version {version}: {typeName}");
        }

        if (Activator.CreateInstance(textureType) is not ITexture textureInstance)
        {
            throw new InvalidOperationException($"Failed to create instance of {typeName}, make sure it inherits ITexture interface");
        }

        SetField(textureInstance, "width", (int)Width);
        SetField(textureInstance, "height", (int)Height);
        SetField(textureInstance, "realWidth", (int)Width);
        SetField(textureInstance, "realHeight", (int)Height);
        SetField(textureInstance, "mipsNumber", (int)MipMapCount);
        SetField(textureInstance, "generateMipChain", HasMipMaps);

        var fourCCString = Header.PixelFormat.FourCCString;
        textureInstance.SetTextureType(Enum.Parse<TextureType>(fourCCString));

        return textureInstance;
    }

    private static void SetField(ITexture texture, string fieldName, object value)
    {
        var field = texture.GetType().GetField(fieldName) ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {texture.GetType().Name}");
        field.SetValue(texture, value);
    }
}
