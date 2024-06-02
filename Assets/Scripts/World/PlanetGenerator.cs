using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Starfire
{
    public class PlanetGenerator : MonoBehaviour
    {
        private GameObject planetPrefab_1;

        public void Start()
        {
            planetPrefab_1 = Resources.Load<GameObject>("Prefabs/Planets/RiversPlanet.prefab");
        }
        
    }
}