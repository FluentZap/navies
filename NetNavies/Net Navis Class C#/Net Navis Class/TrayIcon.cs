using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
namespace Net_Navis
{
	class NaviTrayIcon : ApplicationContext
	{
		public NotifyIcon tray = new NotifyIcon();
		public ContextMenuStrip MainMenu;
		private ToolStripMenuItem withEventsField_mnuHideNavi;
		public ToolStripMenuItem mnuHideNavi {
			get { return withEventsField_mnuHideNavi; }
			set {
				if (withEventsField_mnuHideNavi != null) {
					withEventsField_mnuHideNavi.Click -= mnuDisplayForm_Click;
				}
				withEventsField_mnuHideNavi = value;
				if (withEventsField_mnuHideNavi != null) {
					withEventsField_mnuHideNavi.Click += mnuDisplayForm_Click;
				}
			}
		}
		public ToolStripSeparator mnuSep1;

		public ToolStripMenuItem mnuExit;

		public void Initialise(NetNavi_Type Navi)
		{
			mnuHideNavi = new ToolStripMenuItem("Hide Navi");
			mnuSep1 = new ToolStripSeparator();
			mnuExit = new ToolStripMenuItem("Close");
			MainMenu = new ContextMenuStrip();
			MainMenu.Items.AddRange(new ToolStripItem[] {
				mnuHideNavi,
				mnuSep1,
				mnuExit
			});

			tray = new NotifyIcon();
			tray.Icon = Navi.Icon;
			tray.ContextMenuStrip = MainMenu;
			tray.Text = Navi.Navi_Name + " Tray Icon Service";
			tray.Visible = true;
		}


		private void AppContext_ThreadExit(object sender, System.EventArgs e)
		{
			//Guarantees that the icon will not linger.
			tray.Visible = false;
		}



		private void mnuDisplayForm_Click(object sender, System.EventArgs e)
		{
		}
		public NaviTrayIcon()
		{
			ThreadExit += AppContext_ThreadExit;
		}

	}
}
