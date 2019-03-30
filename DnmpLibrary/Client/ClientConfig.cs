using System;

namespace DnmpLibrary.Client
{
    [ValidableConfig]
    public class ClientConfig
    {
        [ValidableField("(\\d{1,})")]
        public int MaxReliableRetries = 8;

        [ValidableField("(\\d{1,})")]
        public int MaxPingAnswerTime = 10000;

        [ValidableField("(\\d{1,})")]
        public int HeartbeatDelay = 1000;

        [ValidableField("(\\d{1,})")]
        public int ConnectionTimeout = 3000;

        [ValidableField("(\\d{1,})")]
        public int TokenSize = 16;

        [ValidableField("(\\d{1,})")]
        public int ClientTimeout = 3000;

        [ValidableField("(\\d{1,})")]
        public int PingSize = 16;

        [ValidableField("(\\d{1,})")]
        public int PingUpdateTimerStartDelay = 3000;

        [ValidableField("(\\d{1,})")]
        public int PingUpdateTimerDelay = 30000;

        [ValidableField("(\\d{1,})")]
        public int ReconnectionTimeout = 10000;

        [ValidableField("(\\d{1,})")]
        public int RebalancingTimeout = 10000;
    }

    public class ValidableConfigAttribute : Attribute {  }

    public class ValidableFieldAttribute : Attribute
    {
        public string Regex;

        public ValidableFieldAttribute(string regex)
        {
            Regex = regex;
        }
    }
}
