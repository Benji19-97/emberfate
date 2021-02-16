using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runtime.Helpers;

namespace Runtime.Models
{
    [Serializable]
    public class Profile
    {
        public string steamId;
        public string name;
        public int currencyAmount;
        public Stash stash;
        public CharacterInfo[] characters;
        public byte maxCharacterCount;
        public bool @private;

        [NonSerialized] public Character PlayingCharacter = null;

        public string Serialize()
        {
            var profile = new
            {
                steamId,
                name,
                currencyAmount,
                characters,
                maxCharacterCount,
                @private
            };
            return JsonConvert.SerializeObject(profile);
        }
        
        public static Profile Deserialize(string json)
        {
            // ServerLogger.LogWarning("Deserializing profile: " + json);
            
            var jObject = JObject.Parse(json);
            var profile = new Profile()
            {
                steamId = (string) jObject["steamId"],
                name = (string) jObject["name"],
                maxCharacterCount = Convert.ToByte(jObject["maxCharacterCount"]),
                characters =  jObject["characters"].ToObject<CharacterInfo[]>(),
                currencyAmount = Convert.ToInt32(jObject["currencyAmount"]),
                stash = Stash.Deserialize(jObject["stash"]?["data"]?.ToObject<byte[]>()),
                @private = Convert.ToBoolean(jObject["private"])
            };
            return profile;
        }
    }
}