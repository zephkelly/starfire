using System.IO;

namespace Starfire.Core.V2.Save.Serialization
{
    public interface ISaveSerializer
    {
        byte[] Serialize(SaveData data);
        SaveData Deserialize(byte[] data);
        SaveHeader DeserializeHeader(byte[] data);
    }
}
