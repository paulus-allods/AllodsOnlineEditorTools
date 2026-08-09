using System.Numerics;
using System.Text.Json;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class QuaternionJdbConverter : JdbConverter<Quaternion>
{
    protected override object WriteValue(JdbStructSerializer serializer, Quaternion value)
        => new Dictionary<string, object?> { ["x"] = value.X, ["y"] = value.Y, ["z"] = value.Z, ["w"] = value.W };

    protected override Quaternion ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
        => new(element.GetProperty("x").GetSingle(),
            element.GetProperty("y").GetSingle(),
            element.GetProperty("z").GetSingle(),
            element.GetProperty("w").GetSingle());
}
