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
            Thread.Sleep(3000);
            StopTrace();
        }

        void m2()
        {
            StartTrace();
            Thread.Sleep(1500);
            m3();
            StopTrace();
        }

        void m1()
        {
            StartTrace();
            m2();
            Thread.Sleep(1000);
            m3();
            StopTrace();
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
            }
        }

        public void StopTrace()
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
                    string className = stackTrace.GetFrame(i).GetMethod().DeclaringType.FullName;
                    methodsClassesNames.Add(new Tuple<String, String>(className, methodName));
                }

                result.calculateMethodInformation(methodsClassesNames, index);
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
        public MethodInfo(string methodName, string className)
        {
            Methods = new List<MethodInfo>();
            Name = methodName;
            Class = className;
        }

        public int getIndexMethod(Tuple<String, String> methodClassName)
        {
            for (int i = 0; i < Methods.Count; ++i)
            {
                if (string.Equals(methodClassName.Item1, Methods[i].Name) &&
                    string.Equals(methodClassName.Item2, Methods[i].Class))
                {
                    return i;
                }
            }
            return -1;
        }

        public void startCountdown()
        {
            innerClock = new Stopwatch();
            innerClock.Start();
        }

        public void finishCountdown()
        {
            innerClock.Stop();
            Time = innerClock.ElapsedMilliseconds + "ms";
            Console.WriteLine(Class + " " + Name + " " + Time);
        }

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

        private Stopwatch innerClock = null;
    }

    [XmlRoot(ElementName = "thread")]
    public class ThreadInfo
    {
        public ThreadInfo(int id)
        {
            Methods = new List<MethodInfo>();
            Id = id.ToString();
        }

        public int getIndexMethod(Tuple<String, String> methodClassName)
        {
            for (int i = 0; i < Methods.Count; ++i)
            {
                if (string.Equals(methodClassName.Item1, Methods[i].Name) &&
                    string.Equals(methodClassName.Item2, Methods[i].Class))
                {
                    return i;
                }
            }
            return -1;
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
            ThreadInfo currThread = Threads[threadId];
            Tuple<String, String> nameClassTuple = methodsClassesNames[methodsClassesNames.Count - 1];

            int methodIndex = currThread.getIndexMethod(nameClassTuple);
            string methodName = nameClassTuple.Item1;
            string className  = nameClassTuple.Item2;

            if (methodIndex == -1)
            {
                currThread.Methods.Add(new MethodInfo(methodName, className));
                methodIndex = currThread.Methods.Count - 1;
            }

            MethodInfo methodRef = currThread.Methods[methodIndex];
            for (int i = methodsClassesNames.Count - 2; i >= 0; --i)
            {
                nameClassTuple = methodsClassesNames[i];
                methodName = nameClassTuple.Item1;
                className  = nameClassTuple.Item2;

                int nextMethodIndex = methodRef.getIndexMethod(methodsClassesNames[i]);
                if (nextMethodIndex == -1)
                {
                    methodRef.Methods.Add(new MethodInfo(methodName, className));
                    nextMethodIndex = methodRef.Methods.Count - 1;
                }

                methodRef = methodRef.Methods[nextMethodIndex];
            }

            methodRef.startCountdown();
        }

        public void calculateMethodInformation(List<Tuple<String, String>> methodsClassesNames, int threadId)
        {
            ThreadInfo currThread = Threads[threadId];
            Tuple<String, String> nameClassTuple = methodsClassesNames[methodsClassesNames.Count - 1];

            int methodIndex = currThread.getIndexMethod(nameClassTuple);
            string methodName = nameClassTuple.Item1;
            string className = nameClassTuple.Item2;

            MethodInfo methodRef = currThread.Methods[methodIndex];
            for (int i = methodsClassesNames.Count - 2; i >= 0; --i)
            {
                nameClassTuple = methodsClassesNames[i];
                methodName = nameClassTuple.Item1;
                className = nameClassTuple.Item2;

                int nextMethodIndex = methodRef.getIndexMethod(methodsClassesNames[i]);
                if (nextMethodIndex == -1)
                {
                    methodRef.Methods.Add(new MethodInfo(methodName, className));
                    nextMethodIndex = methodRef.Methods.Count - 1;
                }

                methodRef = methodRef.Methods[nextMethodIndex];
            }

            methodRef.finishCountdown();
        }
    }
}
