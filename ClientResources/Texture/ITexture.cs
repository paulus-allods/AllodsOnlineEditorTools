namespace AllodsOnlineEditorTools.ClientResources.Texture;

public interface ITexture
{
    public int GetHeight();
    public int GetWidth();
    public TextureType GetTextureType();
    public void SetTextureType(TextureType textureType);
    public string GetFilePath();
}