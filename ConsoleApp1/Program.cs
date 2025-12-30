using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerProcess;

namespace ConsoleApp1 {
     class Program {
        static void Main(string[] args) {
           WUTestProcess wUTestProcess = new WUTestProcess();
           wUTestProcess.StatTest(true);    
        }

    }
}
