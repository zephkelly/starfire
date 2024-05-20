using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Starfire
{
    public class Minimap : MonoBehaviour
    {
        public static Minimap Instance;

        private Transform player;
        [SerializeField] private Transform panelTransform;

        [SerializeField] private float scaleFactor = 0.5f;
        [SerializeField] private GameObject starMarkerPrefab;

        private Dictionary<Vector2, GameObject> starMarkers = new Dictionary<Vector2, GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            player = GameObject.Find("PlayerShip").transform;
        }

        private void Update()
        {
            UpdateMarkerPositions();
        }

        private void UpdateMarkerPositions()
        {
            foreach (var starMarker in starMarkers)
            {
                Vector2 miniMapPos = GetMinimapPosition(starMarker.Key);
                starMarker.Value.transform.localPosition = miniMapPos;
            }
        }

        public void UpdateMinimapMarkers()
        {      
            ClearCurrentMarkers();

            foreach (var starPosition in ChunkManager.Instance.CurrentStarPositions)
            {
                Vector2 miniMapPos = GetMinimapPosition(starPosition);
                GameObject starMarker = Instantiate(starMarkerPrefab, panelTransform);
                starMarker.transform.localPosition = miniMapPos;

                if (starMarkers.ContainsKey(starPosition)) return;
                starMarkers.Add(starPosition, starMarker);
            }
        }

        private void ClearCurrentMarkers()
        {
            foreach (var starMarker in starMarkers)
            {
                Debug.Log("Destroying star marker");
                Destroy(starMarker.Value);
            }

            starMarkers.Clear();
        }

        Vector2 GetMinimapPosition(Vector2 position)
        {
            Vector2 playerWorldPos = player.position;
            Vector2 relativePos = position - playerWorldPos;
            
            relativePos *= scaleFactor;
            return relativePos;
        }
    }
}