using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Starfire
{
    public class Minimap : MonoBehaviour
    {
        // public static Minimap Instance;

        // private Transform player;
        // [SerializeField] private Transform panelTransform;

        // [SerializeField] private float scaleFactor = 0.5f;
        // [SerializeField] private GameObject starMarkerPrefab;

        // private Dictionary<Vector2Int, GameObject> starMarkers = new Dictionary<Vector2Int, GameObject>();

        // private void Awake()
        // {
        //     if (Instance == null)
        //     {
        //         Instance = this;
        //     }
        //     else
        //     {
        //         Destroy(gameObject);
        //     }

        //     player = GameObject.Find("PlayerShip").transform;
            
        // }

        // private void Update()
        // {
        //     UpdateMarkerPositions();
        // }

        // private void UpdateMarkerPositions()
        // {
        //     foreach (var starMarker in starMarkers)
        //     {
        //         Vector2 miniMapPos = GetMinimapPosition(starMarker.Key);
        //         starMarker.Value.transform.localPosition = miniMapPos;
        //     }
        // }

        // public void UpdateMinimapMarkers()
        // {      
        //     ClearCurrentMarkers();

        //     foreach (var starChunkAbsKey in ChunkManager.Instance.CurrentStarChunks)
        //     {
        //         Vector2 miniMapPos = GetMinimapPosition(starChunkAbsKey);
        //         GameObject starMarker = Instantiate(starMarkerPrefab, panelTransform);
        //         starMarker.transform.localPosition = miniMapPos;

        //         if (starMarkers.ContainsKey(starChunkAbsKey)) return;
        //         starMarkers.Add(starChunkAbsKey, starMarker);
        //     }
        // }

        // private void ClearCurrentMarkers()
        // {
        //     foreach (var starMarker in starMarkers)
        //     {
        //         Destroy(starMarker.Value);
        //     }

        //     starMarkers.Clear();
        // }

        // Vector2 GetMinimapPosition(Vector2Int chunkKey)
        // {
        //     if (ChunkManager.Instance.ChunksDict[chunkKey].HasStarObject == false) return Vector2.zero;

        //     Vector2 starPosition = ChunkManager.Instance.ChunksDict[chunkKey].StarObject.transform.position;
        //     Vector2 playerWorldPos = player.position;
        //     Vector2 relativePos = starPosition - playerWorldPos;
            
        //     relativePos *= scaleFactor;
        //     return relativePos;
        // }
    }
}