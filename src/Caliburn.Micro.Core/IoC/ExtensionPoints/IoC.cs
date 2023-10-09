﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Caliburn.Micro;

/// <summary>
/// Used by the framework to pull instances from an IoC container and to inject dependencies into certain existing classes.
/// </summary>
public static class IoC {
    /// <summary>
    /// Gets or sets func to get an instance by type and key.
    /// </summary>
    public static Func<Type, string, object> GetInstance { get; set; }
        = (service, key)
            => throw new InvalidOperationException("IoC is not initialized.");

    /// <summary>
    /// Gets or sets func to get all instances of a particular type.
    /// </summary>
    public static Func<Type, IEnumerable<object>> GetAllInstances { get; set; }
        = service
            => throw new InvalidOperationException("IoC is not initialized.");

    /// <summary>
    /// Gets or sets action to be passes an existing instance to the IoC container to enable dependencies to be injected.
    /// </summary>
    public static Action<object> BuildUp { get; set; }
        = instance
            => throw new InvalidOperationException("IoC is not initialized.");

    /// <summary>
    /// Gets an instance from the container.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The resolved instance.</returns>
    public static T Get<T>(string key = null)
        => (T)GetInstance(typeof(T), key);

    /// <summary>
    /// Gets all instances of a particular type.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <returns>The resolved instances.</returns>
    public static IEnumerable<T> GetAll<T>()
        => GetAllInstances(typeof(T)).Cast<T>();
}