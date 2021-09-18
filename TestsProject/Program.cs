using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using TracerProject;
using Newtonsoft.Json;

namespace TestsProject
{
    public class C
    {
        public ITracer _tracer;

        public C(ITracer tracer)
        {
            _tracer = tracer;
        }

        public void M0()
        {
            M1();
            M2();
        }

        private void M1()
        {
            _tracer.StartTrace();
            Thread.Sleep(100);
            _tracer.StopTrace();
        }

        private void M2()
        {
            _tracer.StartTrace();
            Thread.Sleep(200);
            _tracer.StopTrace();
        }
    }

    public class TestFramework
    {
        public static void Main(string[] args)
        {
            TimeTracer tr = new TimeTracer();
            C c = new C(tr);

            Task task = new Task(c.M0);
            task.Start();
            task.Wait();

            c.M0();


            Console.WriteLine("Finished");

            SerializatorProject.Serializator sr = new SerializatorProject.XMLSerializator();
            sr.doSerialize(c._tracer.GetTraceResult());
            Console.WriteLine(sr.Result);

            sr = new SerializatorProject.JsonSerializator();
            sr.doSerialize(c._tracer.GetTraceResult());
            Console.WriteLine(sr.Result);

            Console.ReadLine();
        }
    }
}
