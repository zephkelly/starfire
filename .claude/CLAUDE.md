We are creating a top down space shooter game set in space, using Unity3D latest LTS.

Architecture
Starfire is a Unity DOTS ECS top-down space shooter using com.unity.entities 1.4.4, com.unity.physics 1.4.4, com.unity.entities.graphics 1.4.17, and com.unity.netcode 1.10.0. All gameplay code lives under Assets/scripts/ organized by concern: core/ (input), entity/ (component data), simulation/ (tier config and tags), systems/ (ECS systems by domain), authoring/ (bakers), network/ (netcode bootstrap and RPC), and demo/ (MonoBehaviour presentation layer).

The simulation uses a 5-tier LOD system that manages entity fidelity based on distance from the player. Tier 0 (Loaded, 0-5k) has full physics and rendering. Tier 1 (Active, 5k-variable per-entity boundary) has physics but no rendering. Tier 2 (Sensor, variable-100k) drops physics entirely and uses Burst IJobEntity for lightweight sensor updates. Tier 3 (Strategic, 100k-200k) collapses individual entities into fleet/field aggregate entities with DynamicBuffers. Tier 4 (Dormant, 200k+) stores entities as compact DormantRecord data with no ECS entity overhead.

Tier transitions use a reactive event pattern: TierEvaluationSystem (Burst parallel job) computes distance-based tier assignments and enables the TierTransition IEnableableComponent as a one-frame event. Multiple independent systems in systems/tier/, systems/ship/, and systems/asteroid/ react to enabled TierTransition components, filtered by entity type (e.g. WithAll<ShipHull> or WithAll<AsteroidTag>). TierTransitionCleanupSystem disables TierTransition at the end of the frame. There is no central dispatcher or handler interface — adding a new entity type means creating a new reactive system and adding an UpdateAfter dependency to TierTransitionCleanupSystem.

Three entity types exist: Ships (full T0-T4 lifecycle, ShipSnapshot for sensor-tier state preservation, AI via AIControlSystem), Asteroids (full T0-T4, orbital physics at every tier via different systems per tier, size-based tier filtering that pushes small asteroids to higher tiers sooner), and Stars (capped at Sensor tier via Persistence=2, provide radial gravity to all rich-tier entities via StarGravitySystem).

Netcode Integration
The game uses Unity Netcode for Entities in a listen-server configuration. StarfireBootstrap (extending ClientServerBootstrap) creates both ServerWorld and ClientWorld in the same process, auto-connecting on port 7979. DefaultGameObjectInjectionWorld is set to ClientWorld so MonoBehaviours (camera, minimap renderer) attach there. Ship, Asteroid, and Star prefabs are ghost prefabs created via GetEntity(TransformUsageFlags.Dynamic) in their bakers.

Systems are classified by world using WorldSystemFilter: server-only systems (19 total, including all tier evaluation, formation/disband, dormant conversion, AI, sensor batch update), client-only systems (3: PlayerInputSystem in GhostInputSystemGroup, MinimapDataSystem, AsteroidRenderingInitSystem), and predicted systems (5 in PredictedSimulationSystemGroup: ShipThrust, ShipAim, SpeedLimit, Constrain2D, StarGravity). FloatingOrigin and WorldPositionSync each have separate server and client variants.

The player lifecycle works as follows: GoInGameClientSystem (client) sends a GoInGameRequest RPC when it detects a NetworkId without NetworkStreamInGame. GoInGameSystem (server) receives this, instantiates a ship ghost prefab at origin, adds PlayerTag and GhostOwner, and links the connection via CommandTarget. ClientPlayerTagSystem (client) finds the ghost entity matching the local NetworkId and adds PlayerTag + switches it to predicted mode via GhostPredictionSwitchingQueues. The server syncs LocalTransform → WorldPosition (after physics), while the client syncs WorldPosition → LocalTransform (after ghost snapshot application). MinimapDataSystem runs in the server world (where all entity data lives) and exposes pixel buffers that MinimapRenderer (client-world MonoBehaviour) reads cross-world via World.GetExistingSystemManaged.

Key DOTS API pitfalls to be aware of: the Starfire.Entity namespace shadows Unity.Entities.Entity — always use fully qualified Unity.Entities.Entity in type positions. DisableRendering is not IEnableableComponent — add/remove via ECB. For physics exclusion, remove PhysicsWorldIndex (ISharedComponentData, re-add with ecb.AddSharedComponent). WithPresent<T>() iterates all entities including disabled — omit it for one-frame events like TierTransition so the default query only matches enabled instances.

There are a few types of entities, Ships, Stars, and Asteroids.

Before writing any plans or code, please ask me any clarifying questions about the game mechanics, features, or design that will help you better understand the requirements and please scan the codebase to understand the existing structure and components.

You are a senior software engineer with expertise in Unity3D game development, C# programming, and game architecture design and enterprise patterns. You stick to DRY principles, SOLID design principles, and best practices for Unity development, however you are also not adverse to repeating code when it makes sense for performance or simplicity.

Please do not use excessive design patterns or abstractions that may overcomplicate the codebase, but do use appropriate patterns and principles to ensure maintainability, scalability, and readability of the code. Please do not use comments in the code, but instead write clear and self-explanatory code that follows naming conventions and best practices for readability and maintainability.


Starfire Netcode Architecture — Complete Reference
High-Level Architecture

SERVER (~50 entities)                    CLIENT (~400k entities)
+------------------------------------+  +------------------------------------+
| Ghost entities (T0 only, ~20/player)|  | Player ghost (predicted)           |
| Player ships (ghost, predicted)     |  | T0 ghost entities (from server)    |
| ServerEntityTracker                 |  | T1-T4 local entities (LocalEntityTag)|
|   NativeHashMap ~400k analytical    |  |   Full ECS simulation from seed    |
|   positions, 10Hz proximity check   |  | ClientLocalPhysicsSystem           |
| GhostPromotionSystem               |  |   (thrust, aim, gravity, speed)    |
|   8 promote + 8 demote per frame    |  | ClientGhostSwapSystem              |
+------------------------------------+  |   (local↔ghost handoff)            |
         |                              +------------------------------------+
         | Ghosts: ~50 snapshots/tick
         | RPCs: promote/demote/damage/destroy
         +--------------------------------------------------+
Core insight: Only ~20 entities per player need server authority (T0/Loaded tier, within 5k units). Everything else (~400k entities) is simulated independently by each client from a shared deterministic seed.

All Architectural Changes (by Phase)
Phase 0-1: Netcode Foundation (Previous Sessions)
Reasoning: Unity Netcode for Entities requires [GhostField] on synced components, IInputComponentData for player input, and assembly references.

File	Change
All component structs	Added [GhostField] annotations on synced fields
PlayerInput.cs	Created — IInputComponentData with Throttle, MovementDirection, AimDirection, Fire, Warp
Starfire.asmdef	Added Unity.NetCode assembly reference
Phase 2: Server/Client World Split (Previous Sessions)
Reasoning: Netcode creates separate server and client worlds. Each system must declare which world(s) it runs in via [WorldSystemFilter].

File	Change
StarfireBootstrap.cs	Created — ClientServerBootstrap, creates server+client worlds, auto-connects port 7979
GoInGameSystem.cs	Created — Server: spawns player ghost on GoInGameRequest. Client: sends request, tags player entity
PlayerInputApplySystem.cs	Created — Server-only, copies PlayerInput → ControlInput
PlayerInputSystem.cs	Changed to ClientSimulation, moved to GhostInputSystemGroup
All 19 server systems	Added [WorldSystemFilter(ServerSimulation)]
5 predicted systems	Moved to PredictedSimulationSystemGroup
FloatingOriginSystem.cs	Split into ServerFloatingOriginSystem + ClientFloatingOriginSystem
WorldPositionSyncSystem.cs	Split into ServerWorldPositionSyncSystem + ClientWorldPositionSyncSystem
Phase 3: Client-Side Simulation
Reasoning: With only ~50 ghosts on the wire, the client must simulate its own T1-T4 entities. 20 systems changed from ServerSimulation to ServerSimulation | ClientSimulation so they process client-local entities too.

File	Change	Reasoning
LocalEntityTag.cs	Created	Marker component distinguishing client-local from ghost entities
ClientLocalPhysicsSystem.cs	Created	Client-only ISystem with 6 Burst jobs (thrust, aim, gravity, speed limit, velocity integration, constrain2D) for LocalEntityTag + RichTierTag entities. Needed because predicted systems only run on ghost entities with Simulate component
ClientWorldPositionSyncSystem.cs	Modified — Added LocalRichSyncJob	Two sync directions: ghosts (WorldPos→LocalTransform), local rich-tier (LocalTransform→WorldPos)
14 tier/ship/asteroid systems	Changed ServerSimulation → Server|Client	Tier evaluation, transitions, fleet formation/disband, dormant conversion/revival must run on client for local entities
5 more systems (AI, sensor, fleet AI, orbit, bounds wrap)	Changed ServerSimulation → Server|Client	Full simulation pipeline needed on client
MinimapDataSystem.cs	Changed ServerSimulation → ClientSimulation	Minimap is a client presentation concern
Phase 4: Server Lightweight Tracking
Reasoning: The server doesn't need full ECS entities for T1+ — just analytical positions for proximity detection and promotion decisions.

File	Change
ServerEntityTracker.cs	Created — SystemBase, maintains NativeHashMap<int, TrackedEntity> for ~400k entities. Analytical orbit updates for asteroids, dead-reckoning for ships. 10Hz proximity check flags entities for promotion/demotion
ServerSpawnSystem.cs	Modified — Only spawns T0 ghost entities now. Calls ServerEntityTracker.Initialize() with same seed. Advances entity IDs for T1+ without creating ECS entities
Phase 5: Ghost Promotion/Demotion
Reasoning: As the player moves, entities cross the T0 boundary. Entities entering T0 need server-authoritative ghosts; entities leaving T0 revert to client-local simulation.

File	Change
PromotionRpcs.cs	Created — EntityPromotedRpc (entity entering T0), EntityDemotedRpc (entity leaving T0, includes position/velocity/health state)
GhostPromotionSystem.cs	Created — Server-only, reads tracker flags, instantiates/destroys ghost prefabs, sends RPCs. Rate-limited 8 promotions + 8 demotions per frame
ClientGhostSwapSystem.cs	Created — Client-only, receives promotion/demotion RPCs. On promotion: hides local entity, waits for ghost arrival, then destroys local. On demotion: restores local entity from RPC state. 2s timeout fallback
Phase 6: State Corrections
Reasoning: When a player damages/destroys a ghost entity on the server, all other clients need to update their local copies.

File	Change
WorldEventRpcs.cs	Created — EntityDestroyedRpc, EntityDamagedRpc
ServerWorldEventSystem.cs	Created — Server-only, detects health changes on ghost entities, broadcasts RPCs to all clients, updates tracker
ClientWorldEventSystem.cs	Created — Client-only, processes destroy/damage RPCs, updates local entities via EntityIdToLocal hashmap
Phase 7: Client World Spawning
Reasoning: Client generates ALL entities locally from the same deterministic seed the server uses, so entity IDs match for promotion/demotion lookups.

File	Change
ClientWorldSpawnSystem.cs	Created — Client-only InitializationSystemGroup, one-time spawn. Calls SolarSystemGenerator.Generate() with same seed, creates local entities with LocalEntityTag, maintains NativeHashMap<int, Entity> EntityIdToLocal for RPC lookups
GhostRenderingInitSystem.cs	Created — Client-only, lazily adds MaterialMeshInfo to ghost ships/stars that arrive without rendering data
Runtime Fixes (This Session)
Reasoning: Post-implementation runtime errors discovered during play testing.

File	Change	Error Fixed
AsteroidFieldOrbitSystem.cs	Replaced [UpdateAfter(ServerWorldPositionSyncSystem/ServerFloatingOriginSystem)] with [UpdateAfter(FixedStepSimulationSystemGroup)]	Cross-world UpdateAfter warnings (referenced server-only systems from client world)
AsteroidSensorOrbitSystem.cs	Same UpdateAfter replacement	Same cross-world warnings
WorldBoundsWrapSystem.cs	Removed both [UpdateAfter] attributes	Cross-world warning + OrderFirst/OrderLast conflict with FixedStepSimulationSystemGroup
ClientWorldSpawnSystem.cs	Removed PhysicsWorldIndex from all 3 archetypes + SetTierTags	"Non-ghost physics objects in default world" — Netcode expects only ghosts in physics world 0
ClientLocalPhysicsSystem.cs	Removed [WithNone(Simulate)] from all jobs, added state.Dependency.Complete() before foreach, added LocalVelocityIntegrationJob, restructured job ordering	Job dependency errors + TempAlloc leaks (broken chaining, unnecessary Simulate type dependency)
ClientGhostSwapSystem.cs	Collect-then-apply pattern (NativeList) instead of structural changes inside foreach	InvalidOperationException: Structural changes not allowed during iteration
ClientWorldEventSystem.cs	Same collect-then-apply fix	Same structural change during iteration bug
ServerWorldEventSystem.cs	Same collect-then-apply fix	Same bug (CreateEntity inside foreach)
System Execution Order
Server World

InitializationSystemGroup
├── PhysicsConfigSystem .............. Creates PhysicsStep singleton, sets MaxDeltaTime
├── ServerSpawnSystem ................ One-time: spawns T0 ghosts, initializes tracker
│
SimulationSystemGroup
│
├── [GhostInputSystemGroup]
│   └── (receives PlayerInput from clients)
│
├── PredictedSimulationSystemGroup
│   ├── ShipThrustSystem ............. Predicted: applies thrust to player ship
│   ├── ShipAimSystem ................ Predicted: rotates player ship
│   ├── StarGravitySystem ............ Predicted: gravity on all RichTier
│   ├── Constrain2DSystem ............ Predicted: constrains Z-axis
│   └── SpeedLimitSystem ............. Predicted: clamps velocity
│
├── FixedStepSimulationSystemGroup
│   └── (Unity Physics: broadphase, narrowphase, solver, export)
│
├── ServerWorldPositionSyncSystem .... LocalTransform → WorldPosition (after physics)
├── ServerFloatingOriginSystem ....... Rebases origin when player exceeds threshold
├── WorldBoundsWrapSystem ............ Wraps entities at world boundary
│
├── TierEvaluationSystem ............. Burst job: computes tier for all entities
├── TierStateTransitionSystem ........ Applies tier tags, physics exclusion, rendering
├── ShipTierTransitionSystem ......... Ship velocity snapshot/restore on T1↔T2
├── AsteroidTierTransitionSystem ..... Asteroid velocity snapshot/restore on T1↔T2
├── TierTransitionCleanupSystem ...... Disables TierTransition event component
│   ├── ShipFleetFormationSystem ..... 1Hz: T2 ships → T3 fleets (max 5/tick)
│   ├── ShipFleetDisbandSystem ....... 1Hz: T3 fleets → T2 ships (max 3/tick)
│   ├── ShipDormantConversionSystem .. 1Hz: T3 → T4 dormant
│   ├── ShipDormantRevivalSystem ..... 1Hz: T4 → T3 revival
│   ├── AsteroidFieldFormationSystem . 1Hz: T2 asteroids → T3 fields (max 5/tick)
│   ├── AsteroidFieldDisbandSystem ... 1Hz: T3 fields → T2 asteroids (max 3/tick)
│   ├── AsteroidDormantConversion .... 1Hz: T3 → T4 dormant
│   └── AsteroidDormantRevivalSystem . 1Hz: T4 → T3 revival
│
├── AIControlSystem .................. NPC ship AI (FSM: patrol/pursue/combat/flee)
├── PlayerInputApplySystem ........... PlayerInput → ControlInput on player ship
├── SensorBatchUpdateSystem .......... Burst: all SensorTier ships in parallel
├── AsteroidSensorOrbitSystem ........ Burst: T2 asteroid circular orbit
├── AsteroidFieldOrbitSystem ......... T3 asteroid field/member orbital rotation
├── FleetAISystem .................... 1Hz: fleet patrol/intercept/retreat
│
├── ServerEntityTracker .............. 10Hz proximity check, analytical orbit updates
├── GhostPromotionSystem ............. Spawns/despawns ghosts (after tracker)
├── ServerWorldEventSystem ........... Broadcasts damage/destroy RPCs
│
├── MinimapDataSystem [OrderLast] .... Minimap rendering (only runs on server currently*)
*Note: MinimapDataSystem has WorldSystemFilterFlags.ServerSimulation in code but conceptually should be client — this may be a leftover from before the split.

Client World

InitializationSystemGroup
├── PhysicsConfigSystem .............. Creates PhysicsStep singleton
├── ClientWorldSpawnSystem ........... One-time: spawns all local entities from seed
│
SimulationSystemGroup
│
├── [GhostInputSystemGroup]
│   └── PlayerInputSystem ............ Reads keyboard/mouse → PlayerInput + ControlInput
│
├── PredictedSimulationSystemGroup
│   ├── ShipThrustSystem ............. Predicted: player ship thrust
│   ├── ShipAimSystem ................ Predicted: player ship rotation
│   ├── StarGravitySystem ............ Predicted: gravity on player ship
│   ├── Constrain2DSystem ............ Predicted: constrains Z-axis
│   └── SpeedLimitSystem ............. Predicted: clamps velocity
│
├── FixedStepSimulationSystemGroup
│   └── (Unity Physics: ghost entities only — local entities excluded)
│
├── ClientFloatingOriginSystem ....... Rebases origin to player WorldPosition
├── ClientWorldPositionSyncSystem .... Ghost: WorldPos→LocalTransform
│                                      Local rich: LocalTransform→WorldPos
├── WorldBoundsWrapSystem ............ Wraps entities at world boundary
│
├── ClientLocalPhysicsSystem ......... LOCAL entities only (not ghosts):
│   ├── LocalThrustJob                  ├ [LocalEntityTag + RichTierTag]
│   ├── LocalAimJob                     ├ Applies thrust from ControlInput
│   ├── LocalGravityJob                 ├ Applies star gravity
│   ├── LocalSpeedLimitJob              ├ Clamps velocity
│   ├── LocalVelocityIntegrationJob     ├ Integrates velocity → position
│   └── LocalConstrain2DJob             └ Constrains to 2D plane
│
├── TierEvaluationSystem ............. Evaluates tier for all local entities
├── TierStateTransitionSystem ........ Applies tier tags, rendering state
├── ShipTierTransitionSystem ......... Ship velocity snapshot/restore
├── AsteroidTierTransitionSystem ..... Asteroid velocity snapshot/restore
├── TierTransitionCleanupSystem
│   ├── ShipFleetFormationSystem
│   ├── ShipFleetDisbandSystem
│   ├── ShipDormantConversionSystem
│   ├── ShipDormantRevivalSystem
│   ├── AsteroidFieldFormationSystem
│   ├── AsteroidFieldDisbandSystem
│   ├── AsteroidDormantConversion
│   └── AsteroidDormantRevivalSystem
│
├── AIControlSystem .................. NPC AI for local RichTier ships
├── SensorBatchUpdateSystem .......... Burst: all local SensorTier ships
├── AsteroidSensorOrbitSystem ........ Burst: T2 asteroid orbits
├── AsteroidFieldOrbitSystem ......... T3 asteroid field orbits
├── FleetAISystem .................... Fleet behavior
│
├── AsteroidRenderingInitSystem ...... Lazy mesh/material/collider for rich-tier asteroids
├── GhostRenderingInitSystem ......... Lazy mesh/material for ghost ships/stars
│
├── ClientGhostSwapSystem ............ Processes promotion/demotion RPCs
├── ClientWorldEventSystem ........... Processes damage/destroy RPCs
│
├── GoInGameClientSystem ............. Sends GoInGameRequest on connection
├── ClientPlayerTagSystem ............ Tags player ghost with PlayerTag
System Filter Summary
Category	Count	Systems
Server only	9	ServerSpawnSystem, ServerWorldPositionSyncSystem, ServerFloatingOriginSystem, PlayerInputApplySystem, GoInGameSystem, ServerEntityTracker, GhostPromotionSystem, ServerWorldEventSystem, MinimapDataSystem
Client only	7	PlayerInputSystem, ClientWorldSpawnSystem, ClientWorldPositionSyncSystem, ClientFloatingOriginSystem, ClientLocalPhysicsSystem, AsteroidRenderingInitSystem, GhostRenderingInitSystem, ClientGhostSwapSystem, ClientWorldEventSystem, GoInGameClientSystem, ClientPlayerTagSystem
Server + Client	16	TierEvaluation, TierStateTransition, TierTransitionCleanup, ShipTierTransition, ShipFleetFormation/Disband, ShipDormantConversion/Revival, AsteroidTierTransition, AsteroidFieldFormation/Disband, AsteroidDormantConversion/Revival, AIControl, SensorBatchUpdate, AsteroidSensorOrbit, AsteroidFieldOrbit, FleetAI, WorldBoundsWrap, PhysicsConfig
Predicted	5	ShipThrust, ShipAim, StarGravity, SpeedLimit, Constrain2D
New Files Created (12)
File	Purpose
network/StarfireBootstrap.cs	Client/server world creation, auto-connect
network/GoInGameSystem.cs	Server+client go-in-game handshake (3 systems in 1 file)
network/PlayerInput.cs	Networked input component
network/PlayerInputApplySystem.cs	Server input → ControlInput
network/ClientWorldSpawnSystem.cs	Client-side deterministic world generation
network/ServerEntityTracker.cs	Lightweight analytical tracking for T1+
network/GhostPromotionSystem.cs	Server ghost spawn/despawn on T0 boundary
network/ClientGhostSwapSystem.cs	Client local↔ghost swap on promotion/demotion
network/PromotionRpcs.cs	Promoted/Demoted RPC definitions
network/WorldEventRpcs.cs	Destroyed/Damaged RPC definitions
network/ServerWorldEventSystem.cs	Server damage/destroy broadcast
network/ClientWorldEventSystem.cs	Client damage/destroy handling
entity/LocalEntityTag.cs	Client-local entity marker
systems/rich/ClientLocalPhysicsSystem.cs	Client physics for non-ghost entities
systems/presentation/GhostRenderingInitSystem.cs	Lazy rendering for ghost entities