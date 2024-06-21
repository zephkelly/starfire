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
        [SerializeField] private float mapBounds = 10000f;
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
        }

        public void SetNewPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void UpdateMinimapMarkers(bool resetOrigin = false)
        {
            if (player == null) return;
            if (resetOrigin) return;
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
                    Vector2 miniMapPos = Vector2.zero;
                    
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

        public void UpdateMarkerPositions(bool resetOrigin = false)
        {
            if (resetOrigin) return;

            foreach (var starMarker in starMarkers)
            {
                Vector2 starPosition = ChunkManager.Instance.Chunks[starMarker.Key].GetStarPosition;
                Vector2 miniMapPos = GetMinimapPosition(starPosition);
                starMarker.Value.transform.localPosition = miniMapPos;
            }

            foreach (var planetMarker in planetMarkers)
            {
                Vector2 miniMapPos = Vector2.zero;

                if (planetMarker.Key.HasPlanetObject)
                {
                    miniMapPos = GetMinimapPosition(planetMarker.Key.GetRigidbody.position);
                }
                else
                {
                    miniMapPos = GetMinimapPosition(planetMarker.Key.GetOrbitPosition());
                }
                
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

            List<Planet> keysToRemovePlanet = new List<Planet>();

            foreach (var planetMarker in planetMarkers)
            {
                if (ChunkManager.Instance.CurrentStarChunks.Contains(planetMarker.Key.ParentChunk.CurrentWorldKey) == false)
                {
                    Destroy(planetMarker.Value);
                    keysToRemovePlanet.Add(planetMarker.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                starMarkers.Remove(key);
            }

            foreach (var key in keysToRemovePlanet)
            {
                planetMarkers.Remove(key);
            }
        }

        private bool IsWithinMapBounds(Vector2 objectPosition)
        {
            Vector2 relativePosition = GetRelativePosition(objectPosition);

            if (Mathf.Abs(relativePosition.x) > mapBounds || Mathf.Abs(relativePosition.y) > mapBounds)
            {
                return false;
            }

            return true;
        }

        private Vector2 GetMinimapPosition(Vector2 objectPosition)
        {
            Vector2 relativePosition = GetRelativePosition(objectPosition);
            
            relativePosition *= scaleFactor;
            return relativePosition;
        }

        private Vector2 GetRelativePosition(Vector2 objectPosition)
        {
            Vector2 playerWorldPos = player.position;
            Vector2 relativePos = objectPosition - playerWorldPos;
            return relativePos;
        }
    }
}