using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TracerProject
{
    public interface ITracer{
        void StartTrace();
        void StopTrace();
        TraseResult GetTraseResult();
    }

    public class TraseResult {
        /* скоро определимся с интерфейсом */
    }
}
