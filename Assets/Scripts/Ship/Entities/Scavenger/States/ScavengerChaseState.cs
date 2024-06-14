using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public class ScavengerChaseState : IState
    {
        private ScavengerShipController shipController;
        private Rigidbody2D scavengerRigid2D;
        private Transform scavengerTransform;

        private Transform playerTransform;
        private Rigidbody2D playerRigid2D;

        private LayerMask whichRaycastableLayers;
        private Vector2 lastKnownPlayerPosition;
        private Vector2 weightedDirection;
        private Vector2 lerpVector;
        private Vector2 visualLerpVector;
        private Vector3[] positiveAngles = new Vector3[16];
        private bool setBiasDirection = false;
        private int numberOfRays = 40;
        private float chaseRadius = 300f;
        private float collisionCheckRadius = 12f;
        private float timeSpentCircling = 0f;

        public ScavengerChaseState(ScavengerShipController _scavenger, Rigidbody2D _scavengerRigid, Transform _playerTransfrom)
        {
            shipController = _scavenger;
            scavengerTransform = _scavenger.transform;
            scavengerRigid2D = _scavengerRigid;
            playerTransform = _playerTransfrom;
            playerRigid2D = _playerTransfrom.GetComponent<Rigidbody2D>();
        }

        public void Enter()
        {
            Debug.Log("Scavenger is chasing the player!");
            
            whichRaycastableLayers = LayerMask.GetMask("Player");
        }

        public void Execute()
        {
            if (playerTransform == null)
            {
                shipController.StateMachine.ChangeState(new ScavengerIdleState(shipController));
                return;
            }

            RaycastToPlayer();
            RaycastRadially();
            CirclePlayer();

            lerpVector = Vector2.Lerp(scavengerTransform.up, weightedDirection, 0.7f);
            visualLerpVector = Vector2.Lerp(scavengerTransform.up, weightedDirection, 0.15f);

            if (timeSpentCircling > 5f)
            {
                shipController.StateMachine.ChangeState(new ScavengerAttackState(shipController, scavengerRigid2D, playerTransform));
            }
        }

        public void FixedUpdate()
        {
            float distance = Vector2.Distance(scavengerTransform.position, playerTransform.position);
            float speed = Mathf.Lerp(shipController.MoveSpeed, shipController.MoveSpeed * 1.1f, Mathf.InverseLerp(40, 220, distance));

            shipController.Move(lerpVector, speed, true);
            scavengerTransform.up = visualLerpVector;
        }

        private void RaycastToPlayer() 
        {
            RaycastHit2D hit = Physics2D.Raycast(scavengerTransform.position, playerTransform.position - scavengerTransform.position, chaseRadius, whichRaycastableLayers);

            if (hit.collider == null) {
                return;
            }

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                lastKnownPlayerPosition = hit.point;
                setBiasDirection = false;
            }
            else
            {
                if (!setBiasDirection)
                {
                    Vector2 obstacleDirection = hit.centroid - (Vector2)scavengerTransform.position;
                    float lateralVelocity = Vector2.Dot(scavengerRigid2D.velocity, Vector2.Perpendicular(obstacleDirection).normalized);

                    Vector2 biasDirection;

                    if (lateralVelocity > 0)
                    {
                        biasDirection = Vector2.Perpendicular(obstacleDirection).normalized;
                    } 
                    else 
                    {
                        biasDirection = -Vector2.Perpendicular(obstacleDirection).normalized;
                    }

                    lastKnownPlayerPosition = hit.point + (biasDirection * 8f);

                    setBiasDirection = true;
                }
            }
        }

        private void RaycastRadially()
        {
            positiveAngles = new Vector3[numberOfRays];
            Vector2 direction = Vector2.zero;
            Vector2 rayStartPosition = Vector2.zero;

            for (int i = 0; i < numberOfRays; i++)
            {
                float angle = i * 2 * Mathf.PI / numberOfRays;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                direction.Normalize();

                float angleBetween = Vector2.Angle(direction, lastKnownPlayerPosition - (Vector2)scavengerTransform.position);
                float weight = Mathf.Pow(1f - (angleBetween / 180f), 2f);

                RaycastHit2D hit = Physics2D.Raycast((Vector2)scavengerTransform.position, direction, collisionCheckRadius * (1 + Mathf.InverseLerp(0, 40, scavengerRigid2D.velocity.magnitude)) * weight, whichRaycastableLayers);
                positiveAngles[i] = direction;

                if (hit.collider == null)
                {
                    positiveAngles[i].z = weight;
                }
                else 
                {
                    float normalizedDistance = 1f - (hit.distance / collisionCheckRadius);
                    positiveAngles[i].z = -normalizedDistance; // This will be between 0 (far) and -1 (close)
                }
            }

            //calulate the weighted direction
            for (int i = 0; i < numberOfRays; i++)
            {
                if (positiveAngles[i].z <= 0)
                {
                    //If the current ray should be disinhibited, disinhibit the rays next to it
                    positiveAngles[(i + 1 + numberOfRays) % numberOfRays].z = (positiveAngles[(i + 1 + numberOfRays) % numberOfRays].z - 0.8f) / 2;
                    positiveAngles[(i - 1 + numberOfRays) % numberOfRays].z = (positiveAngles[(i - 1 + numberOfRays) % numberOfRays].z - 0.8f) / 2;
                }
            }

            for (int i = 0; i < numberOfRays; i++)
            {
                weightedDirection += (Vector2)positiveAngles[i] * positiveAngles[i].z;
            }

            weightedDirection.Normalize();
        }

        private void CirclePlayer()
        {
            if (Vector2.Distance(scavengerTransform.position, playerTransform.position) < 80f)
            {
                timeSpentCircling += Time.deltaTime;

                float predictionTime = 1f;
                Vector2 predictedPlayerPosition = (Vector2)playerTransform.position + (playerRigid2D.velocity * predictionTime);
                Vector2 newPlayerDirection = predictedPlayerPosition - (Vector2)scavengerTransform.position;
                Vector2 newBiasDirection = Vector2.Perpendicular(newPlayerDirection).normalized;

                float distance = Vector2.Distance(scavengerTransform.position, playerTransform.position);
                float biasMagnitude = Mathf.InverseLerp(80, 0, distance);
                float playerVelocity = playerRigid2D.velocity.magnitude;
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
        }

        public void Exit()
        {
        }
    }
}
