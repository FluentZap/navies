Public Module Navi_resources
    Function Get_Data(ByVal Navi_Name As String, ByVal NaviID As Long) As NetNavi_Type
        Dim N As New NetNavi_Type
        Select Case Navi_Name
            Case Is = "Raven"
                N.NaviID = 0
                N.HitBox = New Rectangle(10, 22, 30, 26)
                N.SpriteSheet = My.Resources.Raven
                N.SpriteSize = New Point(48, 48)
                N.HealthMax = 100
                N.EnergyMax = 100
                N.Weight = 50
                N.GroundSpeed = 1
                N.AirSpeed = 0.2
                N.DashSpeed = 2
                N.Acrobatics = 40

            Case Is = "Vex"
                N.NaviID = 1
                N.HitBox = New Rectangle(10, 18, 29, 30)
                N.SpriteSheet = My.Resources.Vex
                N.SpriteSize = New Point(48, 48)
                N.HealthMax = 100
                N.EnergyMax = 100
                N.Weight = 50
                N.GroundSpeed = 1
                N.AirSpeed = 0.1
                N.DashSpeed = 3
                N.Acrobatics = 30

            Case Is = "Barnabus"
                N.NaviID = 2
                N.HitBox = New Rectangle(3, 5, 27, 27)
                N.SpriteSheet = My.Resources.Barnabus
                N.SpriteSize = New Point(32, 32)
                N.HealthMax = 100
                N.EnergyMax = 100
                N.Weight = 50
                N.GroundSpeed = 1
                N.AirSpeed = 0.1
                N.DashSpeed = 2
                N.Acrobatics = 30

            Case Is = "Rebel"
                N.NaviID = 3
                N.HitBox = New Rectangle(3, 5, 27, 27)
                N.SpriteSheet = My.Resources.Barnabus
                N.SpriteSize = New Point(48, 48)
                N.HealthMax = 100
                N.EnergyMax = 100
                N.Weight = 50
                N.GroundSpeed = 1
                N.AirSpeed = 0.1
                N.DashSpeed = 2
                N.Acrobatics = 30
        End Select

        N.Size = N.SpriteSize
        N.Location = New PointF(0, 0)
        N.Sprite = New Point(0, 0)
        N.Navi_Name = Navi_Name
        Return N
    End Function



    Function Get_Animation(ByVal Animation_Name As Animation_Name_Enum) As Animation
        Dim Ani As New Animation
        Select Case Animation_Name
            '---------------VEX--------------
            Case Is = Animation_Name_Enum.Vex_Standing
                Dim frames(0) As Ani_Frame
                frames(0) = New Ani_Frame(New Point(0, 0), 0)
                Ani.Frame = frames
                Ani.Hold_Index = 0
            Case Is = Animation_Name_Enum.Vex_Runing
                Dim frames(7) As Ani_Frame                
                For a = 0 To 7
                    frames(a) = New Ani_Frame(New Point(a + 1, 0), 5)                    
                Next
                Ani.Frame = frames
                Ani.RepeatFrame = 0

            Case Is = Animation_Name_Enum.Vex_Jumping
                Dim frames(5) As Ani_Frame
                For a = 0 To 5
                    frames(a) = New Ani_Frame(New Point(a + 9, 0), 5)
                Next
                Ani.Frame = frames
                Ani.Hold_Index = 4



            Case Is = Animation_Name_Enum.Vex_Dash_Start
                Dim frames(2) As Ani_Frame
                For a = 0 To 2
                    frames(a) = New Ani_Frame(New Point(a, 1), 8)
                Next
                Ani.Frame = frames
                Ani.RepeatFrame = 1



            Case Is = Animation_Name_Enum.Vex_Dash_End
                Dim frames(1) As Ani_Frame
                For a = 0 To 1
                    frames(a) = New Ani_Frame(New Point(a + 3, 1), 8)
                Next
                Ani.Frame = frames
                Ani.Hold_Index = 1



                '---------------RAVEN--------------
            Case Is = Animation_Name_Enum.Raven_Standing
                Dim frames(0) As Ani_Frame
                frames(0) = New Ani_Frame(New Point(0, 0), 0)
                Ani.Frame = frames
                Ani.Hold_Index = 0
            Case Is = Animation_Name_Enum.Raven_Runing
                Dim frames(1) As Ani_Frame

                For a = 0 To 1
                    frames(a) = New Ani_Frame(New Point(a, 1), 10)
                Next
                Ani.Frame = frames
                Ani.RepeatFrame = 0

            Case Is = Animation_Name_Enum.Raven_Jumping
                Dim frames(0) As Ani_Frame
                frames(0) = New Ani_Frame(New Point(6, 0), 0)
                Ani.Frame = frames
                Ani.Hold_Index = 0

                '---------------BARNABUS--------------
            Case Is = Animation_Name_Enum.Barnabus_Standing
                Dim frames(0) As Ani_Frame
                frames(0) = New Ani_Frame(New Point(0, 0), 0)
                Ani.Frame = frames
                Ani.Hold_Index = 0
            Case Is = Animation_Name_Enum.Barnabus_Runing
                Dim frames(9) As Ani_Frame
                Dim b = 1
                For a = 0 To 9
                    frames(a) = New Ani_Frame(New Point(b, 0), 5)
                    b = b + 1
                Next
                Ani.Frame = frames
                Ani.RepeatFrame = 0

            Case Is = Animation_Name_Enum.Barnabus_Jumping
                Dim frames(3) As Ani_Frame
                Dim b = 11
                For a = 0 To 3
                    frames(a) = New Ani_Frame(New Point(b, 0), 10)
                    b = b + 1
                Next
                Ani.Frame = frames
                Ani.Hold_Index = 3

                '---------------Rebel--------------

        End Select


        Return Ani
    End Function



    Class Ani_Frame
        Public Sprite As Point
        Public Duration As Integer
        Sub New(ByVal Sprite As Point, ByVal Duration As Integer)
            Me.Sprite = Sprite
            Me.Duration = Duration
        End Sub
    End Class

    Class Animation
        Public Frame() As Ani_Frame        
        Public Finished As Boolean = False
        Public RepeatFrame As Integer = -1
        Public Hold_Index As Integer
    End Class



    Public Enum Animation_Name_Enum
        None
        Vex_Runing
        Vex_Jumping
        Vex_Standing
        Vex_Dash_Start
        Vex_Dash_End
        Raven_Runing
        Raven_Jumping
        Raven_Standing
        Barnabus_Runing
        Barnabus_Jumping
        Barnabus_Standing
        Rebel_Runing
        Rebel_Jumping
        Rebel_Standing
        Rebel_Dash_Start
        Rebel_Dash_End
    End Enum



    Public Sub Set_Correct_Animation(ByRef Navi As NetNavi_Type)

        Select Case Navi.Navi_Name
            Case Is = "Vex"

                If Navi.OnGround = True Then
                    If Navi.Running = True Then
                        If Navi.Dashing = True Then
                            If Not Navi.Current_Animation = Animation_Name_Enum.Vex_Dash_Start Then Navi.set_Animation(Animation_Name_Enum.Vex_Dash_Start)
                        Else
                            Navi.HasDashed = False
                            If Not Navi.Current_Animation = Animation_Name_Enum.Vex_Runing Then Navi.set_Animation(Animation_Name_Enum.Vex_Runing)
                        End If

                    Else

                        If Navi.HasDashed = True Then
                            If Not Navi.Current_Animation = Animation_Name_Enum.Vex_Dash_End Then Navi.set_Animation(Animation_Name_Enum.Vex_Dash_End)
                            Navi.HasDashed = False
                        Else

                            If Not Navi.Current_Animation = Animation_Name_Enum.Vex_Standing Then
                                If Navi.Current_Animation = Animation_Name_Enum.Vex_Dash_End Then
                                    If Navi.Ani_Current.Finished = True Then Navi.set_Animation(Animation_Name_Enum.Vex_Standing)
                                Else
                                    Navi.set_Animation(Animation_Name_Enum.Vex_Standing)
                                End If
                            End If

                        End If
                    End If
                Else
                    Navi.HasDashed = False
                    If Not Navi.Current_Animation = Animation_Name_Enum.Vex_Jumping Then Navi.set_Animation(Animation_Name_Enum.Vex_Jumping)
                End If



            Case Is = "Raven"
                If Navi.OnGround = True Then
                    If Navi.Running = True Then
                        If Not Navi.Current_Animation = Animation_Name_Enum.Raven_Runing Then Navi.set_Animation(Animation_Name_Enum.Raven_Runing)
                    Else
                        If Not Navi.Current_Animation = Animation_Name_Enum.Raven_Standing Then Navi.set_Animation(Animation_Name_Enum.Raven_Standing)
                    End If
                Else
                    If Not Navi.Current_Animation = Animation_Name_Enum.Raven_Jumping Then Navi.set_Animation(Animation_Name_Enum.Raven_Jumping)
                End If

            Case Is = "Barnabus"
                If Navi.OnGround = True Then
                    If Navi.Running = True Then
                        If Not Navi.Current_Animation = Animation_Name_Enum.Barnabus_Runing Then Navi.set_Animation(Animation_Name_Enum.Barnabus_Runing)
                    Else
                        If Not Navi.Current_Animation = Animation_Name_Enum.Barnabus_Standing Then Navi.set_Animation(Animation_Name_Enum.Barnabus_Standing)
                    End If
                Else
                    If Not Navi.Current_Animation = Animation_Name_Enum.Barnabus_Jumping Then Navi.set_Animation(Animation_Name_Enum.Barnabus_Jumping)
                End If


            Case Is = "Rebel"
                If Navi.OnGround = True Then
                    If Navi.Running = True Then
                        If Not Navi.Current_Animation = Animation_Name_Enum.Barnabus_Runing Then Navi.set_Animation(Animation_Name_Enum.Barnabus_Runing)
                    Else
                        If Not Navi.Current_Animation = Animation_Name_Enum.Barnabus_Standing Then Navi.set_Animation(Animation_Name_Enum.Barnabus_Standing)
                    End If
                Else
                    If Not Navi.Current_Animation = Animation_Name_Enum.Barnabus_Jumping Then Navi.set_Animation(Animation_Name_Enum.Barnabus_Jumping)
                End If



        End Select



    End Sub


End Module
