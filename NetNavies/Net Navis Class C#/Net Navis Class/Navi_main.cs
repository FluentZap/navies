using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Reflection;
using System.Threading;

namespace Net_Navis
{
	//Main Sub's go into Navi Main
	//Navi Resources is for any Navi specific Data
	//Host app calls Initialise then runs DoEvents
	//DXon if switched on changes rendering to a directx window rendering is then done in Draw_DX

	public partial class Navi_Main
	{
        public const int COMPACT_BUFFER_SIZE = 100;

        private NaviFormF NaviForm;
        private MenuForm MenuForm;                
        private NaviFXF NaviGL;        		
        //private NaviTrayIcon NaviTray;
        private HashSet<System.Windows.Forms.Keys> pressedkeys = new HashSet<System.Windows.Forms.Keys>();                	
        private HashSet<System.Windows.Forms.Keys> prevPressedkeys = new HashSet<System.Windows.Forms.Keys>();
		System.Drawing.Imaging.ImageAttributes NormalImage = new System.Drawing.Imaging.ImageAttributes();
		System.Drawing.Imaging.ImageAttributes BlueImage = new System.Drawing.Imaging.ImageAttributes();
		System.Drawing.Imaging.ImageAttributes RedImage = new System.Drawing.Imaging.ImageAttributes();
		System.Drawing.Imaging.ImageAttributes GreenImage = new System.Drawing.Imaging.ImageAttributes();


        private PointF Gravity = new PointF(0.0f, 0.5f);
        private PointF AirFriction = new PointF(0.01f, 0.01f);
        private PointF GroundFriction = new PointF(0.15f, 0f);
        private PointF ScreenScroll;
        private Point ScreenBounds = new Point(15360, 966);
        
        public NetNavi_Type Host_Navi;
        public Dictionary<String, NetNavi_Type> Client_Navi;
        private Navi_Network_TCP Net;


		bool Direct_Control = true;

		private double Physics_Rate;
        private double Render_Rate;
        private bool Advance_Physics;

        private PerformanceTimer Physics_Timer = new PerformanceTimer(60.0);
        
        private PerformanceTimer Render_Timer = new PerformanceTimer(60.0);

		public ulong Program_Step;

        private class Projectiles_Type
		{
			public PointF Location;
			public PointF Speed;
            public double Scale;
			public int Life;
            public Color4 Color;
			public Projectiles_Type(Point Location, PointF Speed, int Life, double Scale, Color4 Color)
			{
				this.Location = Location;
				this.Speed = Speed;
				this.Life = Life;
                this.Scale = Scale;
                this.Color = Color;
			}

		}


        private HashSet<Projectiles_Type> Projectile_List = new HashSet<Projectiles_Type>();

        public Navi_Main(int Navi_Name_ID, ulong NAVIEXEID)
		{
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            Host_Navi = Navi_resources.Get_Data((Navi_Name_ID)Navi_Name_ID, NAVIEXEID);            
		}

        private void OnApplicationExit(object sender, EventArgs e)
        {
            Net.StopNetwork();
        } 

        public void Initialise()
		{
			//Create and show forms
			NaviForm = new NaviFormF();            
            MenuForm = new MenuForm();
			NaviForm.Show();
			NaviForm.TopMost = true;
			NaviForm.Width = Convert.ToInt32(Host_Navi.GetSize().X);
			NaviForm.Height = Convert.ToInt32(Host_Navi.GetSize().Y);
			NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X);
			NaviForm.Top = Convert.ToInt32(Host_Navi.Location.Y);


            NaviForm.Paint += Draw_Navi_GDI;
            NaviForm.KeyDown += NaviForm_KeyDown;
            NaviForm.KeyUp += NaviForm_KeyUp;
            NaviForm.GotFocus += NaviForm_GotFocus;
            NaviForm.LostFocus += NaviForm_LostFocus;
            NaviForm.Disposed += NaviForm_Disposed;                      
            //NaviFXF handels set in startGL
            Client_Navi = new Dictionary<String, NetNavi_Type>();
            
			//Initialise defaults
			//NaviTray = New NaviTrayIcon
			//NaviTray.Initialise(Host_Navi)
			Set_color_filters();
            Physics_Timer.Start();
			Physics_Rate = 1000 / 60.0;
            Render_Rate = 1000 / 60.0;            
            Program_Step = 0;
			//Host_Navi.set_Animation(Animation_Name_Enum.None)
			Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize().Y;
			Host_Navi.Location.X = 1000;
			Host_Navi.Scale = 3;

            Net = new Navi_Network_TCP(this);

            //StartNetwork();

            Thread t = new Thread(ConsoleInput);
            t.IsBackground = true;
            t.Start();
		}


		public void DoEvents()
		{            
            Handle_UI();
            Physics_Timer.Stop(); // doesn't actually stop the timer, just updates it
            //if (Physics_Rate > Physics_Timer.ElapsedTime)
                //Thread.Sleep((int)(Physics_Rate - Physics_Timer.ElapsedTime) + 1);            
            
            if (Physics_Timer.ElapsedTime > Physics_Rate)
            //if (Advance_Physics == true)
            {
                //Advance_Physics = false;
                if (!Net.NetworkHold)
                {
                    Process_Navi_Commands();                    
                    Update_Physics();
                    Navi_resources.Set_Correct_Animation(ref Host_Navi);

                    Host_Navi.Update_Sprite();
                    Host_Navi.ShootCharge += 1;
                    Program_Step += 1;
                    Physics_Timer.Start();
                }
                Net.NetworkHold = true;
                Net.DoNetworkEvents();
            }


            Render_Timer.Stop();
            if (Render_Timer.ElapsedTime > Render_Rate)
            {
                Draw_Navi();
                //Advance_Physics = true;
                Render_Timer.Start();
            }

		}


        public void Advance_Clients()
        {
            foreach (NetNavi_Type navi in Client_Navi.Values)
            {
                navi.Process_Update();
                Process_Client_Commands(navi);
            }
                
        }
        

		public void Handle_UI()
		{                                  
            
            if (pressedkeys.Contains(Keys.W)) {				
			}

			if (pressedkeys.Contains(Keys.S)) {				
			}

			if (pressedkeys.Contains(Keys.A)) {
				Host_Navi.FaceLeft = true;
				Host_Navi.Running = true;
				if (pressedkeys.Contains(Keys.ShiftKey)){Host_Navi.Dashing = true;Host_Navi.HasDashed = true;}
				else
					Host_Navi.Dashing = false;
			}

			if (pressedkeys.Contains(Keys.D)) {
				Host_Navi.FaceLeft = false;
				Host_Navi.Running = true;
				if (pressedkeys.Contains(Keys.ShiftKey)){Host_Navi.Dashing = true;Host_Navi.HasDashed = true;}
				else
					Host_Navi.Dashing = false;
			}

			if (!pressedkeys.Contains(Keys.ShiftKey))
				Host_Navi.Dashing = false;

			if (!pressedkeys.Contains(Keys.D) && !pressedkeys.Contains(Keys.A)) {
				Host_Navi.Running = false;
			}


			if (pressedkeys.Contains(Keys.Space)) {
				if (Host_Navi.OnGround == true) {
					Host_Navi.Jumping = true;
					Host_Navi.HasJumped = true;
				}
			}

            if (pressedkeys.Contains(Keys.Tab))
            {
                pressedkeys.Remove(Keys.Tab);
                if (GLOn == false)
                    GLOn = true;
                else
                    NaviGL.Dispose();
            }

			if (pressedkeys.Contains(Keys.OemQuestion))
                Host_Navi.Shooting = true;
                else
                Host_Navi.Shooting = false;

            if (pressedkeys.Contains(Keys.D1) && !prevPressedkeys.Contains(Keys.D1))
                Net.ConnectToPeer("discojoker.com", 53300);
            else if (pressedkeys.Contains(Keys.D2) && !prevPressedkeys.Contains(Keys.D2))
                Net.ConnectToPeer("fastfattoad.com", 53300);
            else if (pressedkeys.Contains(Keys.D3) && !prevPressedkeys.Contains(Keys.D3))
                Net.ConnectToPeer("127.0.0.1", 53300);
            //else if (pressedkeys.Contains(Keys.T) && !prevPressedkeys.Contains(Keys.T))
            //    StartNetwork("Jonny Flame");
            //else if (pressedkeys.Contains(Keys.Y) && !prevPressedkeys.Contains(Keys.Y))
            //    StartNetwork("Presto Pretzel", 11995);
            //else if (pressedkeys.Contains(Keys.U) && !prevPressedkeys.Contains(Keys.U))
            //    StartNetwork("Mechana Banana", 11996);
            //else if (pressedkeys.Contains(Keys.I) && !prevPressedkeys.Contains(Keys.I))
            //    StartNetwork("Rico Rico", 11997);
            //else if (pressedkeys.Contains(Keys.O) && !prevPressedkeys.Contains(Keys.O))
            //    StartNetwork("Chloe Lamb", 11998);
            //else if (pressedkeys.Contains(Keys.G) && !prevPressedkeys.Contains(Keys.G))
            //    ConnectToPeer("127.0.0.1", 11994);
            //else if (pressedkeys.Contains(Keys.H) && !prevPressedkeys.Contains(Keys.H))
            //    ConnectToPeer("127.0.0.1", 11995);
            //else if (pressedkeys.Contains(Keys.J) && !prevPressedkeys.Contains(Keys.J))
            //    ConnectToPeer("127.0.0.1", 11996);
            //else if (pressedkeys.Contains(Keys.K) && !prevPressedkeys.Contains(Keys.K))
            //    ConnectToPeer("127.0.0.1", 11997);
            //else if (pressedkeys.Contains(Keys.L) && !prevPressedkeys.Contains(Keys.L))
            //    ConnectToPeer("127.0.0.1", 11998);

            prevPressedkeys.Clear();
            foreach (System.Windows.Forms.Keys key in pressedkeys)
                prevPressedkeys.Add(key);
		}

        void ConsoleInput()
        {
            string[] command = {""};

            while (command[0] != "exit" && command[0] != "quit")
            {
                command = Console.ReadLine().Split(' ');

                if (command.Length == 0)
                    continue;

                if (command[0] == "help")
                {
                    Console.WriteLine("\t\"start\"");
                    Console.WriteLine("\t\"stop\"");
                    Console.WriteLine("\t\"connect IP PORT\"");
                    Console.WriteLine("\t\"captain\"");
                    Console.WriteLine("\t\"peers\"");
                    Console.WriteLine("\t\"name\"");
                    Console.WriteLine("\t\"step\"");
                }
                else if (command[0] == "connect")
                {
                    if (command[1] == "home")
                        command[1] = "127.0.0.1";
                    Net.ConnectToPeer(command[1], Convert.ToInt32(command[2]));
                }
                else if (command[0] == "start")
                    Net.StartNetwork();
                else if (command[0] == "stop")
                    Net.StopNetwork();
                else if (command[0] == "name")
                    Console.WriteLine(Net.name);
                else if (command[0] == "step")
                    Console.WriteLine(Program_Step);
                else if (command[0] == "captain")
                {
                    if (Net.networkCaptain == null)
                        Console.WriteLine("me");
                    else
                        Console.WriteLine(Net.networkCaptain);
                }
                else if (command[0] == "peers")
                    foreach (string name in Net.peers.Keys)
                        Console.WriteLine(name);
            }
        }

        public void Process_Navi_Commands()
        {
            /*
            if (Direct_Control == false)
            {
                Host_Navi.Running = false;
                if (Host_Navi.Navi_Location().Right <= Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox().Width)
                {
                    Host_Navi.FaceLeft = false;
                    Host_Navi.Running = true;
                }
                if (Host_Navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox().Width)
                {
                    Host_Navi.FaceLeft = true;
                }
            }
            */

            #region Movement
            //Move Navies
            if (Host_Navi.OnGround == true)
            {
                if (Host_Navi.Running == true)
                {
                    //Check for dashing
                    if (Host_Navi.Dashing == true)
                    {
                        //Dashing
                        if (Host_Navi.FaceLeft == true)
                            Host_Navi.Speed.X -= Host_Navi.DashSpeed;
                        else
                            Host_Navi.Speed.X += Host_Navi.DashSpeed;
                    }
                    else
                    {
                        //Running
                        if (Host_Navi.FaceLeft == true)
                            Host_Navi.Speed.X -= Host_Navi.GroundSpeed;
                        else
                            Host_Navi.Speed.X += Host_Navi.GroundSpeed;
                    }
                }
                //Jumping
                if (Host_Navi.Jumping == true && Host_Navi.HasJumped == true) { Host_Navi.Speed.Y -= Host_Navi.Acrobatics; Host_Navi.HasJumped = false; }


            }
            else
            {
                //Air moving
                if (Host_Navi.Running == true)
                {
                    if (Host_Navi.FaceLeft == true)
                        Host_Navi.Speed.X -= Host_Navi.AirSpeed;
                    else
                        Host_Navi.Speed.X += Host_Navi.AirSpeed;
                }
            }
            #endregion                       
            if (Host_Navi.Shooting == true)
                if (Host_Navi.ShootCharge > 20)
                {
                    Color4 color = new Color4();                    
                    Host_Navi.ShootCharge = 0;
                    Host_Navi.Shoot_Advance += 1;
                    if (Host_Navi.Shoot_Advance > 3) Host_Navi.Shoot_Advance = 1;
                    Host_Navi.Activated_Ability = Host_Navi.Shoot_Advance;

                    if (Host_Navi.Shoot_Advance == 1) { color.R = 255; color.G = 0; color.B = 0; color.A = 255; }
                    if (Host_Navi.Shoot_Advance == 2) { color.R = 0; color.G = 255; color.B = 0; color.A = 255; }
                    if (Host_Navi.Shoot_Advance == 3) { color.R = 0; color.G = 0; color.B = 255; color.A = 255; }

                    Point loc = new Point();
                    PointF Shoot_Point;
                    PointF Speed = new PointF();
                    Shoot_Point = Host_Navi.Get_Shoot_Point();
                    if (Host_Navi.FaceLeft)
                        loc.X = (int)Shoot_Point.X - (int)(8 * Host_Navi.Scale);
                    else
                        loc.X = (int)Shoot_Point.X;
                    loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * Host_Navi.Scale);
                    if (Host_Navi.FaceLeft) Speed.X = -10; else Speed.X = 10;
                    Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, Host_Navi.Scale, color));
                }
        }

        public void Process_Client_Commands(NetNavi_Type navi)
        {
            if (navi.Activated_Ability == 1)
            {
                    Point loc = new Point();
                    PointF Shoot_Point;
                    PointF Speed = new PointF();
                    Shoot_Point = navi.Get_Shoot_Point();
                    if (navi.FaceLeft)
                        loc.X = (int)Shoot_Point.X - (int)(8 * navi.Scale);
                    else
                        loc.X = (int)Shoot_Point.X;
                    loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * navi.Scale);
                    if (navi.FaceLeft) Speed.X = -10; else Speed.X = 10;
                    Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, navi.Scale, new Color4(255, 0, 0, 255)));
            }

            if (navi.Activated_Ability == 2)
            {
                Point loc = new Point();
                PointF Shoot_Point;
                PointF Speed = new PointF();
                Shoot_Point = navi.Get_Shoot_Point();
                if (navi.FaceLeft)
                    loc.X = (int)Shoot_Point.X - (int)(8 * navi.Scale);
                else
                    loc.X = (int)Shoot_Point.X;
                loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * navi.Scale);
                if (navi.FaceLeft) Speed.X = -10; else Speed.X = 10;
                Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, navi.Scale, new Color4(0, 255, 0, 255)));
            }

            if (navi.Activated_Ability == 3)
            {
                Point loc = new Point();
                PointF Shoot_Point;
                PointF Speed = new PointF();
                Shoot_Point = navi.Get_Shoot_Point();
                if (navi.FaceLeft)
                    loc.X = (int)Shoot_Point.X - (int)(8 * navi.Scale);
                else
                    loc.X = (int)Shoot_Point.X;
                loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * navi.Scale);
                if (navi.FaceLeft) Speed.X = -10; else Speed.X = 10;
                Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, navi.Scale, new Color4(0, 0, 255, 255)));
            }

        }


        
        #region Physics update

        public void Update_Physics()
		{
            Update_Physics_Projectiles();
            Update_Physics_Navi(Host_Navi);
            //Update GDI Location
            if (!GLOn)
            {                
                NaviForm.Left = Convert.ToInt32(Host_Navi.Location.X);
                NaviForm.Top = Convert.ToInt32(Host_Navi.Location.Y);
            }            
		}

        private void Update_Physics_Projectiles()
        {
            HashSet<Projectiles_Type> Item_Remove_List = new HashSet<Projectiles_Type>();
            foreach (Navi_Main.Projectiles_Type item in Projectile_List)
            {
                item.Location.X += item.Speed.X;
                item.Location.Y += item.Speed.Y;
                item.Life -= 1;
                if (item.Life <= 0)
                    Item_Remove_List.Add(item);
            }

            foreach (Navi_Main.Projectiles_Type item in Item_Remove_List)
            {
                Projectile_List.Remove(item);
            }
        }

        private void Update_Physics_Navi(NetNavi_Type navi)
        {            
            //Friction
            if (navi.OnGround == true)
            {
                navi.Speed.X -= navi.Speed.X * GroundFriction.X;
                navi.Speed.Y -= navi.Speed.Y * GroundFriction.Y;
            }
            else
            {
                navi.Speed.X -= navi.Speed.X * AirFriction.X;
                navi.Speed.Y -= navi.Speed.Y * AirFriction.Y;
            }

            //Gravity
            if (navi.OnGround == false)
                navi.Speed.Y = navi.Speed.Y + Gravity.Y;
            navi.Speed.X = navi.Speed.X + Gravity.X;
            //Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y

            navi.Location.X = navi.Location.X + navi.Speed.X * navi.Scale;
            navi.Location.Y = navi.Location.Y + navi.Speed.Y * navi.Scale;            

            if (!GLOn)
                Update_Physics_GDI_Bounds(navi);
            else
                Update_Physics_GL_Bounds(navi);
            navi.Location_Last = navi.Location;
        }

        private void Update_Physics_GDI_Bounds(NetNavi_Type navi)
        {                        
                //Bounds
                if (navi.FaceLeft == true)
                {
                    if (navi.Navi_Location().Left <= Screen.PrimaryScreen.WorkingArea.Left) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - (navi.GetSize().X - navi.GetHitBox().Right); navi.Speed.X = 0; }
                }
                else
                {
                    if (navi.Navi_Location().Left <= Screen.PrimaryScreen.WorkingArea.Left) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - navi.GetHitBox().Left; navi.Speed.X = 0; }
                }

                if (navi.FaceLeft == true)
                {
                    if (navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - (navi.GetSize().X - navi.GetHitBox().Left); navi.Speed.X = 0; }
                }
                else
                {
                    if (navi.Navi_Location().Right >= Screen.PrimaryScreen.WorkingArea.Right) { navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - navi.GetHitBox().Right; navi.Speed.X = 0; }
                }

                if (navi.Navi_Location().Bottom > Screen.PrimaryScreen.WorkingArea.Bottom)
                    navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - navi.GetHitBox().Bottom;
                if (navi.Navi_Location().Bottom == Screen.PrimaryScreen.WorkingArea.Bottom) { navi.OnGround = true; navi.Speed.Y = 0; }
                else
                    navi.OnGround = false;            
        }

        private void Update_Physics_GL_Bounds(NetNavi_Type navi)
        {
            //Bounds
            if (navi.FaceLeft == true)
            {
                if (navi.Navi_Location().Left <= 0) { navi.Location.X = 0 - (navi.GetSize().X - navi.GetHitBox().Right); navi.Speed.X = 0; }
            }
            else
            {
                if (navi.Navi_Location().Left <= 0) { navi.Location.X = 0 - navi.GetHitBox().Left; navi.Speed.X = 0; }
            }

            if (navi.FaceLeft == true)
            {
                if (navi.Navi_Location().Right >= ScreenBounds.X) { navi.Location.X = ScreenBounds.X - (navi.GetSize().X - navi.GetHitBox().Left); navi.Speed.X = 0; }
            }
            else
            {
                if (navi.Navi_Location().Right >= ScreenBounds.X) { navi.Location.X = ScreenBounds.X - navi.GetHitBox().Right; navi.Speed.X = 0; }
            }

            if (navi.Navi_Location().Bottom > ScreenBounds.Y)
                navi.Location.Y = ScreenBounds.Y - navi.GetHitBox().Bottom;
            if (navi.Navi_Location().Bottom == ScreenBounds.Y) { navi.OnGround = true; navi.Speed.Y = 0; }
            else
                navi.OnGround = false;

            if (navi.Navi_Location().Top < 0) { navi.Location.Y = 0 - navi.GetHitBox().Top; navi.Speed.Y = 0;}
        }


        #endregion

        #region Fourm Handelers

        private void NaviForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			pressedkeys.Add(e.KeyCode); // automatically checks for duplicates and won't add twice
		}

		private void NaviForm_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			pressedkeys.Remove(e.KeyCode); // returns false if KeyCode isn't in the hashset
		}

		private void NaviForm_GotFocus(object sender, System.EventArgs e)
		{
			Direct_Control = true;
		}

		private void NaviForm_LostFocus(object sender, System.EventArgs e)
		{
			if (GLOn == false)
				Direct_Control = false;
			pressedkeys.Clear();
		}

		private void NaviForm_Disposed(object sender, System.EventArgs e)
		{
			//NaviTray.tray.Visible = True
		}


		private void NaviGL_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (pressedkeys.Contains(e.KeyCode)) {
			} else {
				pressedkeys.Add(e.KeyCode);
			}
		}

		private void NaviGL_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (pressedkeys.Contains(e.KeyCode)) {
				pressedkeys.Remove(e.KeyCode);
			}
		}

		private void NaviGL_LostFocus(object sender, System.EventArgs e)
		{
			pressedkeys.Clear();
		}

		private void NaviGL_Disposed(object sender, System.EventArgs e)
		{
			GLOn = false;
			Init_GL = false;
            foreach (int id in GLNaviTexture.Values)
            {
                int ID = id;
                GL.DeleteTextures(1, ref ID);
            }
            GL.DeleteTextures(1, ref GLProjectileTexture);
            GLNaviTexture.Clear();
			NaviForm.Show();
            //Reset Navi location
            Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize().Y;
            Host_Navi.Location.X = 1000;
            Host_Navi.Scale = 3;
        }
        #endregion
    }
}		