using Microsoft.VisualBasic;
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
	static class Raven
	{
		const string Navi_Name = "Raven";

		//private static long Navi_ID = Raven.My.Settings.NaviID;

		public static void Main()
		{
			Navi_Main Navi_Instance = new Navi_Main(Navi_Name, 0);

			Navi_Instance.Initialise();
			do {
				Application.DoEvents();
				Navi_Instance.DoEvents();
			} while (true);

		}

	}
}
