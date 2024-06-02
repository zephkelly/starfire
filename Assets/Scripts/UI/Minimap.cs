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

        [SerializeField] private float scaleFactor = 0.02f;
        [SerializeField] private GameObject starMarkerPrefab;

        private Dictionary<Vector2Int, GameObject> starMarkers = new Dictionary<Vector2Int, GameObject>();

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

            foreach (var starChunkAbsKey in ChunkManager.Instance.CurrentStarChunks)
            {
                Vector2 miniMapPos = GetMinimapPosition(starChunkAbsKey);

                if (starMarkers.ContainsKey(starChunkAbsKey))
                {
                    starMarkers[starChunkAbsKey].transform.localPosition = miniMapPos;
                    continue;
                }

                GameObject starMarker = Instantiate(starMarkerPrefab, panelTransform);
                starMarker.transform.localPosition = miniMapPos;

                starMarkers.Add(starChunkAbsKey, starMarker);
            }
        }

        private void ClearCurrentMarkers()
        {
            List<Vector2Int> keysToRemove = new List<Vector2Int>();

            foreach (var starMarker in starMarkers)
            {
                if (ChunkManager.Instance.CurrentStarChunks.Contains(starMarker.Key) == false)
                {
                    Destroy(starMarker.Value);
                    keysToRemove.Add(starMarker.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                starMarkers.Remove(key);
            }
        }

        Vector2 GetMinimapPosition(Vector2Int chunkKey)
        {
            Vector2 starPosition = ChunkManager.Instance.Chunks[chunkKey].StarPosition;
            Vector2 playerWorldPos = player.position;
            Vector2 relativePos = starPosition - playerWorldPos;
            
            relativePos *= scaleFactor;
            return relativePos;
        }
    }
}