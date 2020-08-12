using System;
using System.Collections.Generic;
using System.Linq;
using Aggregator.Exceptions;
using Aggregator.Internal;

namespace Aggregator
{
    /// <summary>
    /// Base class for aggregate root entities based on events deriving from <see cref="object"/>.
    /// </summary>
    public abstract class AggregateRoot : AggregateRoot<object>
    {
    }

    /// <summary>
    /// Base class for aggregate root entities.
    /// </summary>
    /// <typeparam name="TEventBase">The event base type.</typeparam>
    public abstract class AggregateRoot<TEventBase>
        : IAggregateRootInitializer<TEventBase>, IAggregateRootChangeTracker<TEventBase>
    {
        private readonly Dictionary<Type, Action<TEventBase>> _handlers = new Dictionary<Type, Action<TEventBase>>();
        private readonly Queue<TEventBase> _changes = new Queue<TEventBase>();

        void IAggregateRootInitializer<TEventBase>.Initialize(IEnumerable<TEventBase> events)
        {
            if (events == null) return;
            foreach (TEventBase @event in events)
                Handle(@event);
        }

        bool IAggregateRootChangeTracker<TEventBase>.HasChanges
            => _changes.Any();

        TEventBase[] IAggregateRootChangeTracker<TEventBase>.GetChanges()
            => _changes.ToArray();

        /// <summary>
        /// Registers an event handler.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to bind the handler to.</typeparam>
        /// <param name="handler">The handler to invoke.</param>
        protected void Register<TEvent>(Action<TEvent> handler)
            where TEvent : TEventBase
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            Type eventType = typeof(TEvent);
            if (_handlers.ContainsKey(eventType))
                throw new HandlerForEventAlreadyRegisteredException(eventType);

            _handlers.Add(eventType, @event => handler((TEvent)@event));
        }

        /// <summary>
        /// Apply a new event to the aggregate root.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to apply.</typeparam>
        /// <param name="event">The event to apply.</param>
        protected void Apply<TEvent>(TEvent @event)
            where TEvent : TEventBase
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            Handle(@event);
            _changes.Enqueue(@event);
        }

        private void Handle(TEventBase @event)
        {
            Type eventType = @event.GetType();
            if (!_handlers.TryGetValue(eventType, out Action<TEventBase> handler))
                throw new UnhandledEventException(eventType);

            handler(@event);
        }
    }
}
