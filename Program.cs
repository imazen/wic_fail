using ImageResizer.Plugins.WicBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WICFails
{
    class Program
    {
        static void Main(string[] args)
        {
            new WicFail().ExerciseWic("../../red-leaf.jpg");
            Console.ReadLine();
        }
    }
}
