using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Starfire
{
    public class PlanetGenerator : MonoBehaviour
    {
        private GameObject planetPrefab_1;

        private ObjectPool<GameObject> planetPool;

        public ObjectPool<GameObject> GetPlanetPool { get => planetPool; }

        private void Awake()
        {
            planetPrefab_1 = Resources.Load<GameObject>("Prefabs/Planets/RiversPlanet");

            planetPool = new ObjectPool<GameObject>(() => 
            {
                return Instantiate(planetPrefab_1);
            }, _starObject => 
            {
                _starObject.SetActive(true);
            }, _starObject => 
            {
                _starObject.SetActive(false);
            }, _starObject => 
            {
                Destroy(_starObject);
            }, false, 5, 10);
        }

        private void Start()
        {
        }

        public List<Planet> GetStarPlanets(Star _star)
        {
            List<Planet> planets = new List<Planet>();
            int planetCount = Random.Range(0, 6);

            List<float> orbitDistances = GetOrbitDistances(_star, planetCount);

            for (int i = 0; i < planetCount; i++)
            {
                planets.Add(GeneratePlanet(_star, orbitDistances[i]));
            }

            return planets;
        }

        public Planet GeneratePlanet(Star _star, float _orbitDistance)
        {
            PlanetType[] allowablePlanetTypes = GetPlanetTypes(_star.GetStarType);
            PlanetType planetType = GetPlanetType(_star, allowablePlanetTypes, _orbitDistance);

            // Generate a planet
            Planet planet = new Planet(planetType, _orbitDistance);
            return planet;
        }

        private PlanetType[] GetPlanetTypes(StarType _starType)
        {
            switch (_starType)
            {
                case StarType.NeutronStar:
                    return new PlanetType[] { PlanetType.Land, PlanetType.Rivers, PlanetType.Desert };
                default:
                    return new PlanetType[] { PlanetType.Land, PlanetType.Rivers, PlanetType.Desert };
            }
        }

        private List<float> GetOrbitDistances(Star _star, int _planetCount)
        {
            float maxStarRadius = _star.GetRadius * 0.85f;
            float minStarRadius = _star.GetRadius * 0.15f;

            float partialRadius = maxStarRadius / _planetCount;

            List<float> orbitDistances = new List<float>();

            for (int i = 0; i < _planetCount; i++)
            {
                float orbitDistance = Random.Range(minStarRadius + (partialRadius * i), minStarRadius + (partialRadius * (i + 1)));
                orbitDistances.Add(orbitDistance);
            }

            return orbitDistances;
        }

        
        private PlanetType GetPlanetType(Star _star, PlanetType[] _alloweableTypes, float _orbitDistance)
        {
            // get all of the allowable types as a list
            List<PlanetType> allowableTypes = new List<PlanetType>(_alloweableTypes);

            if (allowableTypes.Contains(PlanetType.Land))
            {
                if (_orbitDistance < _star.GetRadius * 0.25f)
                {
                    return PlanetType.Land;
                }
                else if (_orbitDistance < _star.GetRadius * 0.5f)
                {
                    return PlanetType.Land;
                }
                else if (_orbitDistance < _star.GetRadius * 0.75f)
                {
                    return PlanetType.Land;
                }
                else
                {
                    return PlanetType.Land;
                }
            }
            else if (allowableTypes.Contains(PlanetType.Rivers))
            {
                if (_orbitDistance < _star.GetRadius * 0.25f)
                {
                    return PlanetType.Rivers;
                }
                else if (_orbitDistance < _star.GetRadius * 0.5f)
                {
                    return PlanetType.Rivers;
                }
                else if (_orbitDistance < _star.GetRadius * 0.75f)
                {
                    return PlanetType.Rivers;
                }
                else
                {
                    return PlanetType.Rivers;
                }
            }

            return PlanetType.None;
        }
    }
}