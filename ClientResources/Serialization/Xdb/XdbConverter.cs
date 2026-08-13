using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

public interface IXdbConverter : ITypeConverter
{
    XElement? Write(XdbStructSerializer serializer, string elementName, object? value);

    object? Read(XdbStructSerializer serializer, XElement element, Type type);
}

public abstract class XdbConverter<T> : IXdbConverter
{
    public virtual bool CanConvert(Type type) => type == typeof(T);

    public XElement? Write(XdbStructSerializer serializer, string elementName, object? value)
        => WriteValue(serializer, elementName, (T)value!);

    protected abstract XElement? WriteValue(XdbStructSerializer serializer, string elementName, T value);

    public object? Read(XdbStructSerializer serializer, XElement element, Type type)
        => ReadValue(serializer, element, type);

    protected abstract T ReadValue(XdbStructSerializer serializer, XElement element, Type type);
}
