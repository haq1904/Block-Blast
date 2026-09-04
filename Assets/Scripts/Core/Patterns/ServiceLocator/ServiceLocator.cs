using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    /// <summary>
    /// Registers a service to the locator. Typically called in Awake.
    /// </summary>
    public static void Register<T>(T service)
    {
        var type = typeof(T);

        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} is already registered. Overwriting.");
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
        }
    }

    /// <summary>
    /// Retrieves a service from the locator. Typically called in Start or when needed.
    /// </summary>
    public static T Get<T>()
    {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var service))
        {
            return (T)service;
        }

        Debug.LogError($"[ServiceLocator] ERROR: Cannot find service of type {type.Name}. Did you forget to register it in Awake?");
        return default;
    }

    /// <summary>
    /// Unregisters a service. Called when the service owner is destroyed to free memory.
    /// </summary>
    public static void Unregister<T>()
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services.Remove(type);
        }
    }

    /// <summary>
    /// Clears all registered services. Useful when changing scenes or resetting the game.
    /// </summary>
    public static void ClearAll()
    {
        _services.Clear();
    }
}
