using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
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
        [SerializeField] private GameObject planetMarkerPrefab;

        private Dictionary<Vector2Int, GameObject> starMarkers = new Dictionary<Vector2Int, GameObject>();
        private Dictionary<Planet, GameObject> planetMarkers = new Dictionary<Planet, GameObject>();

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

        public void UpdateMinimapMarkers()
        {      
            ClearCurrentMarkers();

            UpdateStarMarkers();
            UpdatePlanetMarkers();
        }

        private void UpdateStarMarkers()
        {
            foreach (var starChunkAbsKey in ChunkManager.Instance.CurrentStarChunks)
            {
                Vector2 starPosition = ChunkManager.Instance.Chunks[starChunkAbsKey].GetStarPosition;
                Vector2 miniMapPos = GetMinimapPosition(starPosition);

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

        private void UpdatePlanetMarkers()
        {
            foreach (var planetChunkAbsKey in ChunkManager.Instance.CurrentStarChunks)
            {
                if (ChunkManager.Instance.Chunks[planetChunkAbsKey].HasPlanets is false) continue;

                foreach (var planet in ChunkManager.Instance.Chunks[planetChunkAbsKey].GetPlanets)
                {
                    Vector2 miniMapPos;
                    
                    if (planet.HasPlanetObject)
                    {
                        miniMapPos = GetMinimapPosition(planet.GetRigidbody.position);
                    }
                    else
                    {
                        miniMapPos = GetMinimapPosition(planet.GetOrbitPosition());
                    }


                    if (planetMarkers.ContainsKey(planet))
                    {
                        planetMarkers[planet].transform.localPosition = miniMapPos;
                        continue;
                    }

                    GameObject planetMarker = Instantiate(planetMarkerPrefab, panelTransform);
                    planetMarker.transform.localPosition = miniMapPos;

                    planetMarkers.Add(planet, planetMarker);
                }
            }
        }

        private void UpdateMarkerPositions()
        {
            foreach (var starMarker in starMarkers)
            {
                Vector2 starPosition = ChunkManager.Instance.Chunks[starMarker.Key].GetStarPosition;
                Vector2 miniMapPos = GetMinimapPosition(starPosition);
                starMarker.Value.transform.localPosition = miniMapPos;
            }

            foreach (var planetMarker in planetMarkers)
            {
                Vector2 miniMapPos = GetMinimapPosition(planetMarker.Key.GetOrbitPosition());
                planetMarker.Value.transform.localPosition = miniMapPos;
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

        private Vector2 GetMinimapPosition(Vector2 objectPosition)
        {
            Vector2 starPosition = objectPosition;
            Vector2 playerWorldPos = player.position;
            Vector2 relativePos = starPosition - playerWorldPos;
            
            relativePos *= scaleFactor;
            return relativePos;
        }
    }
}