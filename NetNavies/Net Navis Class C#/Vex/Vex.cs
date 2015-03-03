using Microsoft.VisualBasic;
using Net_Navis;
using Net_Navis.My;
using Net_Navis.My.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
namespace Vex
{
	static class Vex
	{
		const string Navi_Name = "Vex";

		private static long Navi_ID = Vex.My.Settings.NaviID;

		public static void Main()
		{
			Navi_Main Navi_Instance = new Navi_Main(Navi_Name, Navi_ID);
			Navi_Instance.Initialise();
			do {
				Application.DoEvents();
				Navi_Instance.DoEvents();
			} while (true);

		}

	}
}
