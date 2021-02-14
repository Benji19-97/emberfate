using System;
using Mirror;

namespace Runtime.Models
{
    [Serializable]
    public class CharacterInfo
    {
        public string characterName;
        public string characterId;
    }
    
    public static class CharacterInfoReaderWriter
    {
        public static void WriteCharacterInfo(this NetworkWriter writer, CharacterInfo info)
        {
            writer.WriteString(info.characterName);
            writer.WriteString(info.characterId);

        }

        public static CharacterInfo ReadCharacterInfo(this NetworkReader reader)
        {
            return new CharacterInfo()
            {
                characterName = reader.ReadString(),
                characterId = reader.ReadString()
            };
        }
    }
}