'Main Sub's go into Navi Main
'Navi Resources is for any Navi specific Data
'Host app calls Initialise then runs DoEvents
'DXon if switched on changes rendering to a directx window rendering is then done in Draw_DX

Partial Public Class Navi_Main
    Private WithEvents NaviForm As NaviForm
    Private WithEvents MenuForm As MenuForm
    Private WithEvents NaviDX As NaviDX
    Private WithEvents NaviTray As NaviTrayIcon
    Public pressedkeys As New HashSet(Of Windows.Forms.Keys)

    'DirectX
    Dim DXDevice As Device
    Dim DXPP As PresentParameters
    Dim DXSprite As Sprite
    Dim DXNaviTexture() As Texture
    Dim DXProjectileTexture() As Texture

    Dim Init_DirectX As Boolean
    Dim DXOn As Boolean ' = True


    Dim NormalImage As New Imaging.ImageAttributes
    Dim BlueImage As New Imaging.ImageAttributes
    Dim RedImage As New Imaging.ImageAttributes
    Dim GreenImage As New Imaging.ImageAttributes

    Public Gravity As PointF = New PointF(0, 0.5)
    Public AirFriction As New PointF(0.01, 0.01)
    Public GroundFriction As New PointF(0.15, 0)

    Public Host_Navi As NetNavi_Type
    Public Client_Navi As NetNavi_Type

    Dim Direct_Control As Boolean = True


    Private Physics_Timer As Double
    Private Physics_Rate As Double
    Private Program_Step As Long


    Class Projectiles_Type
        Public Location As PointF
        Public Speed As PointF
        Public Life As Integer

        Sub New(ByVal Location As Point, ByVal Speed As PointF, ByVal Life As Integer)
            Me.Location = Location
            Me.Speed = Speed
            Me.Life = Life
        End Sub

    End Class

    Public Projectile_List As HashSet(Of Projectiles_Type) = New HashSet(Of Projectiles_Type)


    Sub New(ByVal Navi_Name As String, ByVal NaviID As Long)
        Host_Navi = Get_Data(Navi_Name, NaviID)


    End Sub



    Sub Initialise()
        'Create and show forms
        NaviForm = New NaviForm
        MenuForm = New MenuForm
        NaviForm.Show()
        NaviForm.TopMost = True
        NaviForm.Width = CInt(Host_Navi.GetSize.X)
        NaviForm.Height = CInt(Host_Navi.GetSize.Y)
        NaviForm.Left = CInt(Host_Navi.Location.X)
        NaviForm.Top = CInt(Host_Navi.Location.Y)

        Host_Navi.Get_Compact_buffer()

        'Initialise_Network()

        'Initialise defaults
        'NaviTray = New NaviTrayIcon
        'NaviTray.Initialise(Host_Navi)
        Set_color_filters()

        Physics_Timer = DateAndTime.Timer
        Physics_Rate = 1 / 60
        Program_Step = 0

        'Host_Navi.set_Animation(Animation_Name_Enum.None)

        Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetSize.Y
        Host_Navi.Location.X = 1000

        Host_Navi.Scale = 3

    End Sub


    Sub DoEvents()
        'Slowing program down
        System.Threading.Thread.Sleep(CInt(Physics_Rate * 1000))

        If Physics_Timer <= DateAndTime.Timer Then
            Handle_UI()
            Process_Navi_Commands()
            Update_Physics()
            Set_Correct_Animation(Host_Navi)
            Host_Navi.Update_Sprite()
            'DoNetworkEvents()

            Physics_Timer = DateAndTime.Timer + Physics_Rate
            Program_Step += 1
        End If

        Draw_All()
    End Sub


    Sub Handle_UI()
        If pressedkeys.Contains(Keys.W) Then
            Host_Navi.Scale += 0.5F
            Host_Navi.OldSprite = New Point(500, 500)
            If Host_Navi.Scale < 0.5 Then Host_Navi.Scale = 0.5
        End If

        If pressedkeys.Contains(Keys.S) Then
            Host_Navi.Scale -= 0.5F
            Host_Navi.OldSprite = New Point(500, 500)
            If Host_Navi.Scale < 0.5 Then Host_Navi.Scale = 0.5
        End If


        If pressedkeys.Contains(Keys.A) Then
            Host_Navi.FaceLeft = True
            Host_Navi.Running = True
            If pressedkeys.Contains(Keys.ShiftKey) Then Host_Navi.Dashing = True : Host_Navi.HasDashed = True Else Host_Navi.Dashing = False
        End If

        If pressedkeys.Contains(Keys.D) Then
            Host_Navi.FaceLeft = False
            Host_Navi.Running = True
            If pressedkeys.Contains(Keys.ShiftKey) Then Host_Navi.Dashing = True : Host_Navi.HasDashed = True Else Host_Navi.Dashing = False
        End If

        If Not pressedkeys.Contains(Keys.ShiftKey) Then Host_Navi.Dashing = False

        If Not pressedkeys.Contains(Keys.D) AndAlso Not pressedkeys.Contains(Keys.A) Then
            Host_Navi.Running = False
        End If


        If pressedkeys.Contains(Keys.Space) Then
            If Host_Navi.OnGround = True Then
                Host_Navi.Jumping = True
                Host_Navi.HasJumped = True
            End If
        End If

        If pressedkeys.Contains(Keys.Tab) Then If DXOn = False Then DXOn = True Else NaviDX.Dispose()


        If pressedkeys.Contains(Keys.OemQuestion) Then
            If Host_Navi.FaceLeft = True Then
                Projectile_List.Add(New Projectiles_Type(New Point(CInt(Host_Navi.Navi_Location.Left), CInt(Host_Navi.Navi_Location.Top)), New PointF(-20, 0), 600))
            Else
                Projectile_List.Add(New Projectiles_Type(New Point(CInt(Host_Navi.Navi_Location.Right), CInt(Host_Navi.Navi_Location.Top)), New PointF(20, 0), 600))
            End If

            pressedkeys.Remove(Keys.OemQuestion)
        End If


    End Sub

    Sub Draw_All()
        If DXOn = False Then
            If Not Host_Navi.Sprite = Host_Navi.OldSprite OrElse Not Host_Navi.FaceLeft = Host_Navi.OldFaceLeft Then
                NaviForm.Invalidate()
                Host_Navi.OldSprite = Host_Navi.Sprite
                Host_Navi.OldFaceLeft = Host_Navi.FaceLeft
            End If
        End If

        If DXOn = True Then
            If Init_DirectX = False Then Start_Directx()

            Draw_DX()

        End If

    End Sub


    Sub Process_Navi_Commands()

        If Direct_Control = False Then
            Host_Navi.Running = False
            If Host_Navi.Navi_Location.Right <= Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox.Width Then
                Host_Navi.FaceLeft = False
                Host_Navi.Running = True
            End If
            If Host_Navi.Navi_Location.Right >= Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox.Width Then
                Host_Navi.FaceLeft = True
            End If
        End If



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

        Dim Item_Remove_List As New HashSet(Of Projectiles_Type)
        For Each item In Projectile_List
            item.Location.X += item.Speed.X
            item.Location.Y += item.Speed.Y
            item.Life -= 1
            If item.Life <= 0 Then Item_Remove_List.Add(item)
        Next

        For Each item In Item_Remove_List
            Projectile_List.Remove(item)
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

        Host_Navi.Location.X = Host_Navi.Location.X + Host_Navi.Speed.X * Host_Navi.Scale
        Host_Navi.Location.Y = Host_Navi.Location.Y + Host_Navi.Speed.Y * Host_Navi.Scale

        'Bounds
        If Host_Navi.FaceLeft = True Then
            If Host_Navi.Navi_Location.Left <= Screen.PrimaryScreen.WorkingArea.Left Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - (Host_Navi.GetSize.X - Host_Navi.GetHitBox.Right) : Host_Navi.Speed.X = 0
        Else
            If Host_Navi.Navi_Location.Left <= Screen.PrimaryScreen.WorkingArea.Left Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Left - Host_Navi.GetHitBox.Left : Host_Navi.Speed.X = 0
        End If

        If Host_Navi.FaceLeft = True Then
            If Host_Navi.Navi_Location.Right >= Screen.PrimaryScreen.WorkingArea.Right Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - (Host_Navi.GetSize.X - Host_Navi.GetHitBox.Left) : Host_Navi.Speed.X = 0
        Else
            If Host_Navi.Navi_Location.Right >= Screen.PrimaryScreen.WorkingArea.Right Then Host_Navi.Location.X = Screen.PrimaryScreen.WorkingArea.Right - Host_Navi.GetHitBox.Right : Host_Navi.Speed.X = 0
        End If

        If Host_Navi.Navi_Location.Bottom > Screen.PrimaryScreen.WorkingArea.Bottom Then Host_Navi.Location.Y = Screen.PrimaryScreen.WorkingArea.Bottom - Host_Navi.GetHitBox.Bottom
        If Host_Navi.Navi_Location.Bottom = Screen.PrimaryScreen.WorkingArea.Bottom Then Host_Navi.OnGround = True : Host_Navi.Speed.Y = 0 Else Host_Navi.OnGround = False

        'Update Location
        NaviForm.Left = CInt(Host_Navi.Location.X)
        NaviForm.Top = CInt(Host_Navi.Location.Y)

        If IsClient = True Then NaviForm.Left = CInt(Host_Navi.Location.X) + 32
    End Sub

    Sub Navi_Redraw(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles NaviForm.Paint
        NaviForm.Width = CInt(Host_Navi.GetSize.X)
        NaviForm.Height = CInt(Host_Navi.GetSize.Y)
        e.Graphics.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor

        Dim xoff, yoff As Single
        xoff = CSng(-0.5 + 0.5 * Host_Navi.SpriteSize.X / Host_Navi.GetSize.X) + Host_Navi.SpriteSize.X * Host_Navi.Sprite.X
        yoff = CSng(-0.5 + 0.5 * Host_Navi.SpriteSize.Y / Host_Navi.GetSize.Y) + Host_Navi.SpriteSize.Y * Host_Navi.Sprite.Y

        If Host_Navi.FaceLeft = True Then
            e.Graphics.DrawImage(Host_Navi.SpriteSheet, New Rectangle(CInt(Host_Navi.GetSize.X), 0, CInt(-Host_Navi.GetSize.X - 1), CInt(Host_Navi.GetSize.Y)), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage)
        Else
            e.Graphics.DrawImage(Host_Navi.SpriteSheet, New Rectangle(0, 0, CInt(Host_Navi.GetSize.X), CInt(Host_Navi.GetSize.Y)), xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y, GraphicsUnit.Pixel, NormalImage)
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





    Sub Start_Directx()
        NaviDX = New NaviDX
        NaviDX.Show()
        NaviDX.Width = Screen.PrimaryScreen.WorkingArea.Width
        NaviDX.Height = Screen.PrimaryScreen.WorkingArea.Height
        'NaviDX.Location = New Point(0, Screen.PrimaryScreen.Bounds.Height - NaviDX.Height)
        NaviForm.Hide()
        DXPP = New PresentParameters
        DXPP.BackBufferHeight = NaviDX.Height
        DXPP.BackBufferWidth = NaviDX.Width
        DXPP.SwapEffect = SwapEffect.Copy
        DXPP.PresentationInterval = PresentInterval.Immediate
        DXPP.Windowed = True

        DXDevice = New Device(0, DeviceType.Hardware, NaviDX, CreateFlags.HardwareVertexProcessing, DXPP)
        DXSprite = New Sprite(DXDevice)


        ReDim DXNaviTexture(10)
        'My.Resources.Raven.MakeTransparent(Color.FromArgb(255, 0, 255, 0))
        DXNaviTexture(0) = New Texture(DXDevice, CType(Host_Navi.SpriteSheet.Clone, Drawing.Bitmap), Usage.None, Pool.Managed)


        ReDim DXProjectileTexture(10)
        DXProjectileTexture(0) = New Texture(DXDevice, CType(My.Resources.Shot2.Clone, Drawing.Bitmap), Usage.None, Pool.Managed)



        Init_DirectX = True
    End Sub


    Sub Draw_DX()

        DXDevice.Clear(ClearFlags.Target, Color.Transparent, 0, 1)
        DXDevice.BeginScene()
        DXSprite.Begin(SpriteFlags.AlphaBlend)
        DXDevice.SetSamplerState(0, SamplerStageStates.MagFilter, TextureFilter.Point)
        Dim xoff, yoff As Integer
        xoff = Host_Navi.SpriteSize.X * Host_Navi.Sprite.X
        yoff = Host_Navi.SpriteSize.Y * Host_Navi.Sprite.Y

        'DXSprite.Draw(DXNaviTexture(0), New Rectangle(xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y), Vector3.Empty, New Vector3(Host_Navi.Location.X, Host_Navi.Location.Y, 0), Color.White)

        If Host_Navi.FaceLeft = True Then
            DXSprite.Transform = Matrix.Transformation2D(New Vector2(0, 0), 0, New Vector2(-Host_Navi.Scale, Host_Navi.Scale), New Vector2(0, 0), 0, New Vector2(Host_Navi.Location.X + (Host_Navi.SpriteSize.X * Host_Navi.Scale), Host_Navi.Location.Y))
            DXSprite.Draw(DXNaviTexture(0), New Rectangle(xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y), Vector3.Empty, New Vector3(0, 0, 0), Color.White)
        Else
            DXSprite.Transform = Matrix.Transformation2D(New Vector2(0, 0), 0, New Vector2(Host_Navi.Scale, Host_Navi.Scale), New Vector2(0, 0), 0, New Vector2(Host_Navi.Location.X, Host_Navi.Location.Y))
            DXSprite.Draw(DXNaviTexture(0), New Rectangle(xoff, yoff, Host_Navi.SpriteSize.X, Host_Navi.SpriteSize.Y), Vector3.Empty, New Vector3(0, 0, 0), Color.White)
        End If

        For Each item In Projectile_List
            DXSprite.Transform = Matrix.Transformation2D(New Vector2(0, 0), 0, New Vector2(3, 3), New Vector2(0, 0), 0, New Vector2(item.Location.X, item.Location.Y))
            DXSprite.Draw(DXProjectileTexture(0), New Rectangle(0, 0, 8, 6), Vector3.Empty, New Vector3(0, 0, 0), Color.White)
        Next

        DXSprite.Transform = Matrix.Identity
        DXSprite.Draw(DXProjectileTexture(0), New Rectangle(0, 0, 8, 6), Vector3.Empty, New Vector3(0, 0, 0), Color.White)

        DXSprite.End()

        DXDevice.EndScene()
        DXDevice.Present()

    End Sub


    Private Sub NaviForm_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles NaviForm.KeyDown
        If pressedkeys.Contains(e.KeyCode) Then
        Else
            pressedkeys.Add(e.KeyCode)
        End If
    End Sub

    Private Sub NaviForm_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles NaviForm.KeyUp
        If pressedkeys.Contains(e.KeyCode) Then
            pressedkeys.Remove(e.KeyCode)
        End If
    End Sub

    Private Sub NaviForm_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles NaviForm.GotFocus
        Direct_Control = True
    End Sub

    Private Sub NaviForm_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles NaviForm.LostFocus
        If DXOn = False Then Direct_Control = False
        pressedkeys.Clear()
    End Sub

    Private Sub NaviForm_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles NaviForm.Disposed
        'NaviTray.tray.Visible = True
    End Sub


    Private Sub NaviDX_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles NaviDX.KeyDown
        If pressedkeys.Contains(e.KeyCode) Then
        Else
            pressedkeys.Add(e.KeyCode)
        End If
    End Sub

    Private Sub NaviDX_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles NaviDX.KeyUp
        If pressedkeys.Contains(e.KeyCode) Then
            pressedkeys.Remove(e.KeyCode)
        End If
    End Sub

    Private Sub NaviDX_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles NaviDX.LostFocus
        pressedkeys.Clear()
    End Sub

    Private Sub NaviDX_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles NaviDX.Disposed
        DXOn = False
        Init_DirectX = False
        DXDevice = Nothing
        DXSprite = Nothing
        DXNaviTexture = Nothing
        DXPP = Nothing
        NaviForm.Show()
    End Sub



End Class
