using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    [SerializeField]
    public class StarPrefixes
    {
        public string[] items;
    }

    [SerializeField]
    public class StarSufixes
    {
        public string[] items;
    }

    public class NameGenerator
    {
        private StarSufixes LoadStarSufix()
        {
            TextAsset sufixJson = Resources.Load<TextAsset>("Data/starSufixes");
            if (sufixJson == null)
            {
                Debug.LogError("Failed to load star sufixes JSON");
                return null;
            }

            StarSufixes data = JsonUtility.FromJson<StarSufixes>(sufixJson.text);
            if (data == null || data.items == null|| data.items.Length == 0)
            {
                Debug.LogError("No star sufixes found or JSON is malformed");
                return null;
            }

            return data;
        }

        private StarPrefixes LoadStarPrefix()
        {
            TextAsset prefixJson = Resources.Load<TextAsset>("Data/starPrefixes");
            if (prefixJson == null)
            {
                Debug.LogError("Failed to load star prefixes JSON");
                return null;
            }

            StarPrefixes data = JsonUtility.FromJson<StarPrefixes>(prefixJson.text);
            if (data == null || data.items == null || data.items.Length == 0)
            {
                Debug.LogError("No star prefixes found or JSON is malformed");
                return null;
            }

            return data;
        }


        public string GetStarName()
        {
            var prefix = LoadStarPrefix();
            var sufix = LoadStarSufix();

            var randName = prefix.items[Random.Range(0, prefix.items.Length)];
            var randName2 = prefix.items[Random.Range(0, prefix.items.Length)];
            var randName3 = prefix.items[Random.Range(0, prefix.items.Length)];

            var randSufix = sufix.items[Random.Range(0, sufix.items.Length)];

            var nameType = Random.Range(0, 10);

            if (nameType >= 6)
            {
                return randName;
            }
            else if (nameType >= 3)
            {
                return randName + " " + randSufix;
            }
            else if (nameType == 1)
            {
                return randName + "-" + randName2;
            }
            else
            {
                return randName + "-" + randName3 + " " + randSufix;
            }
        }
    }
}
