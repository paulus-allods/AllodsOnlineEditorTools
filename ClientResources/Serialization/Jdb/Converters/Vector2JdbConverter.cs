using System.Numerics;
using System.Text.Json;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class Vector2JdbConverter : JdbConverter<Vector2>
{
    protected override object WriteValue(JdbStructSerializer serializer, Vector2 value) =>
        new Dictionary<string, object?> { ["x"] = value.X, ["y"] = value.Y };

    protected override Vector2 ReadValue(JdbStructSerializer serializer, JsonElement element, Type type) =>
        new(element.GetProperty("x").GetSingle(), element.GetProperty("y").GetSingle());
}
