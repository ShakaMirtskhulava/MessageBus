namespace MessageBus.Extensions;

public static class GenericTypeExtensions
{
    public static string GetGenericTypeName(this Type type)
        => type.IsGenericType ? GetGenericName(type) : type.Name;

    public static string GetGenericTypeName(this object @object)
        => @object.GetType().GetGenericTypeName();

    private static string GetGenericName(Type type)
    {
        var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
        return $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
    }
}
