using System;
using UnityEngine;

namespace Starfire.Core
{
    /// <summary>
    /// Attribute for displaying float fields with higher decimal precision in the Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HighPrecisionAttribute : PropertyAttribute
    {
        /// <summary>
        /// Number of decimal places to display.
        /// </summary>
        public int DecimalPlaces { get; }

        /// <summary>
        /// Minimum allowed value.
        /// </summary>
        public float MinValue { get; }

        /// <summary>
        /// Creates a high precision float attribute.
        /// </summary>
        /// <param name="decimalPlaces">Number of decimal places to display (default: 5)</param>
        /// <param name="minValue">Minimum allowed value (default: 0)</param>
        public HighPrecisionAttribute(int decimalPlaces = 5, float minValue = 0f)
        {
            DecimalPlaces = decimalPlaces;
            MinValue = minValue;
        }
    }
}
