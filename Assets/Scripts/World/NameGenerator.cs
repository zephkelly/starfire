using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public interface JsonData
    {
        public string[] items { get; set; }
    }

    [SerializeField]
    public class StarDataWrapper
    {
        public string[] items;
    }

    public class NameGenerator
    {
        private string[] LoadJson(string path)
        {
            TextAsset json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError("Failed to load JSON");
                return null;
            }

            StarDataWrapper data = JsonUtility.FromJson<StarDataWrapper>(json.text);
            if (data == null || data.items.Length == 0)
            {
                Debug.LogError("No data found or JSON is malformed");
                return null;
            }

            return data.items;
        }

        private string Capitalise(string input)
        {
            return input.Substring(0, 1).ToUpper() + input.Substring(1);
        }

        public string GetStarName()
        {
            var prefix = LoadJson("Data/star/starPrefixes");
            var stems = LoadJson("Data/star/starStems");
            var suffix = LoadJson("Data/star/starSuffixes");
            var tagsFront = LoadJson("Data/star/starTagsFront");
            var tagsBack = LoadJson("Data/star/starTagsBack");

            var randPrefix = prefix[Random.Range(0, prefix.Length)];
            var randStem = stems[Random.Range(0, stems.Length)];
            var randSuffix = suffix[Random.Range(0, suffix.Length)];
            var randTagFront = tagsFront[Random.Range(0, tagsFront.Length)];
            var randTagBack = tagsBack[Random.Range(0, tagsBack.Length)];

            var nameType = Random.Range(0, 16);

            if (nameType >= 16)
            {
                return randTagFront + " " + Capitalise(randStem);
            }
            else if (nameType >= 15)
            {
                return Capitalise(randStem) + "-" + Capitalise(randSuffix);
            }
            else if (nameType >= 10)
            {
                return randPrefix + randStem + " " + randTagBack;
            }
            else if (nameType >= 6)
            {
                return randTagFront + " " + randPrefix + "-" + Capitalise(randStem);
            }
            else if (nameType >= 3)
            {
                return Capitalise(randStem) + "-" + Capitalise(randSuffix) + " " + randTagBack;
            }
            else if (nameType == 2)
            { 
                return randPrefix + randSuffix + " " + randTagBack;
            }
            else
            {
                return randPrefix + "-" + Capitalise(randStem);
            }
        }
    }
}
