using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace starfire
{
    public class FloatingOrigins : MonoBehaviour
    {
        private Transform playerTransform;
        private Vector3 lastPlayerPosition;

        [SerializeField] private float floatingOriginLimit = 2000f;

        [SerializeField] private Transform worldObjects;
        [SerializeField] private Transform playerObjects;

        private void Awake()
        {
            playerTransform = GameObject.Find("PlayerShip").transform;
        }

        private void LateUpdate()
        {
            if (playerTransform.position.magnitude > floatingOriginLimit)
            {
                worldObjects.position = new Vector2(
                    worldObjects.position.x - playerTransform.position.x,
                    worldObjects.position.y - playerTransform.position.y
                );

                playerObjects.position = new Vector2(
                    playerObjects.position.x - playerTransform.position.x,
                    playerObjects.position.y - playerTransform.position.y
                );
            }
        }
    }
}
