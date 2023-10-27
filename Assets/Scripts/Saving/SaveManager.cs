using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Starfire.Generation;

namespace Starfire.IO
{
  public class SaveManager
  {
    public void SaveChunks(List<Chunk> inactiveChunks)
    {
      // ChunkListSerializable serializableChunkList = new ChunkListSerializable(inactiveChunks);

      // //Take SerializableChunkList and convert to JSON
      // string path = Application.persistentDataPath + "/chunks.json";
      // string json = JsonUtility.ToJson(serializableChunkList);

      // //Write JSON to file
      // if (File.Exists(path))
      // {
      //   File.AppendAllText(path, json);
      //   Debug.Log("File appended. New chunk count: " + inactiveChunks.Count + ".");
      // }
      // else
      // {
      //   File.WriteAllText(path, json);
      //   Debug.Log("File created. New chunk count: " + inactiveChunks.Count + ".");
      // }

      ChunkListSerializable serializableChunkList = new ChunkListSerializable(inactiveChunks);

      //Take SerializableChunkList and convert to JSON
      string path = Application.persistentDataPath + "/chunks.json";
      string json = JsonUtility.ToJson(serializableChunkList);

      //Read existing JSON data from file
      string existingJson = "";
      if (File.Exists(path))
      {
          existingJson = File.ReadAllText(path);
      }

      //Combine existing and new JSON data
      if (existingJson != "")
      {
          //Remove the closing square bracket from the existing JSON data
          existingJson = existingJson.Substring(0, existingJson.Length - 1);

          //Add a comma to separate the existing and new JSON data
          if (existingJson != "")
          {
            existingJson += ",";
          }

          //Add the new JSON data to the existing JSON data
          existingJson += json.Substring(1);
          json = existingJson;
      }

      //Write combined JSON data to file
      File.WriteAllText(path, json);
    }

    public List<Chunk> LoadChunks(List<Vector2Int> chunksToLoad)
    {
      string path = Application.persistentDataPath + "/chunks.json";

      if (File.Exists(path))
      {
        string json = File.ReadAllText(path);

        ChunkListSerializable savedChunks = JsonUtility.FromJson<ChunkListSerializable>(json);
        List<Chunk> chunks = savedChunks.GetChunkList();

        Debug.Log("File loaded. Chunk count: " + chunks.Count);
        return chunks;
      }
      else
      {
        Debug.LogError("Save file not found in " + path);
        return null;
      }
    }
  }
}