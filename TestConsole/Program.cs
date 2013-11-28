using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using NationalInstruments;
using NationalInstruments.VisaNS;
using NationalInstruments.NI4882;
using InstrumentsDotNetNative;

namespace TestConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            HP8753DotNet hp8753;

            Console.WriteLine("Application Init....");           

            try
            {
                hp8753 = new HP8753DotNet(24);
                hp8753.VISAtoGPIBTimeout(10000);
                hp8753.Initialize();
                hp8753.setNumPoints(201);
                hp8753.setDataTransferFormat("FORM4");                
                hp8753.isRemote = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                Console.ReadKey();
                return -1;
            }

            Console.WriteLine("Init successfull\nReading data from instrument...");

            try
            {
                string raw = hp8753.getRawMeasurementData();
                char[] delimiter = { ',' };
                string[] data = raw.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine("Printing the data in form");
                uint n = (uint)data.Count();
                uint i = 0;
                while (n >= 0)
                {
                    Console.WriteLine("\t{0}\t{1}", data[i], data[i + 1]);
                    i += 2;
                    n -= 2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                Console.ReadKey();
                return -2;
            }
            Console.ReadKey();
            Console.WriteLine("Processing ended, releasing instrument");
            hp8753.isRemote = false;
            hp8753.GoToLocal();


            return 0;
        }
    }
}
