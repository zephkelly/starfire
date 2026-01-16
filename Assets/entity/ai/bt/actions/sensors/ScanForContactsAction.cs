using System.Collections.Generic;
using System.Linq;
using Starfire.Entity.Modules.Sensor;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Queries sensor module for detected entities at a minimum detection level.
    /// Excludes entities that have already been identified.
    /// Returns Success if contacts found, Failure if none.
    /// </summary>
    public class ScanForContactsAction : BTAction
    {
        private readonly DetectionLevel _minDetectionLevel;
        private readonly int _maxResults;
        private readonly string _outputKey;
        private readonly string _excludeIdentifiedKey;

        public ScanForContactsAction(
            DetectionLevel minDetectionLevel = DetectionLevel.Presence,
            int maxResults = 10,
            string outputKey = "sensor_contacts",
            string excludeIdentifiedKey = "identified_entities")
        {
            _minDetectionLevel = minDetectionLevel;
            _maxResults = maxResults;
            _outputKey = outputKey;
            _excludeIdentifiedKey = excludeIdentifiedKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            var sensor = Context.Systems?.GetAllModulesOfType<ISensorShipModule>().FirstOrDefault();
            if (sensor == null)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] ScanForContacts: FAIL - No sensor module");
#endif
                return BTNodeStatus.Failure;
            }

            // Get identified entities to exclude
            HashSet<int> identifiedIds = null;
            if (Context.TryGet<HashSet<int>>(_excludeIdentifiedKey, out var existingSet))
            {
                identifiedIds = existingSet;
            }

            // Get entities at minimum detection level
            var contacts = sensor.GetEntitiesAtLevel(_minDetectionLevel)
                .Where(e => e.IsValid)
                .Where(e => identifiedIds == null || !identifiedIds.Contains(e.Controller.GetInstanceID()))
                .OrderBy(e => e.Distance)
                .Take(_maxResults)
                .ToList();

#if UNITY_EDITOR
            if (Context.DebugLogging)
            {
                var allDetected = sensor.DetectedEntities.Count;
                var atLevel = sensor.GetEntitiesAtLevel(_minDetectionLevel).Count();
                var identifiedCount = identifiedIds?.Count ?? 0;
                Debug.Log($"[BT:{Context.EntityName}] ScanForContacts: Sensor has {allDetected} total, {atLevel} at {_minDetectionLevel}+, {identifiedCount} identified, found {contacts.Count} unidentified");
            }
#endif

            if (contacts.Count == 0)
            {
                return BTNodeStatus.Failure;
            }

            Context.Set(_outputKey, contacts);
            return BTNodeStatus.Success;
        }
    }
}
