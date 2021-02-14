using System;

namespace Runtime.Models
{
    [Serializable]
    public class PlayerData
    {
        public string steamId;
        public string name;
        public int currencyAmount;
        public Stash stash;
        public CharacterInfo[] characters;
        public byte maxCharacterCount;
    }
}