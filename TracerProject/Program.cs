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
    class B
    {
        public ITracer t = new TimeTracer();
        public void foo3() { t.StartTrace(); Thread.Sleep(10); t.StopTrace(); }
        public void foo2() { t.StartTrace(); foo3(); t.StopTrace(); }
        public void foo1() { t.StartTrace(); foo2(); Thread.Sleep(20); foo3(); t.StopTrace(); }
        public void foo0() { t.StartTrace(); foo1(); t.StopTrace(); }
    }

    class A
    {
        public B obj;

        public B getInnerObject() { return obj; }
    }

    #region ITracer
    public interface ITracer{
        void StartTrace();
        void StopTrace();
        TraceResult GetTraceResult();
    }
    #endregion

    #region TimeTracer
    public class TimeTracer : ITracer
    {
        static void Main(string[] args)
        {
            A obj1 = new A();
            obj1.obj = new B();

            //Task task1 = new Task(obj1.obj.foo0);
            //task1.Start();

            obj1.obj.foo0();
            //obj1.obj.foo2();
            //obj1.obj.foo2();
            obj1.obj.foo0();

            obj1.obj.foo1();
            obj1.obj.foo1();

            //task1.Wait();
            Console.WriteLine("Finished");

            XmlSerializer formatter = new XmlSerializer(typeof(TraceResult));

            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream("result.xml", FileMode.Create))
            {
                formatter.Serialize(fs, obj1.obj.t.GetTraceResult());

                Console.WriteLine("Объект сериализован");
            }

            Console.ReadLine();
        }

        private List<Tuple<String, String>> getMethodsClassesList()
        {
            StackTrace stackTrace = new StackTrace(false);
            List<Tuple<String, String>> methodsClassesNames = new List<Tuple<String, String>>();
            string classWrapperName = stackTrace.GetFrame(2).GetMethod().DeclaringType.FullName;
            for (int i = 2; i < stackTrace.FrameCount - 1; ++i)
            {
                string methodName = stackTrace.GetFrame(i).GetMethod().Name;
                if ("InnerInvoke".Equals(methodName)) break;
                string className = stackTrace.GetFrame(i).GetMethod().DeclaringType.FullName;
                if (!classWrapperName.Equals(className)) break;
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
                    string.Equals(methodClassName.Item2, Methods[i].Name))
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
