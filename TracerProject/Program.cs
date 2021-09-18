using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.Json.Serialization;

namespace TracerProject
{

    #region ITracer
    public interface ITracer{
        void StartTrace();
        void StopTrace();
        TraceResult GetTraceResult();
    }
    #endregion

    public class Foo
    {
        public Bar _bar;
        public ITracer _tracer;

        internal Foo(ITracer tracer)
        {
            _tracer = tracer;
            _bar = new Bar(_tracer);
        }

        public void MyMethod()
        {
            _tracer.StartTrace();
            Thread.Sleep(230);
            _bar.InnerMethod();
            Thread.Sleep(30);
            _tracer.StopTrace();
        }
    }

    public class Bar
    {
        public ITracer _tracer;

        internal Bar(ITracer tracer)
        {
            _tracer = tracer;
        }

        public void InnerMethod()
        {
            _tracer.StartTrace();
            _tracer.StopTrace();
        }
    }


    #region TimeTracer
    public class TimeTracer : ITracer
    {
        static void Main(string[] args)
        {
            TimeTracer tr = new TimeTracer();
            Foo c = new Foo(tr);

            Task task = new Task(c.MyMethod);
            task.Start();
            task.Wait();

            c.MyMethod();

            

            Console.WriteLine("Finished");

            XmlSerializer formatter = new XmlSerializer(typeof(TraceResult));
            using (FileStream fs = new FileStream("result.xml", FileMode.Create))
            {
                formatter.Serialize(fs, c._tracer.GetTraceResult());

                Console.WriteLine("Объект сериализован");
            }

            Console.ReadLine();
        }

        private List<Tuple<String, String>> getMethodsClassesList()
        {
            StackTrace stackTrace = new StackTrace(false);
            List<Tuple<String, String>> methodsClassesNames = new List<Tuple<String, String>>();
            for (int i = 2; i < stackTrace.FrameCount - 1; ++i)
            {
                string methodName = stackTrace.GetFrame(i).GetMethod().Name;
                if ("InnerInvoke".Equals(methodName)) break;
                string className = stackTrace.GetFrame(i).GetMethod().DeclaringType.FullName;
                methodsClassesNames.Add(new Tuple<String, String>(className, methodName));
            }
            return methodsClassesNames;
        }

        public void StartTrace()
        {
            lock (balanceLock)
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                int index = result.getThreadIndex(threadId);

                List<Tuple<String, String>> methodsClassesNames = getMethodsClassesList(); 

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

                List<Tuple<String, String>> methodsClassesNames = getMethodsClassesList();

                result.calculateMethodInformation(methodsClassesNames, index);
            }
        }

        public TraceResult GetTraceResult()
        {
            for (int i = 0; i < result.Threads.Count; ++i)
            {
                result.Threads[i].countThreadTimeTotal();
            }
            return result;
        }

        private readonly object balanceLock = new object();
        private TraceResult result = new TraceResult();
    }
    #endregion

    #region MethodInfo
    [XmlRoot(ElementName = "method")]
    public class MethodInfo
    {
        private static int innerCount = 0;
        public MethodInfo() { }
        public MethodInfo(string methodName, string className)
        {
            Methods = new List<MethodInfo>();
            Name = methodName;
            Class = className;
            innerId = innerCount++;
        }

        public int getIndexMethod(Tuple<String, String> methodClassName)
        {
            for (int i = 0; i < Methods.Count; ++i)
            {
                if (string.Equals(methodClassName.Item1, Methods[i].Class) &&
                    string.Equals(methodClassName.Item2, Methods[i].Name)  &&
                    Methods[i].innerId != -1)
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
            innerId = -1;
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

        public int getInnerId() { return innerId; }
        public Stopwatch getInnerClock() { return innerClock; }
        public long getInnerClockTime() { if (innerClock == null) return 0; else return innerClock.ElapsedMilliseconds; }

        private Stopwatch innerClock = null;
        private int innerId;
    }
    #endregion MethodInfo

    #region ThreadInfo
    [XmlRoot(ElementName = "thread")]
    public class ThreadInfo
    {
        public ThreadInfo() { }
        public ThreadInfo(int id)
        {
            Methods = new List<MethodInfo>();
            Id = id.ToString();
        }

        public int getIndexMethod(Tuple<String, String> methodClassName)
        {
            for (int i = 0; i < Methods.Count; ++i)
            {
                if (string.Equals(methodClassName.Item1, Methods[i].Class) &&
                    string.Equals(methodClassName.Item2, Methods[i].Name) &&
                    Methods[i].getInnerId() != -1)
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

        public void countThreadTimeTotal()
        {
            long total = 0;
            for (int i = 0; i < Methods.Count; ++i)
            {
                MethodInfo currMethod = Methods[i];
                if (currMethod.getInnerClock() == null)
                {
                    List<MethodInfo> nextBatch = currMethod.Methods;
                    this.Methods.RemoveAt(i);
                    this.Methods.InsertRange(i, nextBatch);
                    i = i - 1;
                }
                else
                {
                    total += currMethod.getInnerClockTime();
                }
            }
            Time = total.ToString() + "ms";
        }

    }
    #endregion ThreadInfo

    #region TraceResult
    [XmlRoot(ElementName = "traceResult")]
    [Serializable]
    public class TraceResult {

        [XmlElement(ElementName = "thread")]
        [JsonPropertyNameAttribute("thread")]
        public List<ThreadInfo> Threads { get; set; } = new List<ThreadInfo>();

        public TraceResult() { }

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

        public MethodInfo getFinalMethodRef(List<Tuple<String, String>> methodsClassesNames, MethodInfo methodRef)
        {
            for (int i = methodsClassesNames.Count - 2; i >= 0; --i)
            {
                Tuple<String, String> nameClassTuple = methodsClassesNames[i];
                string className = nameClassTuple.Item1;
                string methodName = nameClassTuple.Item2;

                int nextMethodIndex = methodRef.getIndexMethod(methodsClassesNames[i]);
                if (nextMethodIndex == -1)
                {
                    methodRef.Methods.Add(new MethodInfo(methodName, className));
                    nextMethodIndex = methodRef.Methods.Count - 1;
                }

                methodRef = methodRef.Methods[nextMethodIndex];
            }
            return methodRef;
        }

        public void insertMethodInformation(List<Tuple<String, String>> methodsClassesNames, int threadId)
        {
            ThreadInfo currThread = Threads[threadId];
            Tuple<String, String> nameClassTuple = methodsClassesNames[methodsClassesNames.Count - 1];

            int methodIndex = currThread.getIndexMethod(nameClassTuple);
            string className  = nameClassTuple.Item1;
            string methodName = nameClassTuple.Item2;

            if (methodIndex == -1)
            {
                currThread.Methods.Add(new MethodInfo(methodName, className));
                methodIndex = currThread.Methods.Count - 1;
            }

            MethodInfo methodRef = getFinalMethodRef(methodsClassesNames, currThread.Methods[methodIndex]);

            methodRef.startCountdown();
        }

        public void calculateMethodInformation(List<Tuple<String, String>> methodsClassesNames, int threadId)
        {
            ThreadInfo currThread = Threads[threadId];
            Tuple<String, String> nameClassTuple = methodsClassesNames[methodsClassesNames.Count - 1];

            int methodIndex = currThread.getIndexMethod(nameClassTuple);
            string className = nameClassTuple.Item1;
            string methodName = nameClassTuple.Item2;

            MethodInfo methodRef = getFinalMethodRef(methodsClassesNames, currThread.Methods[methodIndex]);

            methodRef.finishCountdown();
        }
    }
    #endregion TraceResult
}
