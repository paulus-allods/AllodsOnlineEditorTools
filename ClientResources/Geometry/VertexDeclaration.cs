namespace AllodsOnlineEditorTools.ClientResources.Geometry;

public class VertexDeclaration
{
    public int Stride { get; set; }
    public required VertexComponent Position { get; set; }
    public required VertexComponent Normal { get; set; }
    public required VertexComponent Color { get; set; }
    public required VertexComponent Texcoord0 { get; set; }
    public required VertexComponent Texcoord1 { get; set; }
    public required VertexComponent Weights { get; set; }
    public required VertexComponent Indices { get; set; }

    public class VertexComponent
    {
        public VertexElementType Type { get; set; }
        public int Offset { get; set; }
    }
}
