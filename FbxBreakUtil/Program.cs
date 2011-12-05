using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FbxBreak;
using System.IO;
using System.Reflection;

namespace FbxBreakUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            var breaker = new FbxModelBreaker(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Sample\\Ship.fbx");
            breaker.Save(null, BreakerOutputFormat.Fbx);

            foreach (var item in FbxModelBreaker.globalMessages)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Press Enter");
            Console.ReadLine();
        }
    }
}
