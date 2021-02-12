using System;
using Mirror;

namespace Runtime
{
    [Serializable]
    public struct CharacterInfo
    {
        public string name;
        public string @class;
        public byte level;
    }
    
    public static class CharacterInfoReaderWriter
    {
        public static void WriteCharacterInfo(this NetworkWriter writer, CharacterInfo info)
        {
            writer.WriteString(info.name);
            writer.WriteString(info.@class);
            writer.WriteByte(info.level);
        }

        public static CharacterInfo ReadCharacterInfo(this NetworkReader reader)
        {
            CharacterInfo characterInfo = default;
            characterInfo.name = reader.ReadString();
            characterInfo.@class = reader.ReadString();
            characterInfo.level = reader.ReadByte();

            return characterInfo;
        }
    }
}