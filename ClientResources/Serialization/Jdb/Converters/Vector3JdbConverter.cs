using System.Numerics;
using System.Text.Json;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class Vector3JdbConverter : JdbConverter<Vector3>
{
    protected override object WriteValue(JdbStructSerializer serializer, Vector3 value) =>
        new Dictionary<string, object?> { ["x"] = value.X, ["y"] = value.Y, ["z"] = value.Z };

    protected override Vector3 ReadValue(JdbStructSerializer serializer, JsonElement element, Type type) => new(element.GetProperty("x").GetSingle(),
        element.GetProperty("y").GetSingle(), element.GetProperty("z").GetSingle());
}
