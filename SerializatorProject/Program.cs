using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
        
        static void Main(String[] args) { }
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
}
