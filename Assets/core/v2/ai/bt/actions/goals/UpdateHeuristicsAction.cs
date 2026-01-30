
namespace StarfireV2
{

    /// <summary>
    /// Calculates ship state heuristics and writes them to blackboard.
    /// Should run early in the BT, before goal evaluation.
    /// Always returns Success.
    /// </summary>
    public class UpdateHeuristicsAction : BTAction
    {
        private readonly bool _writeIndividualKeys;

        public UpdateHeuristicsAction(bool writeIndividualKeys = true)
        {
            _writeIndividualKeys = writeIndividualKeys;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            var modules = Context.Modules;
            if (modules == null)
            {
                UnityEngine.Debug.LogWarning($"[UpdateHeuristics] FAILURE: Context.Modules is null. " +
                    $"Controller.Entity type={(Context.Controller?.Entity?.GetType().Name ?? "null")}");
                return BTNodeStatus.Failure;
            }

            var heuristics = ShipHeuristics.Calculate(modules, Context);

            // Write full struct for goal evaluation
            Context.Set(HeuristicKeys.HeuristicData, heuristics);

            // Write individual values for BT conditions
            if (_writeIndividualKeys)
            {
                // Health/Defense
                Context.Set(HeuristicKeys.HullPercent, heuristics.HullPercent);
                Context.Set(HeuristicKeys.ShieldPercent, heuristics.ShieldPercent);
                Context.Set(HeuristicKeys.OverallDefense, heuristics.OverallDefense);

                // Derived States
                Context.Set(HeuristicKeys.Confidence, heuristics.Confidence);
                Context.Set(HeuristicKeys.Skittishness, heuristics.Skittishness);
                Context.Set(HeuristicKeys.Vulnerability, heuristics.Vulnerability);

                // Threat Assessment
                Context.Set(HeuristicKeys.ThreatLevel, heuristics.ThreatLevel);
                Context.Set(HeuristicKeys.NearbyHostileCount, heuristics.NearbyHostileCount);
                Context.Set(HeuristicKeys.ClosestThreatDistance, heuristics.ClosestThreatDistance);

                // Detection/Awareness
                Context.Set(HeuristicKeys.UnidentifiedContactCount, heuristics.UnidentifiedContactCount);
                Context.Set(HeuristicKeys.HasUnidentifiedContacts, heuristics.HasUnidentifiedContacts);
                Context.Set(HeuristicKeys.ClosestUnidentifiedDistance, heuristics.ClosestUnidentifiedDistance);

                // Situational
                Context.Set(HeuristicKeys.HasTarget, heuristics.HasTarget);
                Context.Set(HeuristicKeys.IsInCombat, heuristics.IsInCombat);

                // Propulsion Capabilities
                Context.Set(HeuristicKeys.MaxSpeed, heuristics.MaxSpeed);
                Context.Set(HeuristicKeys.MaxAcceleration, heuristics.MaxAcceleration);
                Context.Set(HeuristicKeys.HasPropulsionModule, heuristics.HasPropulsionModule);

                // Sensor Capabilities
                Context.Set(HeuristicKeys.SensorRange, heuristics.SensorRange);
                Context.Set(HeuristicKeys.SilhouetteRange, heuristics.SilhouetteRange);
                Context.Set(HeuristicKeys.HasSensorModule, heuristics.HasSensorModule);

                // Module Flags
                Context.Set(HeuristicKeys.HasTransponderModule, heuristics.HasTransponderModule);
                Context.Set(HeuristicKeys.HasWeaponModules, heuristics.HasWeaponModules);
                Context.Set(HeuristicKeys.HasShieldModule, heuristics.HasShieldModule);
            }

            return BTNodeStatus.Success;
        }
    }
}
