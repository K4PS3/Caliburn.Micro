﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro;

/// <inheritdoc />
public class EventAggregator : IEventAggregator {
    private readonly List<Handler> _handlers = new();

    /// <inheritdoc />
    public virtual bool HandlerExistsFor(Type messageType) {
        lock (_handlers) {
            return _handlers.Any(handler => handler.Handles(messageType) && !handler.IsDead);
        }
    }

    /// <inheritdoc />
    public virtual void Subscribe(object subscriber, Func<Func<Task>, Task> marshal) {
        if (subscriber == null) {
            throw new ArgumentNullException(nameof(subscriber));
        }

        if (marshal == null) {
            throw new ArgumentNullException(nameof(marshal));
        }

        lock (_handlers) {
            if (_handlers.Any(x => x.Matches(subscriber))) {
                return;
            }

            _handlers.Add(new Handler(subscriber, marshal));
        }
    }

    /// <inheritdoc />
    public virtual void Unsubscribe(object subscriber) {
        if (subscriber == null) {
            throw new ArgumentNullException(nameof(subscriber));
        }

        lock (_handlers) {
            Handler found = _handlers.FirstOrDefault(x => x.Matches(subscriber));
            if (found != null) {
                _handlers.Remove(found);
            }
        }
    }

    /// <inheritdoc />
    public virtual Task PublishAsync(object message, Func<Func<Task>, Task> marshal, CancellationToken cancellationToken = default) {
        if (message == null) {
            throw new ArgumentNullException(nameof(message));
        }

        if (marshal == null) {
            throw new ArgumentNullException(nameof(marshal));
        }

        Handler[] toNotify;

        lock (_handlers) {
            toNotify = _handlers.ToArray();
        }

        return marshal(async () => {
            Type messageType = message.GetType();

            IEnumerable<Task> tasks = toNotify.Select(h => h.Handle(messageType, message, cancellationToken));

            await Task.WhenAll(tasks);

            var dead = toNotify.Where(h => h.IsDead).ToList();

            if (dead.Any()) {
                lock (_handlers) {
                    dead.Apply(x => _handlers.Remove(x));
                }
            }
        });
    }

    private sealed class Handler {
        private readonly Func<Func<Task>, Task> _marshal;
        private readonly WeakReference _reference;
        private readonly Dictionary<Type, MethodInfo> _supportedHandlers = new();

        public Handler(object handler, Func<Func<Task>, Task> marshal) {
            _marshal = marshal;
            _reference = new WeakReference(handler);

            IEnumerable<Type> interfaces
                = handler
                    .GetType()
                    .GetTypeInfo().ImplementedInterfaces
                    .Where(x => x.GetTypeInfo().IsGenericType &&
                                x.GetGenericTypeDefinition() == typeof(IHandle<>));
            foreach (Type @interface in interfaces) {
                Type type = @interface.GetTypeInfo().GenericTypeArguments[0];
                MethodInfo method = @interface.GetRuntimeMethod("HandleAsync", new[] { type, typeof(CancellationToken) });
                if (method == null) {
                    continue;
                }

                _supportedHandlers[type] = method;
            }
        }

        public bool IsDead
            => _reference.Target == null;

        public bool Matches(object instance)
            => _reference.Target == instance;

        public bool Handles(Type messageType)
            => _supportedHandlers.Any(pair => pair.Key.GetTypeInfo().IsAssignableFrom(messageType.GetTypeInfo()));

        public Task Handle(Type messageType, object message, CancellationToken cancellationToken) {
            object target = _reference.Target;

            return target == null
                ? Task.FromResult(false)
                : _marshal(() => {
                    var tasks = _supportedHandlers
                        .Where(handler => handler.Key.GetTypeInfo().IsAssignableFrom(messageType.GetTypeInfo()))
                        .Select(pair => pair.Value.Invoke(target, new[] { message, cancellationToken }))
                        .Select(result => (Task)result)
                        .ToList();

                    return Task.WhenAll(tasks);
                });
        }
    }
}