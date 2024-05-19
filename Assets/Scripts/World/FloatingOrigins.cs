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
        [SerializeField] private Transform player;

        private void Awake()
        {
            playerTransform = GameObject.Find("PlayerShip").transform;
        }

        private void LateUpdate()
        {
            // if (playerTransform.position.magnitude > floatingOriginLimit)
            // {
            //     worldObjects.position = new Vector2(
            //         worldObjects.position.x - playerTransform.position.x,
            //         worldObjects.position.y - playerTransform.position.y
            //     );

            //     player.position = new Vector2(
            //         player.position.x - playerTransform.position.x,
            //         player.position.y - playerTransform.position.y
            //     );

            //     Camera.main.transform.position = new Vector3(
            //         Camera.main.transform.position.x - playerTransform.position.x,
            //         Camera.main.transform.position.y - playerTransform.position.y,
            //         Camera.main.transform.position.z
            //     );
            // }
        }
    }
}
