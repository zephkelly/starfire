using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Entity;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Provides minimap contact data from an entity's sensor module.
    /// </summary>
    public class SensorMinimapDataProvider : IMinimapDataProvider
    {
        private readonly ISensorShipModule _sensor;
        private readonly EntityControllerBase _source;
        private readonly FactionData _sourceFaction;

        private readonly List<MinimapContactData> _contacts = new();
        private float _lastUpdateTime;

        public IReadOnlyList<MinimapContactData> Contacts => _contacts;
        public float MaxRange => _sensor?.DetectionRange ?? 0f;
        public Vector2 SourcePosition => _source != null ? (Vector2)_source.transform.position : Vector2.zero;
        public float SourceRotation => _source != null ? _source.transform.eulerAngles.z : 0f;
        public bool IsActive => _sensor != null && _source != null;
        public float TimeSinceLastUpdate => Time.time - _lastUpdateTime;
        public float UpdateInterval => _sensor?.PollingRate ?? 1f;

        public event Action OnDataUpdated;

        public SensorMinimapDataProvider(
            ISensorShipModule sensor,
            EntityControllerBase source,
            FactionData sourceFaction)
        {
            _sensor = sensor;
            _source = source;
            _sourceFaction = sourceFaction;

            if (_sensor != null)
            {
                _sensor.OnEntityDetected += OnSensorEntityDetected;
                _sensor.OnEntityLost += OnSensorEntityLost;
            }

            RefreshContacts();
        }

        public void Refresh()
        {
            RefreshContacts();
        }

        public void Dispose()
        {
            if (_sensor != null)
            {
                _sensor.OnEntityDetected -= OnSensorEntityDetected;
                _sensor.OnEntityLost -= OnSensorEntityLost;
            }
        }

        private void OnSensorEntityDetected(DetectedEntity entity)
        {
            RefreshContacts();
        }

        private void OnSensorEntityLost(DetectedEntity entity)
        {
            RefreshContacts();
        }

        private void RefreshContacts()
        {
            _contacts.Clear();

            if (_sensor == null || _source == null) return;

            Vector2 sourcePos = SourcePosition;
            float maxRange = MaxRange;

            foreach (var detected in _sensor.DetectedEntities)
            {
                if (!detected.IsValid) continue;

                var contact = new MinimapContactData(
                    detected,
                    sourcePos,
                    maxRange,
                    _sourceFaction);

                _contacts.Add(contact);
            }

            _lastUpdateTime = Time.time;
            OnDataUpdated?.Invoke();
        }
    }
}
