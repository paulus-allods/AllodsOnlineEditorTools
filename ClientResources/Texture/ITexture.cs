namespace AllodsOnlineEditorTools.ClientResources.Texture;

public interface ITexture
{
    int GetHeight();
    int GetWidth();
    TextureType GetTextureType();
    void SetTextureType(TextureType textureType);
    string GetFilePath();
}
