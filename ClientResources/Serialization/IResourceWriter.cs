namespace AllodsOnlineEditorTools.ClientResources.Serialization;

public interface IResourceWriter
{
    string SerializeResource(object obj, int resourceId);
}
