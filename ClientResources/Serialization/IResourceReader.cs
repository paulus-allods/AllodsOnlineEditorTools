namespace AllodsOnlineEditorTools.ClientResources.Serialization;

public interface IResourceReader
{
    object ParseResource(string text, out int resourceId);
}
