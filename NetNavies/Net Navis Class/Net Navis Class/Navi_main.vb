'Main Sub's go into Navi Main
'Navi Resources is for any Navi spasific Data










Public Class Navi_Main
    Private WithEvents NaviForm As NaviForm
    Private WithEvents MenuForm As MenuForm

    Dim NormalImage As New Imaging.ImageAttributes
    Dim BlueImage As New Imaging.ImageAttributes
    Dim RedImage As New Imaging.ImageAttributes
    Dim GreenImage As New Imaging.ImageAttributes

    Public Gravity As PointF = New PointF(0, 1)
    Public AirFriction As New PointF(0.01, 0.01)
    Public GroundFriction As New PointF(0.15, 0)

    Private Host_Navi As NetNavi_Type

    Private Physics_Timer As Double
    Private Physics_Rate As Double
    Private Physics_Step As Long


    Class Projectiles_Type
        Public Location As Point
        Public Speed As PointF

        Sub New(ByVal Location As Point, ByVal Speed As PointF)
            Me.Location = Location
            Me.Speed = Speed
        End Sub

    End Class

    Private Projectile_List As HashSet(Of Projectiles_Type) = New HashSet(Of Projectiles_Type)


    Sub New(ByVal Navi_Name As String, ByVal NaviID As Long)
        Host_Navi = Get_Data(Navi_Name, NaviID)


    End Sub



    Sub Initialise()
        'Create and show forms
        NaviForm = New NaviForm
        MenuForm = New MenuForm
        NaviForm.Show()
        NaviForm.Show()
        NaviForm.Width = Host_Navi.Size.X
        NaviForm.Height = Host_Navi.Size.Y

        'Initialise defaults
        Set_color_filters()

        'Dim ip As Net.IPAddress = Net.IPAddress.Parse("127.0.0.1")
        'Dim Net_Client As New Net.Sockets.TcpClient
        'Dim Net_Host As New Net.Sockets.TcpListener(ip, 52525)


        'Try
        '   Net_Client.Connect(ip, 52525)
        'Catch
        '   Net_Host.Start()
        'End Try



        Physics_Timer = DateAndTime.Timer
        Physics_Rate = 1 / 60
        Physics_Step = 0

        Host_Navi.set_Animation(Animation_Name_Enum.Vex_Runing)

        Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.Size.Y
        Host_Navi.Location.X = 1000

        Host_Navi.Scale = 3

    End Sub


    Sub DoEvents()
        'Slowing program down
        System.Threading.Thread.Sleep(Physics_Rate * 1000)

        If Physics_Timer <= DateAndTime.Timer Then
            Handle_UI()
            Process_Navi_Commands()
            Update_Physics()
            Set_Correct_Animation(Host_Navi)
            Host_Navi.Update_Sprite()
            Physics_Timer = DateAndTime.Timer + Physics_Rate
            Physics_Step += 1
        End If

        Draw_All()
    End Sub


    Sub Handle_UI()

        If NaviForm.pressedkeys.Contains(Keys.W) Then

        End If

        If NaviForm.pressedkeys.Contains(Keys.S) Then

        End If


        If NaviForm.pressedkeys.Contains(Keys.A) Then
            Host_Navi.FaceLeft = True
            Host_Navi.Running = True
            If NaviForm.pressedkeys.Contains(Keys.ShiftKey) Then Host_Navi.Dashing = True : Host_Navi.HasDashed = True Else Host_Navi.Dashing = False
        End If

        If NaviForm.pressedkeys.Contains(Keys.D) Then
            Host_Navi.FaceLeft = False
            Host_Navi.Running = True
            If NaviForm.pressedkeys.Contains(Keys.ShiftKey) Then Host_Navi.Dashing = True : Host_Navi.HasDashed = True Else Host_Navi.Dashing = False
        End If

        If Not NaviForm.pressedkeys.Contains(Keys.D) AndAlso Not NaviForm.pressedkeys.Contains(Keys.A) Then
            Host_Navi.Running = False
        End If


        If NaviForm.pressedkeys.Contains(Keys.Space) Then
            If Host_Navi.OnGround = True Then
                Host_Navi.Jumping = True
                Host_Navi.HasJumped = True
            End If
        End If



        If NaviForm.pressedkeys.Contains(Keys.ControlKey) Then
            Projectile_List.Add(New Projectiles_Type(New Point(Host_Navi.GetHitBox.Left, Host_Navi.GetHitBox.Top), New PointF(5, 0)))
            NaviForm.pressedkeys.Remove(Keys.ControlKey)
        End If


    End Sub

    Sub Draw_All()
        If Not Host_Navi.Sprite = Host_Navi.OldSprite OrElse Not Host_Navi.FaceLeft = Host_Navi.OldFaceLeft Then
            NaviForm.Invalidate()
            Host_Navi.OldSprite = Host_Navi.Sprite
            Host_Navi.OldFaceLeft = Host_Navi.FaceLeft
        End If

        'If Projectile_List.Count > 0 Then 'ProjectileForm.Invalidate()
        'ProjectileForm.Refresh()
        'ProjectileForm.PictureBox1.Invalidate()
    End Sub


    Sub Process_Navi_Commands()


        'Move Navies
        If Host_Navi.OnGround = True Then
            If Host_Navi.Running = True Then
                If Host_Navi.Dashing = True Then 'Check for dashing
                    'Dashing
                    If Host_Navi.FaceLeft = True Then Host_Navi.Speed.X -= Host_Navi.DashSpeed Else Host_Navi.Speed.X += Host_Navi.DashSpeed
                Else
                    'Running
                    If Host_Navi.FaceLeft = True Then Host_Navi.Speed.X -= Host_Navi.GroundSpeed Else Host_Navi.Speed.X += Host_Navi.GroundSpeed
                End If
            End If
            'Jumping
            If Host_Navi.Jumping = True AndAlso Host_Navi.HasJumped = True Then Host_Navi.Speed.Y -= Host_Navi.Acrobatics : Host_Navi.HasJumped = False

        Else

            If Host_Navi.Running = True Then 'Air moving
                If Host_Navi.FaceLeft = True Then Host_Navi.Speed.X -= Host_Navi.AirSpeed Else Host_Navi.Speed.X += Host_Navi.AirSpeed
            End If
        End If


    End Sub

    Sub Update_Physics()


        For Each item In Projectile_List
            item.Location.X += item.Speed.X
            item.Location.Y += item.Speed.Y
        Next



        'Friction
        If Host_Navi.OnGround = True Then
            Host_Navi.Speed.X -= Host_Navi.Speed.X * GroundFriction.X
            Host_Navi.Speed.Y -= Host_Navi.Speed.Y * GroundFriction.Y
        Else
            Host_Navi.Speed.X -= Host_Navi.Speed.X * AirFriction.X
            Host_Navi.Speed.Y -= Host_Navi.Speed.Y * AirFriction.Y
        End If

        'Gravity
        If Host_Navi.OnGround = False Then Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y
        'Host_Navi.Speed.Y = Host_Navi.Speed.Y + Gravity.Y

        Host_Navi.Location.X = Host_Navi.Location.X + Host_Navi.Speed.X
        Host_Navi.Location.Y = Host_Navi.Location.Y + Host_Navi.Speed.Y

        'Bounds
        If Host_Navi.FaceLeft = True Then
            If Host_Navi.Navi_Location.Left <= Screen.PrimaryScreen.WorkingArea.Left Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - (Host_Navi.GetSize.X - Host_Navi.GetHitBox.Right) : Host_Navi.Speed.X = 0
        Else
            If Host_Navi.Navi_Location.Left <= Screen.PrimaryScreen.WorkingArea.Left Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - Host_Navi.GetHitBox.Left : Host_Navi.Speed.X = 0
        End If

        'If Host_Navi.Navi_Location.Left <= Screen.PrimaryScreen.WorkingArea.Left Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - (Host_Navi.GetSize.X - Host_Navi.GetHitBox.Right)
        If Host_Navi.Navi_Location.Right > Screen.PrimaryScreen.WorkingArea.Right Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox.Right : Host_Navi.Speed.X = 0
        If Host_Navi.Navi_Location.Bottom > Screen.PrimaryScreen.WorkingArea.Bottom Then Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetHitBox.Bottom
        If Host_Navi.Navi_Location.Bottom = Screen.PrimaryScreen.WorkingArea.Bottom Then Host_Navi.OnGround = True : Host_Navi.Speed.Y = 0 Else Host_Navi.OnGround = False

        'Update Location
        NaviForm.Left = Int(Host_Navi.Location.X)
        NaviForm.Top = Int(Host_Navi.Location.Y)

    End Sub

    Sub Navi_Redraw(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles NaviForm.Paint
        NaviForm.Width = Host_Navi.GetSize.X
        NaviForm.Height = Host_Navi.GetSize.Y
        e.Graphics.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor

        Dim xoff, yoff As Single
        xoff = CSng(-0.5 + 0.5 * Host_Navi.SpriteSize.X / Host_Navi.GetSize.X) + Host_Navi.SpriteSize.X * Host_Navi.Sprite.X
        yoff = CSng(-0.5 + 0.5 * Host_Navi.SpriteSize.Y / Host_Navi.GetSize.Y) + Host_Navi.SpriteSize.Y * Host_Navi.Sprite.Y

        If Host_Navi.FaceLeft = True Then
            'xoff = Navi.Form.SpriteSize.X * Navi.Sprite.X
            'yoff = Navi.Form.SpriteSize.Y * Navi.Sprite.Y
            'e.Graphics.DrawImage(NaviSprite(0, 0), New Rectangle(Navi.Form.Size.X, 0, -Navi.Size.X, Navi.Size.Y), xoff, yoff, Navi.Form.SpriteSize.X, Navi.Form.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage)
            e.Graphics.DrawImage(Host_Navi.SpriteSheet, New Rectangle(Host_Navi.GetSize.X, 0, -Host_Navi.GetSize.X - 1, Host_Navi.GetSize.Y), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage)
        Else
            e.Graphics.DrawImage(Host_Navi.SpriteSheet, New Rectangle(0, 0, Host_Navi.GetSize.X, Host_Navi.GetSize.Y), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage)
        End If


    End Sub

    Sub Set_color_filters()

        Dim NormalColorMatrixElements As Single()() = { _
           New Single() {1, 0, 0, 0, 0}, _
           New Single() {0, 1, 0, 0, 0}, _
           New Single() {0, 0, 1, 0, 0}, _
           New Single() {0, 0, 0, 1, 0}, _
           New Single() {0, 0, 0, 0, 1}}

        Dim BlueColorMatrixElements As Single()() = { _
           New Single() {1, 0, 0, 0, 0}, _
           New Single() {0, 1, 0, 0, 0}, _
           New Single() {0, 0, 2, 0, 0}, _
           New Single() {0, 0, 0, 1, 0}, _
           New Single() {-0.2, -0.2, 0, 0, 1}}


        NormalImage.SetColorMatrix(New Imaging.ColorMatrix(NormalColorMatrixElements), Imaging.ColorMatrixFlag.Default, Imaging.ColorAdjustType.Bitmap)
        BlueImage.SetColorMatrix(New Imaging.ColorMatrix(BlueColorMatrixElements), Imaging.ColorMatrixFlag.Default, Imaging.ColorAdjustType.Bitmap)

    End Sub

End Class
