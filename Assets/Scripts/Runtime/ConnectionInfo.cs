using System;

namespace Runtime
{
    [Serializable]
    public struct ConnectionInfo
    {
        public string playerName;
        public string steamId;
        public byte maxCharacterCount;
        public CharacterInfo[] characterInfos;
    }
}