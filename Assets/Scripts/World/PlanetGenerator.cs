using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Starfire
{
    public class PlanetGenerator : MonoBehaviour
    {
        public static PlanetGenerator Instance { get; private set; }

        private GameObject riversPlanetPrefab;
        private GameObject desertPlanetPrefab;
        private GameObject gasRingPlanetPrefab;

        private ObjectPool<GameObject> riversPlanetPool;
        private ObjectPool<GameObject> desertPlanetPool;
        private ObjectPool<GameObject> gasRingPlanetPool;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }

            riversPlanetPrefab = Resources.Load<GameObject>("Prefabs/Planets/RiversPlanet");
            desertPlanetPrefab = Resources.Load<GameObject>("Prefabs/Planets/DesertPlanet");
            gasRingPlanetPrefab = Resources.Load<GameObject>("Prefabs/Planets/GasRingPlanet");

            CreatePlanetObjectPools();
        }

        public GameObject GetPlanetObject(PlanetType type)
        {
            switch (type)
            {
                case PlanetType.Rivers:
                    return riversPlanetPool.Get();
                case PlanetType.Desert:
                    return desertPlanetPool.Get();
                case PlanetType.GasLayers:
                    return gasRingPlanetPool.Get();
                default:
                    return riversPlanetPool.Get();
            }
        }
        
        public void ReturnPlanetObject(PlanetType type, GameObject planetObject)
        {
            switch (type)
            {
                case PlanetType.Rivers:
                    riversPlanetPool.Release(planetObject);
                    break;
                case PlanetType.Desert:
                    desertPlanetPool.Release(planetObject);
                    break;
                case PlanetType.GasLayers:
                    gasRingPlanetPool.Release(planetObject);
                    break;
                default:
                    riversPlanetPool.Release(planetObject);
                    break;
            }
        }

        public List<Planet> GetStarPlanets(Chunk _parentChunk, Star _star)
        {
            PlanetType[] allowablePlanetTypes = GetAllowablePlanetTypes(_star.StarType);
            List<Planet> planets = new List<Planet>();
            int planetCount = UnityEngine.Random.Range(2, 6);

            List<float> orbitDistances = GetAllOrbitDistances(_star, planetCount);

            // Create a planet for each orbit distance we made
            for (int i = 0; i < planetCount; i++)
            {
                planets.Add(CreatePlanet(_parentChunk, _star, allowablePlanetTypes, orbitDistances[i]));
            }

            return planets;
        }

        public Planet CreatePlanet(Chunk _parentChunk, Star _star, PlanetType[] _allowableTypes, float _orbitDistance)
        {
            PlanetType planetType = GetPlanetType(_star, _allowableTypes, _orbitDistance);
            return new Planet(_parentChunk, planetType, _orbitDistance);
        }

        private PlanetType[] GetAllowablePlanetTypes(StarType _starType)
        {
            // Different stars, different planets available
            switch (_starType)
            {
                case StarType.NeutronStar:
                    return new PlanetType[] { PlanetType.Land, PlanetType.Rivers, PlanetType.Desert, PlanetType.Ice, PlanetType.GasLayers };
                default:
                    return new PlanetType[] { PlanetType.Land, PlanetType.Rivers, PlanetType.Desert, PlanetType.Ice, PlanetType.GasLayers };
            }
        }

        private List<float> GetAllOrbitDistances(Star _star, int _planetCount)
        {
            float maxStarRadius = _star.Radius * 0.80f;
            float minStarRadius = _star.Radius * 0.15f;

            float partialRadius = maxStarRadius / _planetCount;
            float minDistanceBetweenPlanets = _star.Radius * 0.12f;

            List<float> orbitDistances = new List<float>();

            for (int i = 0; i < _planetCount; i++)
            {
                float minOrbitDistance = minStarRadius + (partialRadius * i);
                float maxOrbitDistance = minStarRadius + (partialRadius * (i + 1));

                if (i > 0)
                {
                    minOrbitDistance = Math.Max(minOrbitDistance, orbitDistances[i - 1] + minDistanceBetweenPlanets);
                }

                float orbitDistance = UnityEngine.Random.Range(minOrbitDistance, maxOrbitDistance);
                orbitDistances.Add(orbitDistance);
            }

            return orbitDistances;
        }

        
        private PlanetType GetPlanetType(Star _star, PlanetType[] _allowableTypes, float _planetOrbitDistance)
        {
            float starHotZone = _star.Radius * 0.15f;
            float starHabitableZone = _star.Radius * 0.5f;
            float starColdZone = _star.Radius * 0.7f;

            // Map each PlanetType to a range of allowable orbit distances
            Dictionary<PlanetType, (float min, float max)> planetTypeRanges = new Dictionary<PlanetType, (float min, float max)>
            {
                { PlanetType.Land, (starHabitableZone, starColdZone) },
                { PlanetType.Rivers, (starHabitableZone, starColdZone) },
                { PlanetType.Desert, (starHotZone, starHabitableZone) },
                { PlanetType.Ice, (starColdZone, _star.Radius) },
                { PlanetType.GasLayers, (starColdZone, _star.Radius) },
            };

            List<PlanetType> allowableTypes = new List<PlanetType>();
            foreach (PlanetType type in _allowableTypes)
            {
                if (planetTypeRanges.ContainsKey(type) && _planetOrbitDistance >= planetTypeRanges[type].min && _planetOrbitDistance < planetTypeRanges[type].max)
                {
                    allowableTypes.Add(type);
                }
            }

            if (allowableTypes.Count == 0)
            {
                Debug.LogWarning("No allowable planet types for this star");
                return PlanetType.Rivers;
            }

            int randomIndex = UnityEngine.Random.Range(0, allowableTypes.Count);
            return allowableTypes[randomIndex];
        }

        private void CreatePlanetObjectPools()
        {
            riversPlanetPool = new ObjectPool<GameObject>(() => 
            {
                return Instantiate(riversPlanetPrefab);
            }, _starObject => 
            {
                _starObject.SetActive(true);
            }, _starObject => 
            {
                _starObject.SetActive(false);
            }, _starObject => 
            {
                Destroy(_starObject);
            }, false, 2, 5);

            desertPlanetPool = new ObjectPool<GameObject>(() => 
            {
                return Instantiate(desertPlanetPrefab);
            }, _starObject => 
            {
                _starObject.SetActive(true);
            }, _starObject => 
            {
                _starObject.SetActive(false);
            }, _starObject => 
            {
                Destroy(_starObject);
            }, false, 2, 5);

            gasRingPlanetPool = new ObjectPool<GameObject>(() => 
            {
                return Instantiate(gasRingPlanetPrefab);
            }, _starObject => 
            {
                _starObject.SetActive(true);
            }, _starObject => 
            {
                _starObject.SetActive(false);
            }, _starObject => 
            {
                Destroy(_starObject);
            }, false, 2, 5);
        }
    }
}