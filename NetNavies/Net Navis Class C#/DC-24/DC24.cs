using Net_Navis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
namespace DC24
{
    class DC24
    {
        const int Navi_Name = (int)Navi_Name_ID.DC24;

        public static ulong NAVIEXEID = Properties.Settings.Default.NAVIEXEID;

        public static void Main()
        {
            Application.Exit();
            //Assembly resolve. Loads assemblys from resource files
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                String resourceName = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Name + "." + new AssemblyName(args.Name).Name + ".dll";                
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
            
            Run_Navi();
        }


        public static void Run_Navi()
        {
            Navi_Main Navi_Instance = new Navi_Main(Navi_Name, NAVIEXEID);
            Navi_Instance.Initialise();
            do
            {
                Application.DoEvents();                
            } while (!Navi_Instance.DoEvents());
        }
    }
}
