using System;
using System.Text;
using UnityEngine;

namespace Starfire.Core.V2.Save.Serialization
{
    public class JsonSaveSerializer : ISaveSerializer
    {
        private readonly bool _prettyPrint;

        public JsonSaveSerializer(bool prettyPrint = true)
        {
            _prettyPrint = prettyPrint;
        }

        public byte[] Serialize(SaveData data)
        {
            string json = JsonUtility.ToJson(data, _prettyPrint);
            return Encoding.UTF8.GetBytes(json);
        }

        public SaveData Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public SaveHeader DeserializeHeader(byte[] data)
        {
            // JsonUtility doesn't support partial parsing, so we deserialize the whole thing
            // and return just the header. For large files, consider a streaming JSON parser.
            string json = Encoding.UTF8.GetString(data);
            var save = JsonUtility.FromJson<SaveData>(json);
            return save.Header;
        }
    }
}
