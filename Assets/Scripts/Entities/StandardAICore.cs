using UnityEngine;

namespace Starfire
{
    public class StandardAICore : AICore
    {   
        public StandardAICore() { }

        public float TimeSpentNotCircling { get; private set; }
        public float TimeSpentCircling { get; private set; }

        protected float timeToSpendNotShootingProjectile = 0f;
        protected float timeToSpendShootingProjectile = 0f;
        public bool CanFireProjectile()
        {
            timeToSpendNotShootingProjectile -= Time.deltaTime;

            if (timeToSpendNotShootingProjectile <= 0)
            {
                timeToSpendShootingProjectile -= Time.deltaTime;

                if (timeToSpendShootingProjectile <= 0)
                {
                    timeToSpendNotShootingProjectile = Random.Range(1f, 2f);
                    timeToSpendShootingProjectile = Random.Range(6f, 8f);

                    return false;
                }

                return true;
            }

            return false;
        }

        public bool IsTargetWithinSight(Vector2 ourShipPosition, Vector2 relativeUpVector, Vector2 targetShipPosition, float sightDistance, float maximumFireAngle)
        {
            float distanceToPlayer = Vector2.Distance(ourShipPosition, targetShipPosition);
            float angleToPlayer = Vector2.Angle(relativeUpVector, targetShipPosition - ourShipPosition);

            if (distanceToPlayer < sightDistance && angleToPlayer < maximumFireAngle)
            {
                return true;
            }

            return false;
        }

        public Vector2 GetProjectileFiringPosition(Vector2 ourShipPosition, Vector2 targetShipPosition)
        {
            Vector2 perpendicularVector = Vector2.Perpendicular(targetShipPosition - ourShipPosition).normalized;

            if (Random.value > 0.5f)
            {
                perpendicularVector *= -1;
            }

            float amplitude = Random.Range(6f, 8f); // Adjust the range as needed
            float frequency = Random.Range(1f, 2f); // Adjust the range as needed
            Vector2 targetPosition = targetShipPosition + (perpendicularVector * Mathf.Sin(Time.time * frequency) * amplitude);

            return targetPosition;
        }

        public Vector2 GetTargetPosition(GameObject ourShipObject, Vector2 ourShipPosition, Vector2 ourShipVelocity, Vector2 targetShipPosition, LayerMask whichRaycastableLayers, float chaseRadius = 60f)
        {
            Vector2 directionToPlayer = targetShipPosition - ourShipPosition;
            Vector2 lastKnownPlayerPosition = Vector2.zero;

            RaycastHit2D[] hits = Physics2D.RaycastAll(ourShipPosition, directionToPlayer, chaseRadius, whichRaycastableLayers);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.gameObject == ourShipObject)
                {
                    continue;
                }

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    return hit.point;
                }

                lastKnownPlayerPosition = ShouldAddBiasDirection(hit, ourShipPosition, ourShipVelocity);
                break;
            }

            return lastKnownPlayerPosition;
        }

        private Vector2 ShouldAddBiasDirection(RaycastHit2D hit, Vector2 ourShipPosition, Vector2 ourShipVelocity)
        {
            Vector2 obstacleDirection = hit.centroid - ourShipPosition;
            Vector2 obstacleNormalizedPerpendicular = Vector2.Perpendicular(obstacleDirection).normalized;
            Vector2 biasDirection = obstacleNormalizedPerpendicular;

            float lateralVelocity = Vector2.Dot(ourShipVelocity, obstacleNormalizedPerpendicular);

            if (lateralVelocity < 0)
            {
                biasDirection = -biasDirection;
            }

            return hit.point + (biasDirection * 8f);
        }

        private Vector3[] radialRaycastData;  // x, y: direction, z: weight
        public Vector2 FindBestDirection(GameObject ourShipObject, Vector2 ourShipPosition, Vector2 lastTargetShipPosition, float ourShipVelocityMagnitude, int numberOfRays, LayerMask whichRaycastableLayers, float collisionCheckRadius = 30f)
        {
            radialRaycastData = new Vector3[numberOfRays];
            Vector2 direction = Vector2.zero;

            // Raycast in a circle around the scavenger
            for (int i = 0; i < numberOfRays; i++)
            {
                float angle = i * 2 * Mathf.PI / numberOfRays;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                direction.Normalize();

                float angleBetween = Vector2.Angle(direction, lastTargetShipPosition - ourShipPosition);
                float weight = Mathf.Pow(1f - (angleBetween / 180f), 1.15f);

                RaycastHit2D[] hits = Physics2D.RaycastAll(ourShipPosition, direction, collisionCheckRadius * (1 + Mathf.InverseLerp(0, 40, ourShipVelocityMagnitude)) * weight, whichRaycastableLayers);
                radialRaycastData[i] = direction;
                bool hitProcessed = false;

                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider.gameObject == ourShipObject) continue;

                    if (hit.collider == null)
                    {
                        radialRaycastData[i].z = weight;
                        continue;
                    }

                    if (!hitProcessed)
                    {
                        float normalizedDistance = 1f - (hit.distance / collisionCheckRadius);
                        radialRaycastData[i] = new Vector3(direction.x, direction.y, -normalizedDistance);
                        hitProcessed = true;
                    }
                }

                if (hitProcessed) continue;
                
                radialRaycastData[i] = new Vector3(direction.x, direction.y, weight);
            }

            // Normalise the weights
            for (int i = 0; i < numberOfRays; i++)
            {
                if (radialRaycastData[i].z <= 0)
                {
                    // If the current ray should be disinhibited, disinhibit the rays next to it
                    radialRaycastData[(i + 1 + numberOfRays) % numberOfRays].z = (radialRaycastData[(i + 1 + numberOfRays) % numberOfRays].z - 0.8f) / 2;
                    radialRaycastData[(i - 1 + numberOfRays) % numberOfRays].z = (radialRaycastData[(i - 1 + numberOfRays) % numberOfRays].z - 0.8f) / 2;
                }
            }

            Vector2 finalWeightedDirection = Vector2.zero;

            for (int i = 0; i < numberOfRays; i++)
            {
                finalWeightedDirection += (Vector2)radialRaycastData[i] * radialRaycastData[i].z;
            }

            finalWeightedDirection.Normalize();
            return finalWeightedDirection;
        }

        public Vector2 CircleTarget(Vector2 weightedDirection, Vector2 ourShipPosition, Vector2 ourShipVelocity, Vector2 targetShipPosition)
        {
            if (Vector2.Distance(ourShipPosition, targetShipPosition) < 80f)
            {
                TimeSpentCircling += Time.deltaTime;
                TimeSpentNotCircling = 0f;

                float predictionTime = 1f;
                Vector2 predictedPlayerPosition = targetShipPosition + (ourShipVelocity * predictionTime);
                Vector2 newPlayerDirection = predictedPlayerPosition - ourShipPosition;
                Vector2 newBiasDirection = Vector2.Perpendicular(newPlayerDirection).normalized;

                float distance = Vector2.Distance(ourShipPosition, targetShipPosition);
                float biasMagnitude = Mathf.InverseLerp(60, 0, distance);
                float playerVelocity = ourShipVelocity.magnitude;
                float biasMultiplier = 4f;
                
                if (playerVelocity > 50f)
                {
                    biasMultiplier = 2f;
                }

                weightedDirection += newBiasDirection * (biasMagnitude * biasMultiplier);
                weightedDirection.Normalize();
            }
            else
            {
                TimeSpentNotCircling += Time.deltaTime;
                TimeSpentCircling = 0f;
            }

            return weightedDirection;
        }
    }
}