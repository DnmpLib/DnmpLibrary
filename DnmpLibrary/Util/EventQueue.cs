using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NLog;

namespace DnmpLibrary.Util
{
    public static class EventQueue
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private class EventQueueEvent : IComparable<EventQueueEvent>
        {
            public readonly TimerCallback Action;
            public readonly DateTime CallTime;
            public readonly Guid Guid;
            public readonly object State;

            public EventQueueEvent(TimerCallback action, object state, DateTime callTime, Guid guid)
            {
                Action = action;
                CallTime = callTime;
                State = state;
                Guid = guid;
            }

            public int CompareTo(EventQueueEvent other)
            {
                if (Guid == other.Guid)
                    return 0;
                return CallTime == other.CallTime ? (Guid.GetHashCode() < other.Guid.GetHashCode() ? -1 : 1) : (CallTime < other.CallTime ? -1 : 1);
            }
        }

        private static readonly SortedSet<EventQueueEvent> events = new SortedSet<EventQueueEvent>();
        private static readonly ConcurrentDictionary<Guid, EventQueueEvent> eventById = new ConcurrentDictionary<Guid, EventQueueEvent>();

        static EventQueue()
        {
            var worker = new Thread(() =>
            {
                for (; ; )
                {
                    EventQueueEvent minEvent;
                    lock (events)
                        minEvent = events.Any() ? events.Min : null;
                    if (minEvent != null && minEvent.CallTime <= DateTime.Now)
                    {
                        lock (events)
                            if (!events.Remove(minEvent))
                                logger.Fatal("EventQueue failed removing element");
                        eventById.TryRemove(minEvent.Guid, out var _);
                        minEvent.Action.BeginInvoke(minEvent.State, ActionInvokeCallback, new InvokeStateObject
                        {
                            TimerCallback = minEvent.Action
                        });
                    }
                    Thread.Sleep(1);
                }
                // ReSharper disable once FunctionNeverReturns
            });
            worker.Start();
        }

        public static Guid AddEvent(TimerCallback action, object state, DateTime time)
        {
            return AddEvent(action, state, time, Guid.NewGuid());
        }

        public static Guid AddEvent(TimerCallback action, object state, DateTime callTime, Guid newId)
        {
            var newEvent = new EventQueueEvent(action, state, callTime, newId);
            eventById.TryAdd(newId, newEvent);
            lock (events)
            {
                events.Add(newEvent);
            }
            return newId;
        }

        public static void RemoveEvent(Guid id)
        {
            if (!eventById.ContainsKey(id))
                return;
            eventById.TryRemove(id, out var idEvent);
            lock (events)
            {
                events.Remove(idEvent);
            }
        }

        private class InvokeStateObject
        {
            public TimerCallback TimerCallback;
        }

        private static void ActionInvokeCallback(IAsyncResult asyncResult)
        {
            ((InvokeStateObject)asyncResult.AsyncState).TimerCallback.EndInvoke(asyncResult);
        }
    }
}
