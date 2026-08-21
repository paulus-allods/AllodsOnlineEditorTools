using System.Text.Json;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class BigVector3JdbConverter : JdbConverter<BigVector3>
{
    protected override object WriteValue(JdbStructSerializer serializer, BigVector3 value) =>
        new Dictionary<string, object?> { ["x"] = value.X, ["y"] = value.Y, ["z"] = value.Z };

    protected override BigVector3 ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
    {
        // BigVector3's constructor takes global/local components; the resolved doubles are set directly.
        var value = default(BigVector3);
        value.X = element.GetProperty("x").GetDouble();
        value.Y = element.GetProperty("y").GetDouble();
        value.Z = element.GetProperty("z").GetDouble();
        return value;
    }
}
