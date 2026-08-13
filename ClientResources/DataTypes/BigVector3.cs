// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace AllodsOnlineEditorTools.ClientResources.DataTypes;

public struct BigVector3(int globalX, int globalY, int globalZ, float localX, float localY, float localZ)
{
    public double X = globalX * 32 + localX;
    public double Y = globalY * 32 + localY;
    public double Z = globalZ * 32 + localZ;
}
