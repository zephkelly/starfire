using UnityEngine;

namespace Starfire
{
    public class ScavengerAttackState : IState
    {
        private ScavengerShipController shipController;
        private Rigidbody2D scavengerRigid2D;
        private Transform scavengerTransform;
        private Transform playerTransform;
        private Rigidbody2D playerRigid2D;

        private Vector3[] positiveAngles = new Vector3[16];
        private LayerMask whichRaycastableLayers;
        private Vector2 lastKnownPlayerPosition;

        private Vector2 weightedDirection;
        private Vector2 lerpVector;
        private Vector2 visualLerpVector;
        private int numberOfRays = 16;
        private float chaseRadius = 300f;
        private float collisionCheckRadius = 12f;

        private MovementPattern currentMovementPattern = MovementPattern.Normal;
        private CirclePattern currentCirclePattern = CirclePattern.Clockwise;
        private float timeTillCirclePatternChange = 2f;
        private float timeTillMovePatternChange = 3f;
        private float timeSpentNotCircling = 0f;

        private enum CirclePattern
        {
            Clockwise,
            CounterClockwise,
        }

        private enum MovementPattern
        {
            Normal,
            ZigZag,
            Fixed,
            FigureEight
        }

        public ScavengerAttackState(ScavengerShipController _scavenger, Rigidbody2D _scavengerRigid, Transform _playerTransfrom)
        {
            shipController = _scavenger;
            scavengerTransform = _scavenger.transform;
            scavengerRigid2D = _scavengerRigid;
            playerTransform = _playerTransfrom;
            playerRigid2D = _playerTransfrom.GetComponent<Rigidbody2D>();
        }

        public void Enter()
        {
            timeTillCirclePatternChange = Random.Range(4f, 8f);
            timeTillMovePatternChange = Random.Range(4f, 8f);
            whichRaycastableLayers = LayerMask.GetMask("Player");
        }

        public void Execute()
        {
            if (playerTransform == null)
            {
                shipController.ScavengerStateMachine.ChangeState(new ScavengerIdleState(shipController));
                return;
            }

            if (timeSpentNotCircling > 4f)
            {
                shipController.ScavengerStateMachine.ChangeState(new ScavengerChaseState(shipController));
            }

            RaycastToPlayer();
            RaycastRadially();
            CirclePlayer();

            lerpVector = Vector2.Lerp(scavengerTransform.up, AdjustLerpPattern(weightedDirection), 0.7f);
            visualLerpVector = Vector2.Lerp(scavengerTransform.up, AdjustVisualLerpPattern(weightedDirection), 0.15f);

            currentCirclePattern = GetRandomCirclePattern();
            currentMovementPattern = GetRandomMovementPattern();

            shipController.FireProjectile();
        }

        public void FixedUpdate()
        {
            shipController.Move(lerpVector, GetShipSpeed(), true);
            scavengerTransform.up = visualLerpVector;
        }

        private Vector2 AdjustLerpPattern(Vector2 _weightedDirection)
        {
            Vector2 newLerpVector = _weightedDirection;

            if (currentMovementPattern == MovementPattern.Normal)
            {
                newLerpVector = _weightedDirection;
            }
            else if (currentMovementPattern == MovementPattern.ZigZag)
            {
                newLerpVector = _weightedDirection + (Vector2.Perpendicular(_weightedDirection).normalized * Mathf.Sin(Time.time * 3f) * 0.8f);
            }

            return newLerpVector;
        }

        private Vector2 AdjustVisualLerpPattern(Vector2 _weightedDirection)
        {
            Vector2 newVisualLerpVector = AdjustLerpPattern(_weightedDirection);

            if (currentMovementPattern == MovementPattern.Fixed)
            {
                newVisualLerpVector = (lastKnownPlayerPosition - (Vector2)scavengerTransform.position).normalized;
            }

            return newVisualLerpVector;
        }

        private float GetShipSpeed()
        {
            float shipSpeed = shipController.MoveSpeed;

            if (currentMovementPattern == MovementPattern.Fixed)
            {
                shipSpeed = shipSpeed * 0.5f;
            }

            return shipSpeed;
        }

        private CirclePattern GetRandomCirclePattern()
        {
            CirclePattern newCirclePattern = currentCirclePattern;
            timeTillCirclePatternChange -= Time.deltaTime;

            if (timeTillCirclePatternChange <= 0)
            {
                newCirclePattern = (CirclePattern)Random.Range(0, 2);
                timeTillCirclePatternChange = Random.Range(4f, 8f);
            }

            return newCirclePattern;
        }

        private MovementPattern GetRandomMovementPattern()
        {
            MovementPattern newMovementPattern = currentMovementPattern;
            timeTillMovePatternChange -= Time.deltaTime;

            if (timeTillMovePatternChange <= 0)
            {
                timeTillMovePatternChange = Random.Range(4f, 8f);
                int randomMovementPattern = Random.Range(0, 10);

                if (randomMovementPattern <= 1)
                {
                    newMovementPattern = MovementPattern.Fixed;
                }
                else if (randomMovementPattern <= 3)
                {
                    newMovementPattern = MovementPattern.ZigZag;
                }
                else
                {
                    newMovementPattern = MovementPattern.Normal;
                }
            }

            return newMovementPattern;
        }

        private bool setBiasDirection = false;
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
                // TODO: Choose a bias direction based on the future position of the scavenger, to get more accurate predicting based on velocity
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
                timeSpentNotCircling = 0f;

                float predictionTime = 1f;
                Vector2 predictedPlayerPosition = (Vector2)playerTransform.position + (playerRigid2D.velocity * predictionTime);
                Vector2 newPlayerDirection = predictedPlayerPosition - (Vector2)scavengerTransform.position;
                Vector2 newBiasDirection = Vector2.zero;

                if (currentCirclePattern == CirclePattern.Clockwise)
                {
                    newBiasDirection = Vector2.Perpendicular(newPlayerDirection).normalized;
                }
                else
                {
                    newBiasDirection = -Vector2.Perpendicular(newPlayerDirection).normalized;
                }

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
                timeSpentNotCircling += Time.deltaTime;
            }
        }

        public void Exit()
        {
        }
    }
}