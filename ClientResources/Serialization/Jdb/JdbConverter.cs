using System.Text.Json;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;

public interface IJdbConverter : ITypeConverter
{
    object? Write(JdbStructSerializer serializer, object? value);

    object? Read(JdbStructSerializer serializer, JsonElement element, Type type);
}

public abstract class JdbConverter<T> : IJdbConverter
{
    public virtual bool CanConvert(Type type) => type == typeof(T);

    public object? Write(JdbStructSerializer serializer, object? value) => WriteValue(serializer, (T)value!);

    public object? Read(JdbStructSerializer serializer, JsonElement element, Type type) => ReadValue(serializer, element, type);

    protected abstract object? WriteValue(JdbStructSerializer serializer, T value);

    protected abstract T ReadValue(JdbStructSerializer serializer, JsonElement element, Type type);
}
