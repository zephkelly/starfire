using System;

namespace Starfire.Core.V2.World.Simulation.Collision
{
    /// <summary>
    /// Static helper methods for collision detection and response.
    /// All methods use double-precision for position accuracy.
    /// </summary>
    public static class CollisionMath
    {
        /// <summary>
        /// Tests if two circles overlap.
        /// </summary>
        /// <param name="posA">Center of circle A</param>
        /// <param name="radiusA">Radius of circle A</param>
        /// <param name="posB">Center of circle B</param>
        /// <param name="radiusB">Radius of circle B</param>
        /// <param name="penetrationDepth">How much the circles overlap</param>
        /// <param name="collisionNormal">Direction from A to B (normalized)</param>
        /// <returns>True if circles overlap</returns>
        public static bool CheckCircleCollision(
            Vector2D posA, float radiusA,
            Vector2D posB, float radiusB,
            out double penetrationDepth,
            out Vector2D collisionNormal)
        {
            Vector2D delta = posB - posA;
            double distSqr = delta.SqrMagnitude;
            double minDist = radiusA + radiusB;

            if (distSqr >= minDist * minDist)
            {
                penetrationDepth = 0;
                collisionNormal = Vector2D.Zero;
                return false;
            }

            double dist = Math.Sqrt(distSqr);
            penetrationDepth = minDist - dist;

            // If entities are at exact same position, use arbitrary normal
            if (dist > 1e-10)
            {
                collisionNormal = delta / dist;
            }
            else
            {
                collisionNormal = new Vector2D(1, 0);
            }

            return true;
        }

        /// <summary>
        /// Resolves an elastic collision between two entities.
        /// Updates velocities in place.
        /// </summary>
        /// <param name="velA">Velocity of entity A (modified)</param>
        /// <param name="massA">Mass of entity A</param>
        /// <param name="velB">Velocity of entity B (modified)</param>
        /// <param name="massB">Mass of entity B</param>
        /// <param name="normal">Collision normal (from A toward B)</param>
        /// <param name="restitution">Coefficient of restitution (1 = elastic, 0 = inelastic)</param>
        public static void ResolveElasticCollision(
            ref Vector2D velA, float massA,
            ref Vector2D velB, float massB,
            Vector2D normal,
            float restitution = 1.0f)
        {
            // Relative velocity along collision normal
            Vector2D relVel = velA - velB;
            double velAlongNormal = Vector2D.Dot(relVel, normal);

            // Don't resolve if velocities are separating
            if (velAlongNormal > 0)
                return;

            // Collision impulse magnitude
            double j = -(1 + restitution) * velAlongNormal;
            j /= (1.0 / massA) + (1.0 / massB);

            // Apply impulse
            Vector2D impulse = normal * j;
            velA = velA + impulse / massA;
            velB = velB - impulse / massB;
        }

        /// <summary>
        /// Separates two overlapping entities by moving them apart.
        /// Movement is distributed inversely proportional to mass.
        /// </summary>
        public static void SeparateOverlap(
            ref Vector2D posA, float massA,
            ref Vector2D posB, float massB,
            Vector2D normal,
            double penetrationDepth)
        {
            if (penetrationDepth <= 0)
                return;

            double totalMass = massA + massB;
            if (totalMass < 1e-10)
                return;

            // Heavier objects move less
            double ratioA = massB / totalMass;
            double ratioB = massA / totalMass;

            // Add small buffer to prevent re-collision
            double separationDistance = penetrationDepth * 1.01;

            posA = posA - normal * (separationDistance * ratioA);
            posB = posB + normal * (separationDistance * ratioB);
        }

        /// <summary>
        /// Calculates the kinetic energy of an impact.
        /// Uses relative velocity to determine collision severity.
        /// </summary>
        /// <param name="velA">Velocity of entity A</param>
        /// <param name="massA">Mass of entity A</param>
        /// <param name="velB">Velocity of entity B</param>
        /// <param name="massB">Mass of entity B</param>
        /// <returns>Impact energy (always positive)</returns>
        public static double CalculateImpactEnergy(
            Vector2D velA, float massA,
            Vector2D velB, float massB)
        {
            Vector2D relVel = velA - velB;
            double relSpeed = relVel.Magnitude;

            // Reduced mass for two-body collision
            double reducedMass = (massA * massB) / (massA + massB);

            // Kinetic energy of relative motion
            return 0.5 * reducedMass * relSpeed * relSpeed;
        }

        /// <summary>
        /// Returns the magnitude of relative velocity (collision intensity).
        /// </summary>
        public static double CalculateCollisionIntensity(Vector2D velA, Vector2D velB)
        {
            return (velA - velB).Magnitude;
        }

        /// <summary>
        /// Determines if an impact has enough energy to destroy an entity.
        /// </summary>
        /// <param name="impactEnergy">Energy of the impact</param>
        /// <param name="structuralIntegrity">Entity's structural integrity</param>
        /// <returns>True if entity should be destroyed</returns>
        public static bool ShouldDestroy(double impactEnergy, float structuralIntegrity)
        {
            return impactEnergy > structuralIntegrity;
        }
    }
}
