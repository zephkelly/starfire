using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire
{
    public class ScavengerChaseState : IState
    {
        private ScavengerShipController _shipController;
        private StateMachine _stateMachine;
        private GameObject _scavengerObject;
        private Rigidbody2D _scavengerRigid2D;
        private Transform _scavengerTransform;
        private Transform _playerTransform;
        private Rigidbody2D _playerRigid2D;

        private Dictionary<Transform, float> nearbyEntites = new Dictionary<Transform, float>();
        private const float nearbyEntityRetentionTime = 10f;

        // Raycasting variables
        private Vector3[] radialRaycastData;  // x, y: direction, z: weight
        private Vector2 lerpVector;
        private Vector2 visualLerpVector;

        private LayerMask whichRaycastableLayers;
        // private bool setBiasDirection = false;
        private int numberOfRays = 16;
        private float chaseRadius = 300f;
        private float collisionCheckRadius = 30f;
        private float minimumDistanceToPlayer = 70f;

        private float timeSpentCircling = 0f;

        public ScavengerChaseState(ScavengerShipController scavengerController)
        {
            _shipController = scavengerController;
            _stateMachine = scavengerController.ScavengerStateMachine;
            _scavengerObject = scavengerController.ScavengerObject;
            _scavengerRigid2D = scavengerController.ScavengerRigidbody;
            _scavengerTransform = scavengerController.ScavengerTransform;
            _playerTransform = scavengerController.PlayerTransform;
            _playerRigid2D = scavengerController.PlayerRigidbody;
        }

        public void Enter()
        {
            whichRaycastableLayers = LayerMask.GetMask("Player", "Entities");
        }

        public void Execute()
        {
            if (_playerTransform == null)
            {
                _stateMachine.ChangeState(new ScavengerIdleState(_shipController));
                return;
            }

            NearbyEntityTimeTick();

            Vector2 lastKnownPlayerPosition = GetPlayerPosition();
            Vector2 weightedDirection = FindBestDirection(lastKnownPlayerPosition);
            weightedDirection = CirclePlayer(weightedDirection);

            lerpVector = Vector2.Lerp(_scavengerTransform.up, weightedDirection, 0.7f);
            visualLerpVector = Vector2.Lerp(_scavengerTransform.up, weightedDirection, 0.15f);

            if (timeSpentCircling > 5f)
            {
                _stateMachine.ChangeState(new ScavengerAttackState(_shipController, _scavengerRigid2D, _playerTransform));
            }
        }

        public void FixedUpdate()
        {
            float distance = 0f;

            if (_playerTransform != null)
            {
                distance = Vector2.Distance(_scavengerTransform.position, _playerTransform.position);
            }

            float speedMultiplier = GetSpeedMultiplier(distance);
            float speed = _shipController.MoveSpeed * speedMultiplier;

            _shipController.Move(lerpVector, speed, true);
            _scavengerTransform.up = visualLerpVector;
        }

        private float GetSpeedMultiplier(float distance)
        {
            float minSpeedMultiplier = 0.5f;
            float maxSpeedMultiplier = 1.1f;
            return Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, Mathf.InverseLerp(0, 220, distance));
        }

        private Vector2 GetPlayerPosition()
        {
            Vector2 directionToPlayer = _playerTransform.position - _scavengerTransform.position;
            Vector2 lastKnownPlayerPosition = Vector2.zero;

            RaycastHit2D[] hits = Physics2D.RaycastAll(_scavengerTransform.position, directionToPlayer, chaseRadius, whichRaycastableLayers);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.gameObject == _scavengerObject)
                {
                    continue;
                }

                if (hit.collider.gameObject.CompareTag("Entities"))
                {
                    AddToNearbyEntities(hit.collider.transform);
                }

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    return hit.point;
                }

                lastKnownPlayerPosition = ShouldAddBiasDirection(hit);
                break;
            }

            return lastKnownPlayerPosition;
        }

        private Vector2 ShouldAddBiasDirection(RaycastHit2D hit)
        {
            Vector2 obstacleDirection = hit.centroid - (Vector2)_scavengerTransform.position;
            Vector2 obstacleNormalizedPerpendicular = Vector2.Perpendicular(obstacleDirection).normalized;
            Vector2 biasDirection = obstacleNormalizedPerpendicular;

            float lateralVelocity = Vector2.Dot(_scavengerRigid2D.velocity, obstacleNormalizedPerpendicular);

            if (lateralVelocity < 0)
            {
                biasDirection = -biasDirection;
            }

            return hit.point + (biasDirection * 8f);
        }

        private Vector2 FindBestDirection(Vector2 lastPlayerPosition)
        {
            radialRaycastData = new Vector3[numberOfRays];
            Vector2 direction = Vector2.zero;
            Vector2 rayStartPosition = Vector2.zero;

            // Raycast in a circle around the scavenger
            for (int i = 0; i < numberOfRays; i++)
            {
                float angle = i * 2 * Mathf.PI / numberOfRays;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                direction.Normalize();

                float angleBetween = Vector2.Angle(direction, lastPlayerPosition - (Vector2)_scavengerTransform.position);
                float weight = Mathf.Pow(1f - (angleBetween / 180f), 1.5f);

                RaycastHit2D[] hits = Physics2D.RaycastAll((Vector2)_scavengerTransform.position, direction, collisionCheckRadius * (1 + Mathf.InverseLerp(0, 40, _scavengerRigid2D.velocity.magnitude)) * weight, whichRaycastableLayers);
                radialRaycastData[i] = direction;
                bool hitProcessed = false;

                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider.gameObject == _scavengerObject) continue;

                    if (hit.collider == null)
                    {
                        radialRaycastData[i].z = weight;
                        continue;
                    }

                    if (hit.collider.gameObject.CompareTag("Entities"))
                    {
                        AddToNearbyEntities(hit.collider.transform);
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

        private Vector2 CirclePlayer(Vector2 weightedDirection)
        {
            float distanceMultiplier = minimumDistanceToPlayer * 0.12f;
            float randomDistance = UnityEngine.Random.Range(minimumDistanceToPlayer - minimumDistanceToPlayer, minimumDistanceToPlayer + minimumDistanceToPlayer);


            if (Vector2.Distance(_scavengerTransform.position, _playerTransform.position) < 80f)
            {
                timeSpentCircling += Time.deltaTime;

                float predictionTime = 1f;
                Vector2 predictedPlayerPosition = (Vector2)_playerTransform.position + (_playerRigid2D.velocity * predictionTime);
                Vector2 newPlayerDirection = predictedPlayerPosition - (Vector2)_scavengerTransform.position;
                Vector2 newBiasDirection = Vector2.Perpendicular(newPlayerDirection).normalized;

                float distance = Vector2.Distance(_scavengerTransform.position, _playerTransform.position);
                float biasMagnitude = Mathf.InverseLerp(80, 0, distance);
                float playerVelocity = _playerRigid2D.velocity.magnitude;
                float biasMultiplier = 6f;
                
                if (playerVelocity > 50f)
                {
                    biasMultiplier = 2f;
                }

                weightedDirection += newBiasDirection * (biasMagnitude * biasMultiplier);
                weightedDirection.Normalize();
            }
            else
            {
                timeSpentCircling = 0f;
            }

            return weightedDirection;
        }

        private void NearbyEntityTimeTick()
        {
            List<Transform> entitiesToRemove = new List<Transform>();

            foreach (var entity in nearbyEntites.ToList())
            {
                if (entity.Key == null)
                {
                    entitiesToRemove.Add(entity.Key);
                    continue;
                }

                nearbyEntites[entity.Key] -= Time.deltaTime;

                if (nearbyEntites[entity.Key] <= 0)
                {
                    entitiesToRemove.Add(entity.Key);
                }
            }

            foreach (Transform entity in entitiesToRemove)
            {
                RemoveFromNearbyEntities(entity);
            }
        }

        private void AddToNearbyEntities(Transform newNearbyEntity)
        {
            if (newNearbyEntity == null) return;

            if (nearbyEntites.ContainsKey(newNearbyEntity))
            {
                nearbyEntites[newNearbyEntity] = nearbyEntityRetentionTime;
            }
            else
            {
                nearbyEntites.Add(newNearbyEntity, nearbyEntityRetentionTime);
            }
        }

        private void RemoveFromNearbyEntities(Transform entityToRemove)
        {
            if (nearbyEntites.ContainsKey(entityToRemove))
            {
                nearbyEntites.Remove(entityToRemove);
            }
        }

        public void Exit()
        {

        }
    }
}
