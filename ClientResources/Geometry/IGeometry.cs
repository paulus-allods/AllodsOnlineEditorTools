using AllodsOnlineEditorTools.ClientResources.Structs.Common;

namespace AllodsOnlineEditorTools.ClientResources.Geometry;

public interface IGeometry
{
    VertexDeclaration[] GetVertexDeclaration();
    Blob GetVertexBuffer();
    Blob GetIndexBuffer();
    ModelElement[] GetModelElements();
    string GetFilePath();
}
