using System;

namespace Runtime
{
    [Serializable]
    public class ConnectionInfo
    {
        public string playerName;
        public string steamId;
        public byte maxCharacterCount;
        public CharacterInfo[] characterInfos;
    }
}