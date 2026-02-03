using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    public struct CelestialBodyQueryResult
    {
        public CelestialBodyInfo? NearestStar;
        public float DistanceToNearestStar;
        public float RingDensity;
        public float GravityStrength;
        public List<CelestialBodyInfo> NearbyBodies;
    }

    public class CelestialBodyQuery : IWorldLayerQuery<CelestialBodyQueryResult>
    {
        private readonly CelestialBodyConfig _config;
        private readonly CelestialBodyLayer _layer;

        // Reusable list to avoid allocation per query
        private readonly List<CelestialBodyInfo> _tempBodies = new List<CelestialBodyInfo>(32);

        public CelestialBodyQuery(CelestialBodyConfig config, CelestialBodyLayer layer)
        {
            _config = config;
            _layer = layer;
        }

        public CelestialBodyQueryResult QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            _tempBodies.Clear();

            // Search radius: need to find the nearest star + its planets
            float searchRadius = _config.starSpacing * 1.5f + _config.planetOrbitRadiusRange.y;
            _layer.CollectBodiesInRadius(absolutePosition, searchRadius, worldSeed, _tempBodies);

            CelestialBodyInfo? nearestStar = null;
            float nearestStarDist = float.MaxValue;
            float ringDensity = 0f;
            float gravityStrength = 0f;

            foreach (var body in _tempBodies)
            {
                double distSq = (body.AbsolutePosition - absolutePosition).SqrMagnitude;
                float dist = (float)System.Math.Sqrt(distSq);

                // Track nearest star
                if (body.Kind == CelestialBodyKind.Star && dist < nearestStarDist)
                {
                    nearestStar = body;
                    nearestStarDist = dist;
                }

                // Accumulate gravity (simple inverse-square approximation)
                if (dist > 0.01f && dist < body.GravityRadius)
                {
                    float normalizedDist = dist / body.GravityRadius;
                    gravityStrength += body.Mass / (dist * dist) * 0.001f;
                }

                // Check ring density
                if (body.HasRing)
                {
                    if (dist >= body.RingInnerRadius && dist <= body.RingOuterRadius)
                    {
                        // Density falls off near edges of ring
                        float ringWidth = body.RingOuterRadius - body.RingInnerRadius;
                        float ringCenter = (body.RingInnerRadius + body.RingOuterRadius) * 0.5f;
                        float distFromCenter = Mathf.Abs(dist - ringCenter);
                        float edgeFade = 1f - Mathf.Clamp01(distFromCenter / (ringWidth * 0.5f));
                        ringDensity = Mathf.Max(ringDensity, edgeFade * _config.ringAsteroidDensity);
                    }
                }
            }

            return new CelestialBodyQueryResult
            {
                NearestStar = nearestStar,
                DistanceToNearestStar = nearestStarDist,
                RingDensity = ringDensity,
                GravityStrength = Mathf.Clamp01(gravityStrength),
                NearbyBodies = new List<CelestialBodyInfo>(_tempBodies),
            };
        }

        /// <summary>
        /// Get all bodies within a radius of a position.
        /// </summary>
        public List<CelestialBodyInfo> GetBodiesInRadius(Vector2D center, float radius, float worldSeed)
        {
            var results = new List<CelestialBodyInfo>();
            _layer.CollectBodiesInRadius(center, radius, worldSeed, results);
            return results;
        }

        /// <summary>
        /// Get the ring density at a specific position (0 = not in ring, up to config.ringAsteroidDensity).
        /// </summary>
        public float GetRingDensityAt(Vector2D position, float worldSeed)
        {
            _tempBodies.Clear();
            // Only need to search within max planet orbit + max ring radius
            float searchRadius = _config.starSpacing * 1.5f + _config.planetOrbitRadiusRange.y;
            _layer.CollectBodiesInRadius(position, searchRadius, worldSeed, _tempBodies);

            float maxRingDensity = 0f;
            foreach (var body in _tempBodies)
            {
                if (!body.HasRing) continue;

                double distSq = (body.AbsolutePosition - position).SqrMagnitude;
                float dist = (float)System.Math.Sqrt(distSq);

                if (dist >= body.RingInnerRadius && dist <= body.RingOuterRadius)
                {
                    float ringWidth = body.RingOuterRadius - body.RingInnerRadius;
                    float ringCenter = (body.RingInnerRadius + body.RingOuterRadius) * 0.5f;
                    float distFromCenter = Mathf.Abs(dist - ringCenter);
                    float edgeFade = 1f - Mathf.Clamp01(distFromCenter / (ringWidth * 0.5f));
                    maxRingDensity = Mathf.Max(maxRingDensity, edgeFade * _config.ringAsteroidDensity);
                }
            }

            return maxRingDensity;
        }

        object IWorldLayerQuery.QueryAt(Vector2D absolutePosition, float worldSeed)
        {
            return QueryAt(absolutePosition, worldSeed);
        }
    }
}
