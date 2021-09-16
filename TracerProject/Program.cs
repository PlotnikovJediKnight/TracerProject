using System;
using System.Diagnostics;
using System.Threading;
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
        void m3()
        {
            StartTrace();
        }

        void m2()
        {
            StartTrace();
            m3();
        }

        void m1()
        {
            StartTrace();
            m2();
        }

        static void Main(string[] args)
        {
            new TimeTracer().m1();
            Console.ReadLine();
        }

        private readonly object balanceLock = new object();
        private TraseResult result = new TraseResult();

        public void StartTrace()
        {
            lock (balanceLock)
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                int index = result.getThreadIndex(threadId);

                StackTrace stackTrace = new StackTrace(false);
                List<Tuple<String, String>> methodsClassesNames = new List<Tuple<String, String>>(); 
                for (int i = 1; i < stackTrace.FrameCount; ++i)
                {
                    string methodName = stackTrace.GetFrame(i).GetMethod().Name;
                    string className  = stackTrace.GetFrame(i).GetMethod().DeclaringType.FullName;
                    methodsClassesNames.Add(new Tuple<String, String>(className, methodName));
                }

                if (index == -1)
                {
                    result.Threads.Add(new ThreadInfo(threadId));
                    index = result.Threads.Count - 1;
                }

                result.insertMethodInformation(methodsClassesNames, index);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
            }
        }

        public void StopTrace()
        {
            lock (balanceLock)
            {

            }
        }

        public TraseResult GetTraseResult()
        {
            return result;
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
        public ThreadInfo(int id)
        {
            Methods = new List<MethodInfo>();
            Id = id.ToString();
        }

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

        public TraseResult()
        {
            Threads = new List<ThreadInfo>();
        }

        public int getThreadIndex(int id)
        {
            string id_string = id.ToString();
            for(int i = 0; i < Threads.Count; ++i)
            {
                if (Threads[i].Id == id_string)
                {
                    return i;
                }
            }
            return -1;
        }

        public void insertMethodInformation(List<Tuple<String, String>> methodsClassesNames, int threadId)
        {

        }
    }
}
