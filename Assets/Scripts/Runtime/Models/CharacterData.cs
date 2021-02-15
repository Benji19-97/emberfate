using System;
using System.IO;
using Mirror;

namespace Runtime.Models
{
    [Serializable]
    public class CharacterData
    {
        public string name;
        public byte level;
        public string @class;

        public byte[] Serialize()
        {
            NetworkWriter writer = new NetworkWriter();
            writer.WriteCharacterData(this);
            byte[] serialized = writer.ToArray();
            return serialized;
        }

        public static CharacterData Deserialize(byte[] data)
        {
            NetworkReader reader = new NetworkReader(data);
            return reader.ReadCharacterData();
        }
    }

    public static class CharacterDataReaderWriter
    {
        public static void WriteCharacterData(this NetworkWriter writer, CharacterData data)
        {
            writer.WriteString(data.name);
            writer.WriteByte(data.level);
            writer.WriteString(data.@class);
        }

        public static CharacterData ReadCharacterData(this NetworkReader reader)
        {
            CharacterData characterData = new CharacterData();
            characterData.name = reader.ReadString();
            characterData.level = reader.ReadByte();
            characterData.@class = reader.ReadString();
            return characterData;
        }
    }
}