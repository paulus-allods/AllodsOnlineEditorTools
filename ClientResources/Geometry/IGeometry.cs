using AllodsOnlineEditorTools.ClientResources.Structs.Common;

namespace AllodsOnlineEditorTools.ClientResources.Geometry;

public interface IGeometry
{
    public VertexDeclaration[] GetVertexDeclaration();
    public Blob GetVertexBuffer();
    public Blob GetIndexBuffer();
    public ModelElement[] GetModelElements();
    public string GetFilePath();
}