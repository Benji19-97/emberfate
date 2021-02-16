using System;
using Mirror;

namespace Runtime.Models
{
    [Serializable]
    public class CharacterData
    {
        public string name;
        public byte level;
        public string @class;
        public bool isHardcore;
        public int deathCount;
        public int season;

        public byte[] Serialize()
        {
            var writer = new NetworkWriter();
            writer.WriteCharacterData(this);
            var serialized = writer.ToArray();
            return serialized;
        }

        public static CharacterData Deserialize(byte[] data)
        {
            var reader = new NetworkReader(data);
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

            try
            {
                writer.WriteBoolean(data.isHardcore);
            }
            catch (Exception e)
            {
                writer.WriteBoolean(false);
            }

            try
            {
                writer.WriteInt32(data.deathCount);
            }
            catch (Exception e)
            {
                writer.WriteInt32(0);
            }
            
            try
            {
                writer.WriteInt32(data.season);
            }
            catch (Exception e)
            {
                writer.WriteInt32(0);
            }
        }

        public static CharacterData ReadCharacterData(this NetworkReader reader)
        {
            var characterData = new CharacterData();
            characterData.name = reader.ReadString();
            characterData.level = reader.ReadByte();
            characterData.@class = reader.ReadString();

            try
            {
                characterData.isHardcore = reader.ReadBoolean();
            }
            catch (Exception e)
            {
                characterData.isHardcore = false;
            }

            try
            {
                characterData.deathCount = reader.ReadInt32();
            }
            catch (Exception e)
            {
                characterData.deathCount = 0;
            }

            try
            {
                characterData.season = reader.ReadInt32();
            }
            catch (Exception e)
            {
                characterData.season = 0;
            }
            
            return characterData;
        }
    }
}