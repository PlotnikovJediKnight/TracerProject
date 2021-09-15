using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.Json.Serialization;

namespace TracerProject
{
    public interface ITracer{
        void StartTrace();
        void StopTrace();
        TraseResult GetTraseResult();
    }

    public class TimeTracer : ITracer
    {
        private readonly object balanceLock = new object();
        public void StartTrace()
        {
            lock (balanceLock)
            {

            }
        }
        public void StopTrace()
        {
        }
        public TraseResult GetTraseResult()
        {
            return null;
        }

    }

    [XmlRoot(ElementName = "method")]
    public class MethodInfo
    {
        [XmlAttribute(AttributeName = "name")]
        [JsonPropertyNameAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "time")]
        [JsonPropertyNameAttribute("time")]
        public string Time { get; set; }

        [XmlAttribute(AttributeName = "class")]
        [JsonPropertyNameAttribute("class")]
        public string Class { get; set; }

        [XmlElement(ElementName = "method")]
        [JsonPropertyNameAttribute("method")]
        public List<MethodInfo> Methods { get; set; }
    }

    [XmlRoot(ElementName = "thread")]
    public class ThreadInfo
    {
        [XmlElement(ElementName = "method")]
        [JsonPropertyNameAttribute("method")]
        public List<MethodInfo> Methods { get; set; }

        [XmlAttribute(AttributeName = "id")]
        [JsonPropertyNameAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "time")]
        [JsonPropertyNameAttribute("time")]
        public string Time { get; set; }
    }


    [XmlRoot(ElementName = "traceResult")]
    public class TraseResult {

        [XmlElement(ElementName = "thread")]
        [JsonPropertyNameAttribute("thread")]
        public List<ThreadInfo> Threads { get; set; }
    }
}
