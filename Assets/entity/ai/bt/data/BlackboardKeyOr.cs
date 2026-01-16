using System;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Allows a BT parameter to be either a fixed value or resolved from the blackboard at runtime.
    /// This enables the same behavior tree asset to be reused with different configurations.
    /// </summary>
    [Serializable]
    public class BlackboardKeyOr<T>
    {
        /// <summary>
        /// If true, the value is read from the blackboard using blackboardKey.
        /// If false, fixedValue is used directly.
        /// </summary>
        public bool useBlackboardKey = false;

        /// <summary>
        /// The blackboard key to read when useBlackboardKey is true.
        /// </summary>
        public string blackboardKey;

        /// <summary>
        /// The fixed value to use when useBlackboardKey is false,
        /// or as a fallback if the blackboard key is not found.
        /// </summary>
        public T fixedValue;

        public BlackboardKeyOr()
        {
        }

        public BlackboardKeyOr(T fixedValue)
        {
            this.fixedValue = fixedValue;
            useBlackboardKey = false;
        }

        public BlackboardKeyOr(string blackboardKey, T fallbackValue = default)
        {
            this.blackboardKey = blackboardKey;
            this.fixedValue = fallbackValue;
            useBlackboardKey = true;
        }

        /// <summary>
        /// Gets the value, either from the blackboard or as the fixed value.
        /// Falls back to fixedValue if the blackboard key is not found.
        /// </summary>
        public T GetValue(BTContext context)
        {
            if (useBlackboardKey && !string.IsNullOrEmpty(blackboardKey))
            {
                if (context.TryGet<T>(blackboardKey, out var value))
                {
                    return value;
                }
            }
            return fixedValue;
        }

        /// <summary>
        /// Implicit conversion from T to BlackboardKeyOr for convenience.
        /// Creates a fixed-value instance.
        /// </summary>
        public static implicit operator BlackboardKeyOr<T>(T value)
        {
            return new BlackboardKeyOr<T>(value);
        }
    }
}
