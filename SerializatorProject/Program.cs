using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TracerProject;

namespace SerializatorProject
{
    public abstract class Serializator
    {
        protected string _serializedResult;
        public string Result { get { return _serializedResult; } }
        public abstract void doSerialize(TracerProject.TraceResult traceResult);
    }

    public class XMLSerializator : Serializator
    {
        public override void doSerialize(TracerProject.TraceResult traceResult)
        {
            XmlSerializer formatter = new XmlSerializer(traceResult.GetType());
            using (var stringwriter = new System.IO.StringWriter())
            {
                formatter.Serialize(stringwriter, traceResult);
                _serializedResult = stringwriter.ToString();
            }
        }
    }

    public class JsonSerializator : Serializator
    {
        public override void doSerialize(TracerProject.TraceResult traceResult)
        {
            _serializedResult = JsonConvert.SerializeObject(traceResult, Newtonsoft.Json.Formatting.Indented);
        }
    }

    public abstract class SerializationWriter
    {
        public abstract void writeSerializedResult(Serializator ser);
    }

    public class ConsoleSerializationWriter : SerializationWriter
    {
        public override void writeSerializedResult(Serializator ser)
        {
            Console.WriteLine(ser.Result);
        }
    }

    public class FileSerializationWriter : SerializationWriter
    {
        private string filePath;

        public FileSerializationWriter(string pathToFile)
        {
            filePath = pathToFile;
        }

        public override void writeSerializedResult(Serializator ser)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                byte[] byteArray = Encoding.Default.GetBytes(ser.Result);
                fs.Write(Encoding.Default.GetBytes(ser.Result), 0, byteArray.Length);
            }
        }
    }





    public class CommonCase
    {
        static void Main()
        {
            Console.WriteLine("Общий случай работы библиотеки.");
            ITracer tr = new TimeTracer();
            C c = new C(tr);

            Task task = new Task(c.M0);
            task.Start();
            c.M0();
            task.Wait();


            TraceResult res = c._tracer.GetTraceResult();
            Serializator ser = new XMLSerializator();
            ser.doSerialize(res);

            SerializationWriter wsr = new ConsoleSerializationWriter();
            wsr.writeSerializedResult(ser);
            wsr = new FileSerializationWriter("result.xml");
            wsr.writeSerializedResult(ser);

            ser = new JsonSerializator();
            ser.doSerialize(res);

            Console.WriteLine("====================================================================================");

            wsr = new ConsoleSerializationWriter();
            wsr.writeSerializedResult(ser);
            wsr = new FileSerializationWriter("result.json");
            wsr.writeSerializedResult(ser);

            Console.ReadLine();
        }
    }

    class ZoZo
    {
        public ITracer _tracer;

        public ZoZo(ITracer tracer)
        {
            _tracer = tracer;
        }

        public void M5()
        {
            _tracer.StartTrace();
            Thread.Sleep(20);
            M8();
            _tracer.StopTrace();
        }

        public void M6()
        {
            _tracer.StartTrace();
            Thread.Sleep(20);
            _tracer.StopTrace();
        }

        public void M8()
        {
            _tracer.StartTrace();
            _tracer.StopTrace();
        }
    }

    public class C
    {
        public ITracer _tracer;
        private ZoZo z;

        public C(ITracer tracer)
        {
            _tracer = tracer;
            z = new ZoZo(_tracer);
        }

        public void M0()
        {
            M1();
            M2();
            z.M5();
            z.M6();
            z.M6();
            z.M5();
            M4();
        }

        private void M1()
        {
            _tracer.StartTrace();
            Thread.Sleep(100);
            M3();
            _tracer.StopTrace();
        }

        private void M2()
        {
            _tracer.StartTrace();
            Thread.Sleep(300);
            M3();
            _tracer.StopTrace();
        }

        public void M3()
        {
            _tracer.StartTrace();
            M4();
            Thread.Sleep(111);
            _tracer.StopTrace();
        }

        public void M4()
        {
            _tracer.StartTrace();
            z.M5();
            Thread.Sleep(20);
            z.M6();
            _tracer.StopTrace();
        }
    }
}
