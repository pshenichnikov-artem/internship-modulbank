using System.Reflection;
using Messaging.Attribute;
using Messaging.Events;

namespace Messaging.Constants;

public static class RoutingKeys
{
    private static readonly Lazy<HashSet<string>> ValidKeys = new(() =>
    {
        var eventTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEvent)) && !t.IsInterface)
            .ToList();

        var keys = new HashSet<string>();


        //ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var eventType in eventTypes)
        {
            var routingKeyAttr = eventType.GetCustomAttribute<RoutingKeyAttribute>();
            if (routingKeyAttr != null) keys.Add(routingKeyAttr.Key);
        }

        return keys;
    });

    public static bool IsValid(string routingKey)
    {
        return ValidKeys.Value.Contains(routingKey);
    }
}