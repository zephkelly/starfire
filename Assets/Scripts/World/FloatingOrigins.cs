using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Starfire
{
    public class FloatingOrigins : MonoBehaviour
    {
        private Transform playerTransform;
        private ShipController playerShipController;

        [SerializeField] private float floatingOriginLimit = 2000f;

        public UnityEvent<Vector2> OnFloatingOrigin;

        private void Start()
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            playerShipController = playerTransform.GetComponent<ShipController>();
        }

        private void Update()
        {
            if (playerTransform.position.magnitude > floatingOriginLimit)
            {
                Vector2 offset = -(Vector2)playerTransform.position;

                OnFloatingOrigin.Invoke(offset);
            }
        }
    }
}
