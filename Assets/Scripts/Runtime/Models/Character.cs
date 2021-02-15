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
#if UNITY_SERVER || UNITY_EDITOR
            ServerLogger.LogMessage("trying to deserialize character: " + json, ServerLogger.LogType.Message);
#endif

            var jObject = JObject.Parse(json);
            var character = new Character
            {
                name = (string) jObject["name"],
                ownerSteamId = (string) jObject["ownerSteamId"],
                id = (string) jObject["_id"],
                data = CharacterData.Deserialize(jObject["data"]?["data"]?.ToObject<byte[]>())
            };
            
#if UNITY_SERVER || UNITY_EDITOR
            ServerLogger.LogMessage("character != null: " + (character != null).ToString(), ServerLogger.LogType.Message);
#endif
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