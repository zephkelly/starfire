# Starfire AI System - Handoff Document

## Overview

Starfire is a top-down space shooter using Unity3D. The AI system uses a **Goal-Oriented Behavior Tree** architecture with a formalized **Perception Layer** pattern.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           EXTERNAL WORLD                                    │
│  ShipSystems, Sensors, Hull, Shield, Propulsion, Transponder, Weapons      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PERCEPTION LAYER (ShipHeuristics)                        │
│  - Queries raw data from all ship systems ONCE per frame                    │
│  - Normalizes values to usable ranges (0-1 for health, etc.)               │
│  - Derives meaningful metrics (Confidence, Skittishness, ThreatLevel)      │
│  - Single snapshot via UpdateHeuristicsAction at BT start                   │
│                                                                             │
│  Output: HeuristicData struct + individual blackboard keys                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
┌──────────────────────────────┐    ┌──────────────────────────────┐
│     HeuristicData Struct     │    │     Blackboard Keys          │
│  (for Goal.Evaluate())       │    │  (for BT conditions/actions) │
│                              │    │                              │
│  - HullPercent, ShieldPercent│    │  - heuristic_hull_percent    │
│  - Confidence, Vulnerability │    │  - heuristic_max_speed       │
│  - MaxSpeed, MaxAcceleration │    │  - heuristic_has_propulsion  │
│  - SilhouetteRange           │    │  - heuristic_silhouette_range│
│  - OwnFaction                │    │  - etc.                      │
└──────────────────────────────┘    └──────────────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         GOAL SYSTEM                                         │
│  - Goals score themselves using HeuristicData (0.0 - 1.0+)                 │
│  - EvaluateGoalsAction picks highest-scoring goal                          │
│  - Hysteresis threshold (0.2) prevents rapid switching                     │
│                                                                             │
│  Goal Types: Patrol(2), Investigate(3), Combat(4), Flee(5), etc.           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    GOAL-SPECIFIC BEHAVIOR TREES                             │
│                                                                             │
│  Master BT (BasicAICoreScoutTree):                                         │
│    Repeater                                                                 │
│      Sequence                                                               │
│        → UpdateHeuristicsAction    (perception snapshot)                   │
│        → EvaluateGoalsAction       (goal scoring)                          │
│        → GoalSelector              (runs active goal's BT)                 │
│            [Patrol]     → PatrolBehaviorBasic subtree                      │
│            [Investigate]→ InvestigateBehaviour subtree                     │
│            [Combat]     → (future)                                         │
│            [Flee]       → (future)                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ACTION LAYER                                        │
│  - Steering actions calculate forces from blackboard values                │
│  - ApplySteeringAction writes DesiredAcceleration to Driver                │
│  - Rotation actions write AimPosition                                       │
│  - ONLY layer that mutates ship state                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Key Files & Locations

### Perception Layer
| File | Purpose |
|------|---------|
| `Assets/entity/ai/heuristics/HeuristicData.cs` | Struct containing all computed heuristics |
| `Assets/entity/ai/heuristics/ShipHeuristics.cs` | Static calculator - queries systems, populates HeuristicData |
| `Assets/entity/ai/heuristics/HeuristicKeys.cs` | Blackboard key constants |
| `Assets/entity/ai/bt/actions/goals/UpdateHeuristicsAction.cs` | BT action that runs ShipHeuristics and writes to blackboard |

### Goal System
| File | Purpose |
|------|---------|
| `Assets/entity/ai/goals/Goal.cs` | Base class for all goals |
| `Assets/entity/ai/goals/GoalType.cs` | Enum: None, Idle, Patrol, Investigate, Combat, Flee, Escort, Guard |
| `Assets/entity/ai/goals/PatrolGoal.cs` | Scores high when no threats, healthy |
| `Assets/entity/ai/goals/InvestigateGoal.cs` | Scores high when unidentified contacts detected |
| `Assets/entity/ai/goals/GoalParameters.cs` | Base parameters for goal configuration |
| `Assets/entity/ai/bt/actions/goals/EvaluateGoalsAction.cs` | Scores all goals, picks winner |

### Behavior Trees
| File | Purpose |
|------|---------|
| `Assets/entity/ship/class/scout/modules/aicore/BasicAICoreScoutTree.asset` | Master BT for scout ships |
| `Assets/entity/ai/bt/trees/ship/behaviors/PatrolBehaviorBasic.asset` | Patrol goal subtree |
| `Assets/entity/ai/bt/trees/ship/behaviors/InvestigateBehaviour.asset` | Investigate goal subtree |

### Steering Actions (Now using Perception Layer)
| File | Purpose |
|------|---------|
| `Assets/entity/ai/bt/actions/steering/CalculateSmartArriveAction.cs` | Optimal arrival with deceleration |
| `Assets/entity/ai/bt/actions/steering/CalculateSeekAction.cs` | Pure pursuit |
| `Assets/entity/ai/bt/actions/steering/CalculateFleeAction.cs` | Escape from target |
| `Assets/entity/ai/bt/actions/steering/CalculateArriveAction.cs` | Basic arrival |
| `Assets/entity/ai/bt/actions/steering/CalculateFlyThroughAction.cs` | Pass through waypoints |
| `Assets/entity/ai/bt/actions/steering/CalculateBrakeAndTurnAction.cs` | Brake while rotating |
| `Assets/entity/ai/bt/actions/steering/CalculateRecoveryAction.cs` | Overshoot recovery |
| `Assets/entity/ai/bt/actions/steering/SetDynamicCruiseSpeedAction.cs` | Dynamic speed limits |
| `Assets/entity/ai/bt/actions/steering/CalculateMaintainDistanceAction.cs` | Hold at sensor range |
| `Assets/entity/ai/bt/actions/steering/SetHoldingPositionAction.cs` | Calculate holding position |
| `Assets/entity/ai/bt/actions/steering/ApplySteeringAction.cs` | Writes to Driver (MUTATION) |

### Steering Context
| File | Purpose |
|------|---------|
| `Assets/entity/ai/steering/SteeringContext.cs` | Contains `FromBlackboard()` method that reads heuristics |

### Detection System
| File | Purpose |
|------|---------|
| `Assets/entity/modules/categories/sensors/sensor/DetectedEntity.cs` | Entity detected by sensors |
| `Assets/entity/modules/categories/sensors/sensor/DetectionLevel.cs` | Enum: None, Presence, Silhouette, Full |

---

## HeuristicData Fields

```csharp
public struct HeuristicData
{
    // Health/Defense (0-1 normalized)
    public float HullPercent;
    public float ShieldPercent;
    public float OverallDefense;

    // Derived States (0-1)
    public float Confidence;        // High when healthy
    public float Skittishness;      // High when damaged
    public float Vulnerability;     // High when shields down but hull OK

    // Threat Assessment
    public int NearbyHostileCount;
    public float ClosestThreatDistance;
    public float ThreatLevel;       // Combined threat metric

    // Detection/Awareness
    public int UnidentifiedContactCount;
    public bool HasUnidentifiedContacts;
    public float ClosestUnidentifiedDistance;

    // Situational
    public bool HasTarget;
    public bool IsInCombat;

    // Propulsion Capabilities
    public float MaxSpeed;
    public float MaxAcceleration;
    public bool HasPropulsionModule;

    // Sensor Capabilities
    public float SensorRange;
    public float SilhouetteRange;
    public bool HasSensorModule;

    // Module Capabilities
    public bool HasTransponderModule;
    public bool HasWeaponModules;
    public bool HasShieldModule;

    // Faction/Identity
    public FactionData OwnFaction;
}
```

---

## Goal Scoring Examples

**PatrolGoal** scores ~1.0 when:
- No unidentified contacts
- No nearby hostiles
- Ship is healthy

**InvestigateGoal** scores ~0.7-1.0 when:
- `HasUnidentifiedContacts == true`
- More contacts = higher score
- Closer contacts = higher score
- Reduced if damaged or under threat

**Goal Selection**: EvaluateGoalsAction picks highest score with 0.2 hysteresis threshold to prevent rapid switching.

---

## Detection Levels

```
None       (0) → No information
Presence   (1) → Something is there
Silhouette (2) → Can see shape, faction data available
Full       (3) → Complete identification
```

Investigation goal completes when target reaches configured detection level (default: Silhouette).

---

## Blackboard Keys (Important Ones)

```csharp
// Heuristics (written by UpdateHeuristicsAction)
"heuristics"                          // Full HeuristicData struct
"heuristic_hull_percent"              // float 0-1
"heuristic_max_speed"                 // float
"heuristic_max_acceleration"          // float
"heuristic_has_propulsion"            // bool
"heuristic_silhouette_range"          // float
"heuristic_has_sensor"                // bool
"heuristic_has_unidentified"          // bool
"heuristic_unidentified_count"        // int

// Steering (written by various actions)
"steering_target"                     // Vector2 position
"steering_force"                      // Vector2 calculated force
"cruise_speed"                        // float speed limit
"cruise_acceleration"                 // float accel limit

// Investigation
"investigation_target"                // DetectedEntity
"monitored_target"                    // DetectedEntity being watched

// Goals
"active_goal"                         // GoalType enum
"active_goal_index"                   // int

// Waypoints
"waypoint_stack"                      // WaypointStackState
```

---

## What Was Just Completed

### Perception Layer Migration (All 7 Phases Complete)

**Problem Solved**: BT actions were querying ShipSystems directly, causing:
- Multiple redundant queries per frame
- Inconsistent data between actions
- Tight coupling to system implementation

**Solution**: Formalized ShipHeuristics as the canonical perception layer:

1. **Extended HeuristicData** with propulsion, sensor, and module capability fields
2. **Added CalculateCapabilities()** to ShipHeuristics
3. **Added HeuristicKeys** for all new fields
4. **Updated UpdateHeuristicsAction** to write all values to blackboard
5. **Created SteeringContext.FromBlackboard()** that reads from heuristics instead of systems
6. **Migrated 10 steering actions** to use blackboard pattern
7. **Migrated IsTargetHostileCondition** to read OwnFaction from heuristics

**Files Modified**:
- `HeuristicData.cs` - Added 10+ new fields
- `ShipHeuristics.cs` - Added CalculateCapabilities method
- `HeuristicKeys.cs` - Added 12 new key constants
- `UpdateHeuristicsAction.cs` - Writes all new keys
- `SteeringContext.cs` - Added FromBlackboard() method
- 8 steering Calculate* actions
- 2 sensor-dependent actions (CalculateMaintainDistance, SetHoldingPosition)
- `IsTargetHostileCondition.cs`

---

## Next Steps (Recommended)

### 1. State Machine of Behavior Trees (Primary Architecture Goal)
The user wants a **state machine where each state is a complete behavior tree**:

```
┌─────────────────────────────────────────────────────────────────┐
│                    GOAL SYSTEM (State Machine)                  │
│                                                                 │
│   ┌─────────┐    ┌─────────────┐    ┌────────┐    ┌──────┐    │
│   │ PATROL  │───▶│ INVESTIGATE │───▶│ COMBAT │───▶│ FLEE │    │
│   │  State  │    │    State    │    │ State  │    │State │    │
│   └────┬────┘    └──────┬──────┘    └───┬────┘    └──┬───┘    │
│        │                │               │            │         │
│        ▼                ▼               ▼            ▼         │
│   ┌─────────┐    ┌─────────────┐    ┌────────┐    ┌──────┐    │
│   │PatrolBT │    │InvestigateBT│    │CombatBT│    │FleeBT│    │
│   │ (full)  │    │   (full)    │    │ (full) │    │(full)│    │
│   └─────────┘    └─────────────┘    └────────┘    └──────┘    │
└─────────────────────────────────────────────────────────────────┘
```

**Key Benefits:**
- Each goal = one state = one focused behavior tree
- BTs can be arbitrarily complex without polluting other behaviors
- Goal system handles state transitions via scoring (utility-based)
- Clean separation: goals decide WHAT to do, BTs decide HOW to do it
- Easy to add new behaviors: create Goal + BT pair

**Currently Implemented:**
- ✅ PatrolBehaviorBasic.asset (Patrol state BT)
- ✅ InvestigateBehaviour.asset (Investigate state BT)
- ❌ CombatBehavior.asset needed
- ❌ FleeBehavior.asset needed

### 2. Implement Combat Goal
- Create `CombatGoal.cs` that scores high when hostile identified at Full detection
- Create `CombatBehavior.asset` subtree
- Add weapon selection, target prioritization, attack patterns

### 3. Implement Flee Goal
- Create `FleeGoal.cs` that scores high when heavily damaged + threats nearby
- Create `FleeBehavior.asset` subtree
- Escape vector calculation, evasive maneuvers

### 4. Goal Transitions & Cleanup
- Ensure blackboard state is properly cleaned up between goal switches
- Add "on goal exit" cleanup actions if needed

### 5. Additional Heuristics (Optional)
Consider adding to HeuristicData:
- `AmmoPercent` - for combat decisions
- `FuelPercent` - for long-range planning
- `AlliesNearbyCount` - for group tactics
- `CoverAvailable` - for tactical positioning

### 6. Testing & Verification
- Play mode test: Verify goal switching works (patrol → investigate on contact)
- Verify steering still works after perception layer migration
- Profile to confirm reduced per-frame system queries

---

## Code Patterns to Follow

### Reading from Perception Layer (Correct)
```csharp
// Check capability
if (!Context.TryGet<bool>(HeuristicKeys.HasPropulsionModule, out var hasPropulsion) || !hasPropulsion)
    return BTNodeStatus.Failure;

// Get values
Context.TryGet<float>(HeuristicKeys.MaxSpeed, out var maxSpeed);
Context.TryGet<float>(HeuristicKeys.MaxAcceleration, out var maxAccel);

// Build steering context from blackboard
var ctx = SteeringContext.FromBlackboard(Context.Controller, Context);
```

### Writing to Blackboard (Correct)
```csharp
Context.Set("steering_force", calculatedForce);
Context.Set("steering_target", targetPosition);
```

### Goal Evaluation Pattern
```csharp
public override float Evaluate(HeuristicData heuristics, BTContext context)
{
    float score = 0.1f; // Base score

    if (heuristics.HasUnidentifiedContacts)
    {
        score = 0.7f;
        score += Mathf.Clamp01(heuristics.UnidentifiedContactCount * 0.1f);
    }

    // Reduce score under threat
    if (heuristics.ThreatLevel > 0.7f)
        score *= 0.3f;

    return score;
}
```

---

## Important GUIDs (for BT Asset References)

```
PatrolBehaviorBasic:    7804e3eb84f722648bc8c3ba704a59a6
InvestigateBehaviour:   9d86f1aa125037d4aa115e11cc77fb17
BasicAICoreScoutTree:   (check the asset file)
```

---

## Questions for New Session

If continuing this work, clarify:
1. Which goal to implement next (Combat or Flee)?
2. Any specific combat behaviors needed (kiting, aggressive, defensive)?
3. Should flee goal seek allies or just maximize distance?
4. Any ship class variations needed (scout vs fighter vs capital)?
