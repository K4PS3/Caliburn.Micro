﻿using System;

namespace Caliburn.Micro;

/// <summary>
/// A result that executes a <see cref="System.Func&lt;TResult&gt;"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public class DelegateResult<TResult> : IResult<TResult> {
    private readonly Func<TResult> _toExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateResult&lt;TResult&gt;"/> class.
    /// </summary>
    /// <param name="action">The action.</param>
    public DelegateResult(Func<TResult> action)
        => _toExecute = action;

    /// <summary>
    /// Occurs when execution has completed.
    /// </summary>
    public event EventHandler<ResultCompletionEventArgs> Completed
        = (sender, e) => { };

    /// <summary>
    /// Gets the result.
    /// </summary>
    public TResult Result { get; private set; }

    /// <summary>
    /// Executes the result using the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    public void Execute(CoroutineExecutionContext context) {
        var eventArgs = new ResultCompletionEventArgs();
        try {
            Result = _toExecute();
        } catch (Exception ex) {
            eventArgs.Error = ex;
        }

        Completed(this, eventArgs);
    }
}