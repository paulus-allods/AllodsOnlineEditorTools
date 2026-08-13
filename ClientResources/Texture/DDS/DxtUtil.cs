namespace AllodsOnlineEditorTools.ClientResources.Texture.DDS;

public static class DxtUtil
{
    private readonly record struct Rgb(byte R, byte G, byte B);

    public static byte[] DecompressDxt1(byte[] imageData, int width, int height)
    {
        using var imageStream = new MemoryStream(imageData);
        return DecompressDxt1(imageStream, width, height);
    }

    public static byte[] DecompressDxt1(Stream imageStream, int width, int height)
    {
        return DecompressBlocks(imageStream, width, height, DecompressDxt1Block);
    }

    public static byte[] DecompressDxt3(byte[] imageData, int width, int height)
    {
        using var imageStream = new MemoryStream(imageData);
        return DecompressDxt3(imageStream, width, height);
    }

    public static byte[] DecompressDxt3(Stream imageStream, int width, int height)
    {
        return DecompressBlocks(imageStream, width, height, DecompressDxt3Block);
    }

    public static byte[] DecompressDxt5(byte[] imageData, int width, int height)
    {
        using var imageStream = new MemoryStream(imageData);
        return DecompressDxt5(imageStream, width, height);
    }

    public static byte[] DecompressDxt5(Stream imageStream, int width, int height)
    {
        return DecompressBlocks(imageStream, width, height, DecompressDxt5Block);
    }

    private delegate void BlockDecoder(BinaryReader reader, int x, int y, int width, int height, byte[] imageData);

    private static byte[] DecompressBlocks(Stream imageStream, int width, int height, BlockDecoder decodeBlock)
    {
        var imageData = new byte[width * height * 4];

        using var imageReader = new BinaryReader(imageStream);
        var blockCountX = (width + 3) / 4;
        var blockCountY = (height + 3) / 4;

        for (var y = 0; y < blockCountY; y++)
        {
            for (var x = 0; x < blockCountX; x++)
            {
                decodeBlock(imageReader, x, y, width, height, imageData);
            }
        }

        return imageData;
    }

    private static void DecompressDxt1Block(BinaryReader imageReader, int x, int y, int width, int height,
        byte[] imageData)
    {
        var c0 = imageReader.ReadUInt16();
        var c1 = imageReader.ReadUInt16();
        var color0 = ConvertRgb565ToRgb888(c0);
        var color1 = ConvertRgb565ToRgb888(c1);

        var lookupTable = imageReader.ReadUInt32();

        for (var blockY = 0; blockY < 4; blockY++)
        {
            for (var blockX = 0; blockX < 4; blockX++)
            {
                var index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                Rgb color;
                byte a = 255;
                if (c0 > c1)
                {
                    color = SelectColor(index, color0, color1);
                }
                else
                {
                    color = index switch
                    {
                        0 => color0,
                        1 => color1,
                        2 => Average(color0, color1),
                        _ => default,
                    };
                    if (index == 3)
                    {
                        a = 0;
                    }
                }

                WritePixel(imageData, x, y, blockX, blockY, width, height, color, a);
            }
        }
    }

    private static void DecompressDxt3Block(BinaryReader imageReader, int x, int y, int width, int height,
        byte[] imageData)
    {
        var alphaBytes = imageReader.ReadBytes(8);

        var c0 = imageReader.ReadUInt16();
        var c1 = imageReader.ReadUInt16();
        var color0 = ConvertRgb565ToRgb888(c0);
        var color1 = ConvertRgb565ToRgb888(c1);

        var lookupTable = imageReader.ReadUInt32();

        for (var blockY = 0; blockY < 4; blockY++)
        {
            for (var blockX = 0; blockX < 4; blockX++)
            {
                var pixelIndex = 4 * blockY + blockX;
                var index = (lookupTable >> 2 * pixelIndex) & 0x03;

                // Each pixel's alpha is a 4-bit value; expand a nibble to a full byte via n * 17.
                var alphaByte = alphaBytes[pixelIndex >> 1];
                var nibble = (pixelIndex & 1) == 0 ? alphaByte & 0x0F : alphaByte >> 4;
                var a = (byte)(nibble | (nibble << 4));

                var color = SelectColor(index, color0, color1);
                WritePixel(imageData, x, y, blockX, blockY, width, height, color, a);
            }
        }
    }

    private static void DecompressDxt5Block(BinaryReader imageReader, int x, int y, int width, int height,
        byte[] imageData)
    {
        var alpha0 = imageReader.ReadByte();
        var alpha1 = imageReader.ReadByte();

        var alphaMask = (ulong)imageReader.ReadByte();
        alphaMask += (ulong)imageReader.ReadByte() << 8;
        alphaMask += (ulong)imageReader.ReadByte() << 16;
        alphaMask += (ulong)imageReader.ReadByte() << 24;
        alphaMask += (ulong)imageReader.ReadByte() << 32;
        alphaMask += (ulong)imageReader.ReadByte() << 40;

        var c0 = imageReader.ReadUInt16();
        var c1 = imageReader.ReadUInt16();
        var color0 = ConvertRgb565ToRgb888(c0);
        var color1 = ConvertRgb565ToRgb888(c1);

        var lookupTable = imageReader.ReadUInt32();

        for (var blockY = 0; blockY < 4; blockY++)
        {
            for (var blockX = 0; blockX < 4; blockX++)
            {
                var pixelIndex = 4 * blockY + blockX;
                var index = (lookupTable >> 2 * pixelIndex) & 0x03;
                var alphaIndex = (uint)((alphaMask >> 3 * pixelIndex) & 0x07);

                var a = alphaIndex switch
                {
                    0 => alpha0,
                    1 => alpha1,
                    _ when alpha0 > alpha1 => (byte)(((8 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 7),
                    6 => (byte)0,
                    7 => (byte)0xff,
                    _ => (byte)(((6 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 5),
                };

                var color = SelectColor(index, color0, color1);
                WritePixel(imageData, x, y, blockX, blockY, width, height, color, a);
            }
        }
    }

    // Color selection shared by DXT1 (c0 > c1 branch), DXT3 and DXT5: the two extra indices
    // are the 2:1 and 1:2 interpolations between the two endpoint colors.
    private static Rgb SelectColor(uint index, Rgb c0, Rgb c1)
    {
        return index switch
        {
            0 => c0,
            1 => c1,
            2 => new Rgb(Lerp21(c0.R, c1.R), Lerp21(c0.G, c1.G), Lerp21(c0.B, c1.B)),
            _ => new Rgb(Lerp21(c1.R, c0.R), Lerp21(c1.G, c0.G), Lerp21(c1.B, c0.B)),
        };
    }

    private static byte Lerp21(byte a, byte b) => (byte)((2 * a + b) / 3);

    private static Rgb Average(Rgb a, Rgb b) =>
        new((byte)((a.R + b.R) / 2), (byte)((a.G + b.G) / 2), (byte)((a.B + b.B) / 2));

    private static void WritePixel(byte[] imageData, int x, int y, int blockX, int blockY, int width, int height,
        Rgb color, byte a)
    {
        var px = (x << 2) + blockX;
        var py = (y << 2) + blockY;
        if (px >= width || py >= height)
        {
            return;
        }

        var offset = ((py * width) + px) << 2;
        imageData[offset] = color.R;
        imageData[offset + 1] = color.G;
        imageData[offset + 2] = color.B;
        imageData[offset + 3] = a;
    }

    private static Rgb ConvertRgb565ToRgb888(ushort color)
    {
        var temp = (color >> 11) * 255 + 16;
        var r = (byte)((temp / 32 + temp) / 32);
        temp = ((color & 0x07E0) >> 5) * 255 + 32;
        var g = (byte)((temp / 64 + temp) / 64);
        temp = (color & 0x001F) * 255 + 16;
        var b = (byte)((temp / 32 + temp) / 32);
        return new Rgb(r, g, b);
    }
}
