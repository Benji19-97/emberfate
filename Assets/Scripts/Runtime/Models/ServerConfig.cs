using System;

namespace Runtime
{
    [Serializable]
    public class ServerConfig
    {
        public string name;
        public string status;
        public int maxConnections;
        public string location;
        public string ip;
        public int port;
    }
}