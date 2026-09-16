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
            if (service is UnityEngine.Object unityObj && unityObj == null)
            {
                _services.Remove(type);
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} was destroyed but not unregistered. Auto-removing.");
                return default;
            }
            return (T)service;
        }

        Debug.LogError($"[ServiceLocator] ERROR: Cannot find service of type {type.Name}. Did you forget to register it in Awake?");
        return default;
    }

    /// <summary>
    /// Attempts to retrieve a service without logging an error if it is not registered.
    /// </summary>
    public static bool TryGet<T>(out T service)
    {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var obj))
        {
            if (obj is UnityEngine.Object unityObj && unityObj == null)
            {
                _services.Remove(type);
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} was destroyed but not unregistered. Auto-removing.");
                service = default;
                return false;
            }
            service = (T)obj;
            return true;
        }

        service = default;
        return false;
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
