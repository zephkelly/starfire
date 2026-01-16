using System;
using System.Collections.Generic;

namespace Starfire.Entity.AI.Goals
{
    /// <summary>
    /// Factory for creating runtime IGoal instances from GoalParameters.
    /// Register custom goal types to extend the factory.
    /// </summary>
    public static class GoalFactory
    {
        private static readonly Dictionary<Type, Func<GoalParameters, IGoal>> _factories = new();

        static GoalFactory()
        {
            // Register built-in goal types
            Register<PatrolGoalParameters>(p => new PatrolGoal(p));
            Register<FleeGoalParameters>(p => new FleeGoal(p));
            Register<InvestigateGoalParameters>(p => new InvestigateGoal(p));
        }

        /// <summary>
        /// Register a factory for a specific GoalParameters type.
        /// </summary>
        public static void Register<T>(Func<T, IGoal> factory) where T : GoalParameters
        {
            _factories[typeof(T)] = p => factory((T)p);
        }

        /// <summary>
        /// Create a runtime goal from parameters.
        /// Returns null if no factory is registered for the parameters type.
        /// </summary>
        public static IGoal Create(GoalParameters parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            var type = parameters.GetType();
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory(parameters);
            }

            UnityEngine.Debug.LogWarning($"GoalFactory: No factory registered for {type.Name}");
            return null;
        }

        /// <summary>
        /// Create a goal directly without parameters.
        /// </summary>
        public static IGoal CreateIdle()
        {
            return new IdleGoal();
        }
    }
}
