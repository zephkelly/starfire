using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Interface for providing contact data to the minimap.
    /// Allows different data sources (sensors, debug all-entities, etc.).
    /// </summary>
    public interface IMinimapDataProvider
    {
        /// <summary>
        /// Current contacts to display on minimap.
        /// </summary>
        IReadOnlyList<MinimapContactData> Contacts { get; }

        /// <summary>
        /// Maximum range of the data source (e.g., sensor detection range).
        /// </summary>
        float MaxRange { get; }

        /// <summary>
        /// Position of the data source center (typically player position).
        /// </summary>
        Vector2 SourcePosition { get; }

        /// <summary>
        /// Forward direction angle of the source in degrees (for ship-up orientation).
        /// </summary>
        float SourceRotation { get; }

        /// <summary>
        /// Whether the provider has valid data and is ready.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Time since last data refresh in seconds.
        /// </summary>
        float TimeSinceLastUpdate { get; }

        /// <summary>
        /// Expected interval between updates in seconds.
        /// </summary>
        float UpdateInterval { get; }

        /// <summary>
        /// Fired when contact data is updated.
        /// </summary>
        event Action OnDataUpdated;

        /// <summary>
        /// Force an immediate refresh of contact data.
        /// </summary>
        void Refresh();
    }
}
