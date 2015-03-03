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
namespace Rebel
{
	static class Rebel
	{
		const string Navi_Name = "Rebel";		

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
