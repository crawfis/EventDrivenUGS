using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CrawfisSoftware.Events
{
    /// <summary>
    /// Editor debugging aid. Accumulates a text log of the events named in the inspector list so
    /// a run can be read back from the Inspector without opening the console.
    ///    Dependencies: EventsPublisher.Instance
    ///    Subscribes: every event of every domain, filtered to _eventsInterestedIn
    ///    Publishes: nothing
    /// </summary>
    internal class EventHistory : MonoBehaviour
    {
        [SerializeField] private List<string> _eventsInterestedIn = new List<string>();
        [SerializeField][TextArea(5, 100)] private string _events;
        private StringBuilder _eventsBuilder = new StringBuilder();
        private HashSet<string> _interestedEventsHashSet = new HashSet<string>();

        private void Awake()
        {
            EventsPublisher.Instance.SubscribeToAllEvents(OnEventPublished);
            foreach (string eventName in _eventsInterestedIn)
            {
                _interestedEventsHashSet.Add(eventName);
            }
        }

        private void OnDestroy()
        {
            // EventsPublisher.Instance outlives every scene, so a handler left subscribed here
            // keeps running on a destroyed component for the rest of the process - and silently,
            // because the handler touches only managed fields and so never raises a
            // MissingReferenceException to point at itself.
            if (EventsPublisher.Instance != null)
                EventsPublisher.Instance.UnsubscribeToAllEvents(OnEventPublished);
        }

        private void OnEventPublished(string eventName, object sender, object data)
        {
            if (!_interestedEventsHashSet.Contains(eventName)) return;
            _eventsBuilder.AppendLine($"{eventName}: {data?.ToString()}");
            _events = _eventsBuilder.ToString();
        }
    }
}
