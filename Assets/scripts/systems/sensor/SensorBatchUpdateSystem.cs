using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(SpeedLimitSystem))]
    [BurstCompile]
    public partial struct SensorBatchUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SensorUpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(SensorTierTag))]
        partial struct SensorUpdateJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(ref WorldPosition worldPos, ref ShipSnapshot snapshot, ref SensorContact sensor)
            {
                worldPos.Value += snapshot.Velocity * DeltaTime;

                sensor.StateTimer += DeltaTime;

                switch (sensor.CurrentAIState)
                {
                    case 0:
                        if (math.lengthsq(sensor.Waypoint) > 0.01)
                        {
                            sensor.PreviousAIState = 0;
                            sensor.CurrentAIState = 1;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 1:
                        double2 toWaypoint = sensor.Waypoint - worldPos.Value;
                        double wpDistSq = toWaypoint.x * toWaypoint.x + toWaypoint.y * toWaypoint.y;
                        if (wpDistSq < 40000.0)
                        {
                            sensor.Waypoint = worldPos.Value + (double2)(new float2(
                                math.cos(sensor.Heading * math.TORADIANS),
                                math.sin(sensor.Heading * math.TORADIANS)) * 5000f);
                            sensor.StateTimer = 0f;
                        }
                        else
                        {
                            float targetHeading = math.degrees((float)math.atan2(toWaypoint.y, toWaypoint.x));
                            sensor.Heading = targetHeading;
                        }
                        if (sensor.StateTimer > 30f && sensor.HullPercent > 0.5f)
                        {
                            sensor.PreviousAIState = 1;
                            sensor.CurrentAIState = 2;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 2:
                        if (sensor.StateTimer > 10f)
                        {
                            sensor.PreviousAIState = 2;
                            sensor.CurrentAIState = 3;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 3:
                        if (sensor.HullPercent < 0.3f)
                        {
                            sensor.PreviousAIState = 3;
                            sensor.CurrentAIState = 4;
                            sensor.StateTimer = 0f;
                        }
                        else if (sensor.StateTimer > 15f)
                        {
                            sensor.PreviousAIState = 3;
                            sensor.CurrentAIState = 1;
                            sensor.StateTimer = 0f;
                        }
                        break;

                    case 4:
                        if (sensor.StateTimer > 10f)
                        {
                            sensor.PreviousAIState = 4;
                            sensor.CurrentAIState = 0;
                            sensor.StateTimer = 0f;
                        }
                        break;
                }

                float headingRad = sensor.Heading * math.TORADIANS;
                snapshot.Velocity = new double2(
                    math.cos(headingRad) * sensor.Speed,
                    math.sin(headingRad) * sensor.Speed);
                snapshot.Heading = sensor.Heading;
                snapshot.AIState = sensor.CurrentAIState;
                snapshot.Position = worldPos.Value;
            }
        }
    }
}
