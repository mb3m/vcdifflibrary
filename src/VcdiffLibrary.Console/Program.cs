using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VcdiffLibrary.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var orign = args[0];
            var diff = args[1];
            var target = args[2];

            var decoder = new VcdiffDecoder(File.OpenRead(orign), File.OpenRead(diff), File.OpenWrite(target));
            decoder.Decode();
        }
    }
}
