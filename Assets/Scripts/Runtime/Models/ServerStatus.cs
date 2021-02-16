using System;

namespace Runtime.Models
{
    [Serializable]
    public class ServerStatus
    {
        public string name;
        public string status;
        public int maxConnections;
        public string location;
        public string ip;
        public ushort port;
    }
}