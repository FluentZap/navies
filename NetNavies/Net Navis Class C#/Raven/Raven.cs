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
namespace Raven
{
	class Raven
	{
		const string Navi_Name = "Raven";

        //public static long Navi_ID = Properties.Settings.Default.NaviID;

        public static long Navi_ID = 0;
		
        public static void Main()
		{
            //Assembly not found error handeler
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            Navi_Main Navi_Instance = new Navi_Main(Navi_Name, Navi_ID);
			Navi_Instance.Initialise();
			do {
				Application.DoEvents();
				Navi_Instance.DoEvents();
			} while (true);

		}

        //Loads in embeded resource file as assembly
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("EmbedAssembly.NetNaviClass.dll"))
            {
                byte[] assembltData = new byte[stream.Length];
                stream.Read(assembltData, 0, assembltData.Length);
                return System.Reflection.Assembly.Load(assembltData);
            }            
        }

	}
}
