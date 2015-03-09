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
namespace Net_Navis
{
	//Main Sub's go into Navi Main
	//Navi Resources is for any Navi specific Data
	//Host app calls Initialise then runs DoEvents
	//DXon if switched on changes rendering to a directx window rendering is then done in Draw_DX

	public partial class Navi_Main
	{
        
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


        private PointF Gravity = new PointF(0f, 0.5f);
        private PointF AirFriction = new PointF(0.01f, 0.01f);
        private PointF GroundFriction = new PointF(0.15f, 0f);
        private Point ScreenScroll;
        private Point ScreenBounds = new Point(15360, 966);
        
        private NetNavi_Type Host_Navi;
        private Dictionary<String, NetNavi_Type> Client_Navi;        

		bool Direct_Control = true;

		private double Physics_Timer;
		private double Physics_Rate;

		private long Program_Step;

        private class Projectiles_Type
		{
			public PointF Location;
			public PointF Speed;
            public double Scale;
			public int Life;
			public Projectiles_Type(Point Location, PointF Speed, int Life, double Scale)
			{
				this.Location = Location;
				this.Speed = Speed;
				this.Life = Life;
                this.Scale = Scale;
			}

		}


        private HashSet<Projectiles_Type> Projectile_List = new HashSet<Projectiles_Type>();

        public Navi_Main(string Navi_Name, long NaviID)
		{
			Host_Navi = Navi_resources.Get_Data(Navi_Name, NaviID);
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
			Physics_Timer = DateTime.Now.TimeOfDay.TotalSeconds;			
			Physics_Rate = 1 / 60.0;
			Program_Step = 0;
			//Host_Navi.set_Animation(Animation_Name_Enum.None)
			Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize().Y;
			Host_Navi.Location.X = 1000;
			Host_Navi.Scale = 3;
		}


		public void DoEvents()
		{
			//Slowing program down
			System.Threading.Thread.Sleep(Convert.ToInt32(Physics_Rate * 1000));

			if (Physics_Timer <= DateTime.Now.TimeOfDay.TotalSeconds) {
				Handle_UI();
				Process_Navi_Commands();
				Update_Physics();
				Navi_resources.Set_Correct_Animation(ref Host_Navi);
				Host_Navi.Update_Sprite();
                Host_Navi.ShootCharge += 1;

                DoNetworkEvents();

				Physics_Timer = DateTime.Now.TimeOfDay.TotalSeconds + Physics_Rate;
				Program_Step += 1;
			}

			Draw_Navi();
		}

		public void Handle_UI()
		{                                  
            
            if (pressedkeys.Contains(Keys.W)) {
				Host_Navi.Scale += 0.5f;
				Host_Navi.OldSprite = new Point(500, 500);
				if (Host_Navi.Scale < 0.5f)
					Host_Navi.Scale = 0.5f;
			}

			if (pressedkeys.Contains(Keys.S)) {
				Host_Navi.Scale -= 0.5f;
				Host_Navi.OldSprite = new Point(500, 500);
				if (Host_Navi.Scale < 0.5f)
					Host_Navi.Scale = 0.5f;
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
				if (GLOn == false)
					GLOn = true;
				else
					NaviGL.Dispose();


			if (pressedkeys.Contains(Keys.OemQuestion))
                Host_Navi.Shooting = true;
                else
                Host_Navi.Shooting = false;
			

            if (pressedkeys.Contains(Keys.D7) && !prevPressedkeys.Contains(Keys.D7))
                StopNetwork();
            //else if (pressedkeys.Contains(Keys.D2) && !prevPressedkeys.Contains(Keys.D2))
            //    StartNetwork("Jonny Fire");
            //else if (pressedkeys.Contains(Keys.D3) && !prevPressedkeys.Contains(Keys.D3))
            //    StartNetwork("Presto Pretzel");
            //else if (pressedkeys.Contains(Keys.D4) && !prevPressedkeys.Contains(Keys.D4))
            //    StartNetwork("Presto Pretzel", 11995);
            //else if (pressedkeys.Contains(Keys.D5) && !prevPressedkeys.Contains(Keys.D5))
            //    ConnectToPeer("192.168.1.244");
            //else if (pressedkeys.Contains(Keys.D6) && !prevPressedkeys.Contains(Keys.D6))
            //    ConnectToPeer("fastfattoad.com");
            //else if (pressedkeys.Contains(Keys.D7) && !prevPressedkeys.Contains(Keys.D7))
            //    ConnectToPeer("discojoker.com");
            //else if (pressedkeys.Contains(Keys.D8) && !prevPressedkeys.Contains(Keys.D8))
            //    ConnectToPeer("127.0.0.1");
            //else if (pressedkeys.Contains(Keys.D9) && !prevPressedkeys.Contains(Keys.D9))
            //    StartNetwork("Mechana Banana", 11996);

            else if (pressedkeys.Contains(Keys.NumPad4) && !prevPressedkeys.Contains(Keys.NumPad4))
                StartNetwork("Jonny Fire");
            else if (pressedkeys.Contains(Keys.NumPad5) && !prevPressedkeys.Contains(Keys.NumPad5))
                StartNetwork("Presto Pretzel", 11995);
            else if (pressedkeys.Contains(Keys.NumPad6) && !prevPressedkeys.Contains(Keys.NumPad6))
                StartNetwork("Mechana Banana", 11996);
            else if (pressedkeys.Contains(Keys.NumPad1) && !prevPressedkeys.Contains(Keys.NumPad1))
                ConnectToPeer("127.0.0.1", 11994);
            else if (pressedkeys.Contains(Keys.NumPad2) && !prevPressedkeys.Contains(Keys.NumPad2))
                ConnectToPeer("127.0.0.1", 11995);
            else if (pressedkeys.Contains(Keys.NumPad3) && !prevPressedkeys.Contains(Keys.NumPad3))
                ConnectToPeer("127.0.0.1", 11996);

            prevPressedkeys.Clear();
            foreach (System.Windows.Forms.Keys key in pressedkeys)
                prevPressedkeys.Add(key);
		}

        public void Process_Navi_Commands()
        {
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
            {
                if (Host_Navi.ShootCharge > 10)
                {
                    Host_Navi.ShootCharge = 0;                    
                    Point loc = new Point();
                    PointF Shoot_Point;
                    PointF Speed = new PointF();
                    Shoot_Point = Host_Navi.Get_Shoot_Point();
                    if (Host_Navi.FaceLeft)
                        loc.X = (int)Shoot_Point.X - (int)(8 * Host_Navi.Scale);
                    else
                        loc.X = (int)Shoot_Point.X;
                    loc.Y = (int)Shoot_Point.Y - (int)((6 / 2) * Host_Navi.Scale);
                    if (Host_Navi.FaceLeft) Speed.X = -20; else Speed.X = 20;
                    Projectile_List.Add(new Projectiles_Type(loc, Speed, 100, Host_Navi.Scale));
                }
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
            //Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y

            navi.Location.X = navi.Location.X + navi.Speed.X * navi.Scale;
            navi.Location.Y = navi.Location.Y + navi.Speed.Y * navi.Scale;

            if (!GLOn) 
                Update_Physics_GDI_Bounds(navi);
            else
                Update_Physics_GL_Bounds(navi);
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