using System.Collections.Generic;

namespace StarfireV2
{
    /// <summary>
    /// Manages goal selection and transitions for a single ship.
    /// Owned by BasicAICoreModule.
    /// </summary>
    public class GoalManager
    {
        private readonly List<IGoal> _availableGoals = new();
        private readonly GoalEvaluator _evaluator;
        private BTContext _context;
        private IGoal _currentGoal;
        private IGoal _commanderAssignedGoal;
        private float _currentGoalScore;

        /// <summary>
        /// Hysteresis threshold to prevent rapid goal switching.
        /// A new goal must score this much higher than the current to trigger a switch.
        /// </summary>
        public float SwitchThreshold { get; set; } = 0.2f;

        /// <summary>
        /// Minimum time between goal switches (in seconds).
        /// </summary>
        public float MinSwitchInterval { get; set; } = 0.5f;

        private float _lastSwitchTime;

        public IGoal CurrentGoal => _currentGoal;
        public bool HasCommanderAssignment => _commanderAssignedGoal != null;
        public IReadOnlyList<IGoal> AvailableGoals => _availableGoals;

        public GoalManager()
        {
            _evaluator = new GoalEvaluator();
        }

        /// <summary>
        /// Initialize the goal manager with a context and default goals.
        /// </summary>
        public void Initialize(BTContext context, IEnumerable<GoalParameters> defaultGoalParameters)
        {
            _context = context;

            // Create goal instances from parameters
            int nullCount = 0;
            foreach (var parameters in defaultGoalParameters)
            {
                if (parameters == null)
                {
                    nullCount++;
                    continue;
                }
                var goal = GoalFactory.Create(parameters);
                if (goal != null)
                {
                    _availableGoals.Add(goal);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[GoalManager] GoalFactory.Create returned null for {parameters.GetType().Name}");
                }
            }
            UnityEngine.Debug.Log($"[GoalManager] Initialized with {_availableGoals.Count} goals ({nullCount} null parameters skipped)");

            // Always have an idle goal as fallback
            bool hasIdleGoal = false;
            foreach (var goal in _availableGoals)
            {
                if (goal.Type == GoalType.Idle)
                {
                    hasIdleGoal = true;
                    break;
                }
            }
            if (!hasIdleGoal)
            {
                _availableGoals.Add(GoalFactory.CreateIdle());
            }

            // Store reference in blackboard for BT access
            context.Set(GoalKeys.GoalManager, this);
        }

        /// <summary>
        /// Add a goal to the available goals list.
        /// </summary>
        public void AddGoal(IGoal goal)
        {
            if (goal != null && !_availableGoals.Contains(goal))
            {
                _availableGoals.Add(goal);
            }
        }

        /// <summary>
        /// Remove a goal from the available goals list.
        /// </summary>
        public void RemoveGoal(IGoal goal)
        {
            _availableGoals.Remove(goal);

            // If this was the current goal, force re-evaluation
            if (_currentGoal == goal)
            {
                _currentGoal?.Deactivate(_context);
                _currentGoal = null;
            }
        }

        /// <summary>
        /// Assign a goal from a commander. Commander goals take precedence.
        /// </summary>
        public void AssignGoal(IGoal goal)
        {
            _commanderAssignedGoal = goal;

            if (goal != null)
            {
                goal.IsCommanderAssigned = true;

                // Add to available if not already present
                if (!_availableGoals.Contains(goal))
                {
                    _availableGoals.Add(goal);
                }

                // Immediately activate if different from current
                if (_currentGoal != goal)
                {
                    TransitionTo(goal);
                }
            }
        }

        /// <summary>
        /// Clear commander assignment, return to autonomous selection.
        /// </summary>
        public void ClearAssignment()
        {
            if (_commanderAssignedGoal != null)
            {
                _commanderAssignedGoal.IsCommanderAssigned = false;
                _commanderAssignedGoal = null;
            }
        }

        /// <summary>
        /// Evaluate all goals and potentially switch to a better one.
        /// Called by EvaluateGoalsAction in the behavior tree.
        /// </summary>
        public void EvaluateAndUpdate(HeuristicData heuristics)
        {
            if (_context == null)
            {
                return;
            }

            // Check if commander assignment completed
            if (_commanderAssignedGoal != null && _commanderAssignedGoal.IsComplete(_context))
            {
                ClearAssignment();
            }

            // Commander assignments always win (unless completed)
            if (_commanderAssignedGoal != null)
            {
                if (_currentGoal != _commanderAssignedGoal)
                {
                    TransitionTo(_commanderAssignedGoal);
                }
                return;
            }

            // Check if current goal completed
            if (_currentGoal?.IsComplete(_context) == true)
            {
                _currentGoal.Deactivate(_context);
                _currentGoal = null;
                _currentGoalScore = float.MinValue;
            }

            // Autonomous goal selection via utility scoring
            var (bestGoal, bestScore) = _evaluator.SelectBest(_availableGoals, heuristics, _context);

            // Hysteresis: only switch if significantly better or no current goal
            bool shouldSwitch = _currentGoal == null ||
                                bestScore > _currentGoalScore + SwitchThreshold;

            // Time-based cooldown
            float currentTime = UnityEngine.Time.time;
            bool cooldownElapsed = currentTime - _lastSwitchTime >= MinSwitchInterval;

            if (shouldSwitch && cooldownElapsed && bestGoal != _currentGoal)
            {
                TransitionTo(bestGoal);
                _currentGoalScore = bestScore;
            }
        }

        /// <summary>
        /// Force a specific goal to be active (bypasses evaluation).
        /// </summary>
        public void ForceGoal(IGoal goal)
        {
            if (goal != null)
            {
                TransitionTo(goal);
            }
        }

        private void TransitionTo(IGoal newGoal)
        {
            _currentGoal?.Deactivate(_context);
            _currentGoal = newGoal;
            _currentGoal?.Activate(_context);
            _lastSwitchTime = UnityEngine.Time.time;
        }
    }
}
