using System;
using Mirror;

namespace Runtime.Models
{
    [Serializable]
    public class Stash
    {
        public string stashName;
        
        public static Stash Deserialize(byte[] data)
        {
            var reader = new NetworkReader(data);
            return reader.ReadStash();
        }

        public byte[] Serialize()
        {
            var writer = new NetworkWriter();
            writer.WriteStash(this);
            var serialized = writer.ToArray();
            return serialized;
        }
    }

    public static class StashReaderWriter
    {
        public static void WriteStash(this NetworkWriter writer, Stash stash)
        {
            writer.WriteString(stash.stashName);
        }

        public static Stash ReadStash(this NetworkReader reader)
        {
            var stash = new Stash();
            stash.stashName = reader.ReadString();
            return stash;
        }
    }
}