namespace Messaging.Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RoutingKeyAttribute(string key) : System.Attribute
{
    public string Key { get; } = key;
}
