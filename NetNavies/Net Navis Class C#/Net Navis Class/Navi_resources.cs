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
	public static class Navi_resources
	{
        public static NetNavi_Type Get_Data(Navi_Name_ID Navi_Name, ulong NAVIEXEID)
		{
			NetNavi_Type N = new NetNavi_Type();            
            switch (Navi_Name) {
                case
Navi_Name_ID.Junker:
                    N.Navi_Display_Name = "Junker";
                    N.HitBox = new Rectangle(0, 0, 35, 27);
                    N.SpriteSheet = Net_Navis.Resource1.Junker;
                    N.GLSpriteSheetName = GLNaviSpriteName.Junker;
                    N.Icon = Net_Navis.Resource1.Rebel_Icon;
                    N.SpriteSize = new Point(35, 27);
                    N.HealthMax = 10;
                    N.EnergyMax = 10;
                    N.Weight = 10;
                    N.GroundSpeed = 0f;
                    N.AirSpeed = 0f;
                    N.DashSpeed = 0;
                    N.Acrobatics = 0;
                    break;
                
                case 
Navi_Name_ID.Raven:
                    N.Navi_Display_Name = "Raven";
					N.HitBox = new Rectangle(10, 22, 26, 26);                    
					N.SpriteSheet = Net_Navis.Resource1.Raven;
                    N.GLSpriteSheetName = GLNaviSpriteName.Raven;
					N.Icon = Net_Navis.Resource1.Raven_Icon;
					N.SpriteSize = new Point(48, 48);
					N.HealthMax = 100;
					N.EnergyMax = 100;
					N.Weight = 50;
					N.GroundSpeed = 0.5f;
					N.AirSpeed = 0.15f;
					N.DashSpeed = 1;
					N.Acrobatics = 15;
                    N.ShootPoint = new Point(40, 33);
					break;
				case 
Navi_Name_ID.Vex:
                    N.Navi_Display_Name = "Vex";
					N.HitBox = new Rectangle(11, 18, 29, 30);
					N.SpriteSheet = Net_Navis.Resource1.Vex;
                    N.GLSpriteSheetName = GLNaviSpriteName.Vex;
					N.Icon = Net_Navis.Resource1.Vex_Icon;
					N.SpriteSize = new Point(48, 48);
					N.HealthMax = 100;
					N.EnergyMax = 100;
					N.Weight = 25;
					N.GroundSpeed = 0.5f;
					N.AirSpeed = 0.1f;
					N.DashSpeed = 2;
					N.Acrobatics = 10;
                    N.ShootPoint = new Point(37, 34);
					break;
				case
Navi_Name_ID.Barnabus:
                    N.Navi_Display_Name = "Barnabus";
					N.HitBox = new Rectangle(3, 5, 27, 27);
					N.SpriteSheet = Net_Navis.Resource1.Barnabus;
                    N.GLSpriteSheetName = GLNaviSpriteName.Barabus;
					N.Icon = Net_Navis.Resource1.Barnabus_Icon;
					N.SpriteSize = new Point(32, 32);
					N.HealthMax = 100;
					N.EnergyMax = 100;
					N.Weight = 50;
					N.GroundSpeed = 0.5f;
					N.AirSpeed = 0.1f;
					N.DashSpeed = 1;
					N.Acrobatics = 10;

					break;
				case 
Navi_Name_ID.Rebel:
                    N.Navi_Display_Name = "Rebel";
					N.HitBox = new Rectangle(13, 22, 22, 26);
					N.SpriteSheet = Net_Navis.Resource1.Rebelpullsheet;
                    N.GLSpriteSheetName = GLNaviSpriteName.Rebel;
					N.Icon = Net_Navis.Resource1.Rebel_Icon;
					N.SpriteSize = new Point(48, 48);
					N.HealthMax = 100;
					N.EnergyMax = 100;
					N.Weight = 30;
					N.GroundSpeed = 0.5f;
					N.AirSpeed = 0.1f;
					N.DashSpeed = 1;
					N.Acrobatics = 10;
					break;
                case
Navi_Name_ID.Zen:
                    N.Navi_Display_Name = "Zen";
                    N.HitBox = new Rectangle(15, 21, 19, 27);
                    N.SpriteSheet = Net_Navis.Resource1.Zen;
                    N.GLSpriteSheetName = GLNaviSpriteName.Zen;
                    N.Icon = Net_Navis.Resource1.Zen_Icon;
                    N.SpriteSize = new Point(48, 48);
                    N.HealthMax = 100;
                    N.EnergyMax = 100;
                    N.Weight = 15;
                    N.GroundSpeed = 0.8f;
                    N.AirSpeed = 0.1f;
                    N.DashSpeed = 2;
                    N.Acrobatics = 10;
                    N.ShootPoint = new Point(35, 37);
                    break;
			}
            N.NaviID = Navi_Name;
            N.NAVIEXEID = NAVIEXEID;
			N.Location = new PointF(0, 0);
			N.Sprite = new Point(0, 0);			
			return N;
		}

		public static Animation Get_Animation(Animation_Name_Enum Animation_Name)
		{
			Animation Ani = new Animation();
			switch (Animation_Name) {
				//---------------VEX--------------
				case 
Animation_Name_Enum.Vex_Standing:
					Ani_Frame[] frames = new Ani_Frame[1];
					frames[0] = new Ani_Frame(new Point(0, 0), 0);
					Ani.Frame = frames;
					Ani.Hold_Index = 0;
					break;
				case				
Animation_Name_Enum.Vex_Runing:
					frames = new Ani_Frame[8];
				for (int a = 0; a <= 7; a++) {
						frames[a] = new Ani_Frame(new Point(a + 1, 0), 5);
					}

					Ani.Frame = frames;
					Ani.RepeatFrame = 0;

					break;
				case 
Animation_Name_Enum.Vex_Jumping:
					frames = new Ani_Frame[6];
					for (int a = 0; a <= 5; a++) {
						frames[a] = new Ani_Frame(new Point(a + 9, 0), 5);
					}

					Ani.Frame = frames;
					Ani.Hold_Index = 4;

					break;


				case
Animation_Name_Enum.Vex_Dash_Start:
					frames = new Ani_Frame[3];
					for (int a = 0; a <= 2; a++) {
						frames[a] = new Ani_Frame(new Point(a, 1), 8);
					}

					Ani.Frame = frames;
					Ani.RepeatFrame = 1;

					break;


				case 
Animation_Name_Enum.Vex_Dash_End:
					frames = new Ani_Frame[2];
					for (int a = 0; a <= 1; a++) {
						frames[a] = new Ani_Frame(new Point(a + 3, 1), 8);
					}

					Ani.Frame = frames;
					Ani.Hold_Index = 1;

					break;


				//---------------RAVEN--------------
				case
Animation_Name_Enum.Raven_Standing:
					frames = new Ani_Frame[1];
					frames[0] = new Ani_Frame(new Point(0, 0), 0);
					Ani.Frame = frames;
					Ani.Hold_Index = 0;
					break;
				case
Animation_Name_Enum.Raven_Runing:
					frames = new Ani_Frame[2];

					for (int a = 0; a <= 1; a++) {
						frames[a] = new Ani_Frame(new Point(a, 1), 10);
					}

					Ani.Frame = frames;
					Ani.RepeatFrame = 0;

					break;
				case 
Animation_Name_Enum.Raven_Jumping:
					frames = new Ani_Frame[1];
					frames[0] = new Ani_Frame(new Point(6, 0), 0);
					Ani.Frame = frames;
					Ani.Hold_Index = 0;

					break;
				//---------------BARNABUS--------------
				case 
Animation_Name_Enum.Barnabus_Standing:
					frames = new Ani_Frame[1];
					frames[0] = new Ani_Frame(new Point(0, 0), 0);
					Ani.Frame = frames;
					Ani.Hold_Index = 0;
					break;
				case 
Animation_Name_Enum.Barnabus_Runing:
					frames = new Ani_Frame[10];
					var b = 1;
					for (int a = 0; a <= 9; a++) {
						frames[a] = new Ani_Frame(new Point(b, 0), 5);
						b = b + 1;
					}

					Ani.Frame = frames;
					Ani.RepeatFrame = 0;

					break;
				case 
Animation_Name_Enum.Barnabus_Jumping:
					frames = new Ani_Frame[4];
					b = 11;
					for (int a = 0; a <= 3; a++) {
						frames[a] = new Ani_Frame(new Point(b, 0), 10);
						b = b + 1;
					}

					Ani.Frame = frames;
					Ani.Hold_Index = 2;

					break;
				//---------------Rebel--------------

				case 
Animation_Name_Enum.Rebel_Standing:
					frames = new Ani_Frame[1];
					frames[0] = new Ani_Frame(new Point(0, 0), 0);
					Ani.Frame = frames;
					Ani.Hold_Index = 0;
					break;
				case 
Animation_Name_Enum.Rebel_Runing:

					frames = new Ani_Frame[10];

					for (int a = 0; a <= 9; a++) {
						frames[a] = new Ani_Frame(new Point(a + 1, 0), 5);

					}

					Ani.Frame = frames;
					Ani.RepeatFrame = 0;

					break;
				case  
Animation_Name_Enum.Rebel_Jumping:
					frames = new Ani_Frame[4];

					for (int a = 0; a <= 3; a++) {
						frames[a] = new Ani_Frame(new Point(a + 10, 0), 7);

					}

					Ani.Frame = frames;
					Ani.Hold_Index = 3;

					break;


                //---------------Zen--------------
                case
Animation_Name_Enum.Zen_Standing:
                    frames = new Ani_Frame[1];
                    frames[0] = new Ani_Frame(new Point(0, 0), 0);
                    Ani.Frame = frames;
                    Ani.Hold_Index = 0;
                    break;
                case
Animation_Name_Enum.Zen_Runing:
                    frames = new Ani_Frame[4];
                    
                    frames[0] = new Ani_Frame(new Point(1, 0), 10);
                    frames[1] = new Ani_Frame(new Point(2, 0), 10);
                    frames[2] = new Ani_Frame(new Point(3, 0), 10);
                    frames[3] = new Ani_Frame(new Point(2, 0), 10);                    

                    Ani.Frame = frames;
                    Ani.RepeatFrame = 0;

                    break;
                case
Animation_Name_Enum.Zen_Jumping:
                    frames = new Ani_Frame[1];
                    frames[0] = new Ani_Frame(new Point(4, 0), 0);                    
                    Ani.Frame = frames;
                    Ani.Hold_Index = 0;
                    break;
                case
Animation_Name_Enum.Zen_Dash:
                    frames = new Ani_Frame[1];                    
                    frames[0] = new Ani_Frame(new Point(5, 0), 0);                    
                    Ani.Frame = frames;
                    Ani.Hold_Index = 0;
                    break;
			}

			return Ani;
		}



		public class Ani_Frame
		{
			public Point Sprite;
			public int Duration;
			public Ani_Frame(Point Sprite, int Duration)
			{
				this.Sprite = Sprite;
				this.Duration = Duration;
			}
		}

		public class Animation
		{
			public Ani_Frame[] Frame;
			public bool Finished = false;
			public int RepeatFrame = -1;
			public int Hold_Index;
		}

		public static void Set_Correct_Animation(ref NetNavi_Type Navi)
		{
			switch (Navi.NaviID) {
				case 
Navi_Name_ID.Vex:

					if (Navi.OnGround == true) {
						if (Navi.Running == true) {
							if (Navi.Dashing == true) {
								if (!(Navi.Current_Animation == Animation_Name_Enum.Vex_Dash_Start))
									Navi.set_Animation(Animation_Name_Enum.Vex_Dash_Start);
							} else {
								Navi.HasDashed = false;
								if (!(Navi.Current_Animation == Animation_Name_Enum.Vex_Runing))
									Navi.set_Animation(Animation_Name_Enum.Vex_Runing);
							}


						} else {
							if (Navi.HasDashed == true) {
								if (!(Navi.Current_Animation == Animation_Name_Enum.Vex_Dash_End))
									Navi.set_Animation(Animation_Name_Enum.Vex_Dash_End);
								Navi.HasDashed = false;

							} else {
								if (!(Navi.Current_Animation == Animation_Name_Enum.Vex_Standing)) {
									if (Navi.Current_Animation == Animation_Name_Enum.Vex_Dash_End) {
										if (Navi.Ani_Current.Finished == true)
											Navi.set_Animation(Animation_Name_Enum.Vex_Standing);
									} else {
										Navi.set_Animation(Animation_Name_Enum.Vex_Standing);
									}
								}

							}
						}
					} else {
						Navi.HasDashed = false;
						if (!(Navi.Current_Animation == Animation_Name_Enum.Vex_Jumping))
							Navi.set_Animation(Animation_Name_Enum.Vex_Jumping);
					}

					break;


				case 
Navi_Name_ID.Raven:
					if (Navi.OnGround == true) {
						if (Navi.Running == true) {
							if (!(Navi.Current_Animation == Animation_Name_Enum.Raven_Runing))
								Navi.set_Animation(Animation_Name_Enum.Raven_Runing);
						} else {
							if (!(Navi.Current_Animation == Animation_Name_Enum.Raven_Standing))
								Navi.set_Animation(Animation_Name_Enum.Raven_Standing);
						}
					} else {
						if (!(Navi.Current_Animation == Animation_Name_Enum.Raven_Jumping))
							Navi.set_Animation(Animation_Name_Enum.Raven_Jumping);
					}

					break;
				case 
Navi_Name_ID.Barnabus:
					if (Navi.OnGround == true) {
						if (Navi.Running == true) {
							if (!(Navi.Current_Animation == Animation_Name_Enum.Barnabus_Runing))
								Navi.set_Animation(Animation_Name_Enum.Barnabus_Runing);
						} else {
							if (!(Navi.Current_Animation == Animation_Name_Enum.Barnabus_Standing))
								Navi.set_Animation(Animation_Name_Enum.Barnabus_Standing);
						}
					} else {
						if (!(Navi.Current_Animation == Animation_Name_Enum.Barnabus_Jumping))
							Navi.set_Animation(Animation_Name_Enum.Barnabus_Jumping);
					}

					break;

				case 
Navi_Name_ID.Rebel:
					if (Navi.OnGround == true) {
						if (Navi.Running == true) {
							if (!(Navi.Current_Animation == Animation_Name_Enum.Rebel_Runing))
								Navi.set_Animation(Animation_Name_Enum.Rebel_Runing);
						} else {
							if (!(Navi.Current_Animation == Animation_Name_Enum.Rebel_Standing))
								Navi.set_Animation(Animation_Name_Enum.Rebel_Standing);
						}
					} else {
						if (!(Navi.Current_Animation == Animation_Name_Enum.Rebel_Jumping))
							Navi.set_Animation(Animation_Name_Enum.Rebel_Jumping);
					}

					break;
                case
Navi_Name_ID.Zen:
                    if (Navi.OnGround == true)
                    {
                        if (Navi.Running == true)
                        {

                            if (Navi.Dashing == true)
                            {
                                if (!(Navi.Current_Animation == Animation_Name_Enum.Zen_Dash))
                                    Navi.set_Animation(Animation_Name_Enum.Zen_Dash);
                            }
                            else
                            {
                                if (!(Navi.Current_Animation == Animation_Name_Enum.Zen_Runing))
                                    Navi.set_Animation(Animation_Name_Enum.Zen_Runing);
                            }

                        }
                        else
                        {
                            if (!(Navi.Current_Animation == Animation_Name_Enum.Zen_Standing))
                                Navi.set_Animation(Animation_Name_Enum.Zen_Standing);                                                                                  
                        }
                    }
                    else
                    {                        
                        if (!(Navi.Current_Animation == Animation_Name_Enum.Zen_Jumping))
                            Navi.set_Animation(Animation_Name_Enum.Zen_Jumping);
                    }
					break;
			}
		}
	}


    public enum Navi_Abilities : int
    {
        Shoot = 1
    }


    public enum Navi_Name_ID : int
    {
        Junker = 0,
        Raven = 1,
        Vex = 2,
        Barnabus = 3,
        Rebel = 4,        
        Zen = 6
    }    

	public enum Animation_Name_Enum
		{
			None,
			Vex_Runing,
			Vex_Jumping,
			Vex_Standing,
			Vex_Dash_Start,
			Vex_Dash_End,
			Vex_Fallfast,
			Raven_Runing,
			Raven_Jumping,
			Raven_Standing,
			Barnabus_Runing,
			Barnabus_Jumping,
			Barnabus_Standing,
			Rebel_Runing,
			Rebel_Jumping,
			Rebel_Standing,
			Rebel_Dash_Start,
			Rebel_Dash_End,
			Rebel_Fallfast,
            Zen_Runing,
            Zen_Jumping,
            Zen_Standing,
            Zen_Dash,            
		}
}
