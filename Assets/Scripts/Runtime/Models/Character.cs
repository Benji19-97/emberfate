using System;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runtime.Models
{
    [Serializable]
    public class Character
    {
        public static readonly string[] @Classes = {"Barbarian", "Sorceress", "Hunter"};

        public string id;
        public string ownerSteamId;
        public string name;
        public CharacterData data;

        public static Character Deserialize(string json)
        {
            var jObject = JObject.Parse(json);
            var character = new Character
            {
                name = (string) jObject["name"],
                ownerSteamId = (string) jObject["ownerSteamId"],
                id = (string) jObject["_id"],
                data = CharacterData.Deserialize(jObject["data"]?["data"]?.ToObject<byte[]>())
            };
            return character;
        }

        public string Serialize()
        {
            var character = new
            {
                ownerSteamId,
                name,
                data = data.Serialize()
            };

            return JsonConvert.SerializeObject(character);
        }
    }
}