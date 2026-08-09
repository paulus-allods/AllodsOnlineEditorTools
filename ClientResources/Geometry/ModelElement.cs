namespace AllodsOnlineEditorTools.ClientResources.Geometry;

public class ModelElement
{
    public required string Name { get; set; }
    public required string MaterialName { get; set; }
    public int VertexDeclarationId { get; set; }
    public int VertexBufferOffset { get; set; }
    public required Material Material { get; set; }
    public List<GeometryFragment> Lods { get; set; } = [];
    
    public class GeometryFragment
    {
        public int VertexBufferBegin { get; set; }
        public int VertexBufferEnd { get; set; }
        public int IndexBufferBegin { get; set; }
        public int IndexBufferEnd { get; set; }
    }
}