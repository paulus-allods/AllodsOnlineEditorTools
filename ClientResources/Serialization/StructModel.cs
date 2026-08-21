using System.Collections.Concurrent;
using System.Reflection;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

/// <summary>
/// A public field of a serializable struct, reflected once and cached. Carries the format-neutral facts
/// every pipeline stage needs: the xdb element name (which is also the identity used as the cross-version
/// match key by <see cref="StructCaster"/>), the <see cref="EnumRefAttribute"/> enum type (if any), and the
/// binary <see cref="FieldOffsetAttribute"/> offset (null on structs that are never read from binary, e.g.
/// test fixtures).
/// </summary>
public sealed record StructField(FieldInfo Field, string XdbName, Type? EnumRef, int? Offset)
{
    public string Name => Field.Name;
    public Type FieldType => Field.FieldType;
    public Type? DeclaringType => Field.DeclaringType;

    public object? GetValue(object target) => Field.GetValue(target);
    public void SetValue(object target, object? value) => Field.SetValue(target, value);
}

/// <summary>The cached, reflected view of a serializable struct type: its xdb name and its fields.</summary>
public sealed record StructModel(Type Type, string XdbName, IReadOnlyList<StructField> Fields);

public static class StructModelCache
{
    private static readonly ConcurrentDictionary<Type, StructModel> Cache = new();

    public static StructModel Get(Type type) => Cache.GetOrAdd(type, Build);

    private static StructModel Build(Type type)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var structFields = new StructField[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            structFields[i] = new StructField(
                field,
                XdbNameAttribute.Resolve(field),
                field.GetCustomAttribute<EnumRefAttribute>()?.EnumType,
                field.GetCustomAttribute<FieldOffsetAttribute>()?.Offset);
        }

        return new StructModel(type, XdbNameAttribute.Resolve(type), structFields);
    }
}
