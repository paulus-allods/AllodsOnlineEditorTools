using AllodsOnlineEditorTools.ClientResources.Texture.DDS;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AllodsOnlineEditorTools.ClientResources.Texture;

public static class ImageSharpExtensions
{
    extension(Image)
    {
        public static Image FromBinTexture(Stream binFile, ITexture texture)
        {
            var dds = DDSTexture.LoadBin(binFile, texture);
            var bitmap = texture.GetTextureType() switch
            {
                //TODO: C565, RGBA
                TextureType.DXT1 => DxtUtil.DecompressDxt1(dds.MipMaps[0], texture.GetWidth(), texture.GetHeight()),
                TextureType.DXT3 => DxtUtil.DecompressDxt3(dds.MipMaps[0], texture.GetWidth(), texture.GetHeight()),
                TextureType.DXT5 => DxtUtil.DecompressDxt5(dds.MipMaps[0], texture.GetWidth(), texture.GetHeight()),
                _ => throw new NotSupportedException(
                    $"Texture type {texture.GetTextureType()} cannot be converted to image")
            };
            return Image.LoadPixelData<Rgba32>(bitmap, texture.GetWidth(), texture.GetHeight());
        }
    }
}
