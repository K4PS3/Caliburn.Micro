﻿using System.Collections;
using System.Collections.Generic;

namespace Caliburn.Micro;

/// <summary>
/// Interface used to define a specialized parent.
/// </summary>
/// <typeparam name="T">The type of children.</typeparam>
public interface IParent<out T> : IParent {
    /// <summary>
    ///   Gets the children.
    /// </summary>
    /// <returns>
    ///   The collection of children.
    /// </returns>
    new IEnumerable<T> GetChildren();
}