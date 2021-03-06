﻿using System;
using System.Collections.Generic;
using System.Linq;
using SimpleCQRS;
using SimpleCQRS.Events;

namespace CQRSGui
{
    public class EventStore : IEventStore
    {
        private readonly IEventPublisher _publisher;

        private struct EventDescriptor
        {
            
            public readonly Event EventData;
            public readonly Guid Id;
            public readonly int Version;

            public EventDescriptor(Guid id, Event eventData, int version)
            {
                EventData = eventData;
                Version = version;
                Id = id;
            }
        }

        public EventStore(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        private readonly Dictionary<Guid, List<EventDescriptor>> _current = new Dictionary<Guid, List<EventDescriptor>>(); 
        
        public void SaveEvents(Guid aggregateId, int expectedVersion, IEnumerable<Event> events)
        {
            List<EventDescriptor> eventDescriptors;
            if(!_current.TryGetValue(aggregateId, out eventDescriptors))
            {
                eventDescriptors = new List<EventDescriptor>();
                _current.Add(aggregateId,eventDescriptors);
            }
            else if(eventDescriptors[eventDescriptors.Count - 1].Version != expectedVersion && expectedVersion != -1)
            {
                throw new ConcurrencyException();
            }
            var i = expectedVersion;
            foreach (var @event in events)
            {
                i++;

                eventDescriptors.Add(new EventDescriptor(aggregateId,@event,i));

                var metadata = new EventMetadata(i);

                _publisher.Publish(@event, metadata);
            }
        }

        public  IEnumerable<Event> GetEventsForAggregate(Guid aggregateId)
        {
            List<EventDescriptor> eventDescriptors;
            if (!_current.TryGetValue(aggregateId, out eventDescriptors))
            {
                throw new AggregateNotFoundException();
            }
            return eventDescriptors.Select(desc => desc.EventData).ToList();
        }

    }

    public class AggregateNotFoundException : Exception
    {
    }

    public class ConcurrencyException : Exception
    {
    }

}