using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct AIControlSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            float dt = SystemAPI.Time.DeltaTime;

            new AIControlJob
            {
                PlayerWorldPos = playerWorldPos,
                DeltaTime = dt
            }.ScheduleParallel();
        }

        [BurstCompile]
    [WithAll(typeof(ShipTag), typeof(RichTierTag))]
    [WithNone(typeof(PlayerTag))]
        partial struct AIControlJob : IJobEntity
        {
            public double2 PlayerWorldPos;
            public float DeltaTime;

            void Execute(
                ref ControlInput input,
                ref SensorContact sensor,
                in WorldPosition worldPos,
                in ShipRotation rotation)
            {
                if (input.DriverType != 1)
                    return;

                sensor.StateTimer += DeltaTime;
                double2 toPlayer = PlayerWorldPos - worldPos.Value;
                double playerDistSq = toPlayer.x * toPlayer.x + toPlayer.y * toPlayer.y;

                switch (sensor.CurrentAIState)
                {
                    case 0: // Idle
                        input.Throttle = 0f;
                        input.FirePressed = 0;

                        if (sensor.StateTimer > 3f)
                        {
                            float heading = rotation.CurrentHeading * math.TORADIANS;
                            sensor.Waypoint = worldPos.Value + (double2)(new float2(
                                math.cos(heading), math.sin(heading)) * sensor.SensorRange * 0.8f);
                            sensor.PreviousAIState = 0;
                            sensor.CurrentAIState = 1;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 1: // Patrol
                        double2 toWaypoint = sensor.Waypoint - worldPos.Value;
                        double wpDistSq = toWaypoint.x * toWaypoint.x + toWaypoint.y * toWaypoint.y;

                        if (wpDistSq < 40000.0)
                        {
                            float heading = sensor.Heading * math.TORADIANS;
                            sensor.Waypoint = worldPos.Value + (double2)(new float2(
                                math.cos(heading + 0.5f), math.sin(heading + 0.5f)) * sensor.SensorRange * 0.8f);
                        }
                        else
                        {
                            var dir = (float2)math.normalizesafe(toWaypoint);
                            input.MovementDirection = dir;
                            input.AimDirection = dir;
                            sensor.Heading = math.degrees(math.atan2(dir.y, dir.x));
                        }

                        input.Throttle = 1f;
                        input.FirePressed = 0;

                        double weaponRange2x = sensor.WeaponRange * 2f;
                        if (playerDistSq < weaponRange2x * weaponRange2x && sensor.HullPercent > 0.5f)
                        {
                            sensor.PreviousAIState = 1;
                            sensor.CurrentAIState = 2;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 2: // Pursue
                        if (playerDistSq > 0.0001)
                        {
                            var pursueDir = (float2)math.normalizesafe(toPlayer);
                            input.MovementDirection = pursueDir;
                            input.AimDirection = pursueDir;
                            sensor.Heading = math.degrees(math.atan2(pursueDir.y, pursueDir.x));
                        }
                        input.Throttle = 1f;
                        input.FirePressed = 0;

                        if (playerDistSq < (double)sensor.WeaponRange * sensor.WeaponRange)
                        {
                            sensor.PreviousAIState = 2;
                            sensor.CurrentAIState = 3;
                            sensor.StateTimer = 0f;
                        }
                        else if (playerDistSq > (double)sensor.SensorRange * sensor.SensorRange)
                        {
                            sensor.PreviousAIState = 2;
                            sensor.CurrentAIState = 1;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 3: // Combat
                        if (playerDistSq > 0.0001)
                        {
                            var aimDir = (float2)math.normalizesafe(toPlayer);
                            input.AimDirection = aimDir;

                            float orbitAngle = math.atan2(aimDir.y, aimDir.x) + math.PI * 0.5f;
                            input.MovementDirection = new float2(math.cos(orbitAngle), math.sin(orbitAngle));
                        }
                        input.Throttle = 0.6f;
                        input.FirePressed = playerDistSq < (double)sensor.WeaponRange * sensor.WeaponRange ? (byte)1 : (byte)0;

                        if (sensor.HullPercent < 0.3f)
                        {
                            sensor.PreviousAIState = 3;
                            sensor.CurrentAIState = 4;
                            sensor.StateTimer = 0f;
                        }
                        else
                        {
                            double weaponRange1_5 = sensor.WeaponRange * 1.5f;
                            if (playerDistSq > weaponRange1_5 * weaponRange1_5)
                            {
                                sensor.PreviousAIState = 3;
                                sensor.CurrentAIState = 2;
                                sensor.StateTimer = 0f;
                            }
                        }
                        break;

                    case 4: // Flee
                        if (playerDistSq > 0.0001)
                        {
                            var fleeDir = (float2)math.normalizesafe(-toPlayer);
                            input.MovementDirection = fleeDir;
                            input.AimDirection = fleeDir;
                            sensor.Heading = math.degrees(math.atan2(fleeDir.y, fleeDir.x));
                        }
                        input.Throttle = 1f;
                        input.FirePressed = 0;

                        double sensorRange1_5 = sensor.SensorRange * 1.5f;
                        if (sensor.StateTimer > 10f || playerDistSq > sensorRange1_5 * sensorRange1_5)
                        {
                            sensor.PreviousAIState = 4;
                            sensor.CurrentAIState = 0;
                            sensor.StateTimer = 0f;
                        }
                        break;
                }
            }
        }
    }
}
