﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FbxBreak;
using System.IO;
using System.Reflection;
using FbxBreak.XFileWriter;

namespace FbxBreakUtil
{
    class Program
    {
        class MySaveHandler : BreakerSaveHandler
        {
            public MySaveHandler(string basePath)
            {
                BasePath = basePath;

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);
            }

            public override string ResolveOutputPath(string id, TransformGroup transform)
            {
                return BasePath + id + ".x";
            }

            public string BasePath { get; set; }
        }


        static void Main(string[] args)
        {
            Process();
        }

        private static void Process()
        {
            var br = Microsoft.DirectX.Direct3D.Device.IsUsingEventHandlers;

            //var pos = new List<Vect4>();

            //var angle = 0.0;
            //for (int i = 0; i < 60; i++)
            //{
            //    pos.Add(new Vect4() { x = Math.Cos(angle) * 100, y = 0, z = Math.Sin(angle) * 100, w = 1 });
            //    angle += Math.PI * 2 * (1 / 60.0f);
            //}

            //var writer = new FbxWriter();
            //writer.AppendSpline("Test", pos.ToArray(), true);
            //writer.Save(@"H:\Spline.fbx");

            var breaker = new FbxModelBreaker(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Sample\\Ship.fbx");
            breaker.Save(new MySaveHandler(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Sample\\Break\\Out_ship\\"), BreakerOutputFormat.X);


            foreach (var item in FbxModelBreaker.globalMessages)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Press Enter");
            Console.ReadLine();
        }
    }
}
