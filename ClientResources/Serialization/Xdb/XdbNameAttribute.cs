using System.Reflection;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
public class XdbNameAttribute(string name) : Attribute
{
    private string Name { get; } = name;

    public static string Resolve(Type type) => type.GetCustomAttribute<XdbNameAttribute>()?.Name ?? type.Name;
    public static string Resolve(FieldInfo field) => field.GetCustomAttribute<XdbNameAttribute>()?.Name ?? field.Name;
}
