Module Vex
    Const Navi_Name As String = "Vex"
    Private Navi_ID As Long = My.Settings.NaviID

    Sub Main()

        Dim Navi_Instance As Navi_Main = New Navi_Main(Navi_Name, Navi_ID)
        Navi_Instance.Initialise()
        Do
            Application.DoEvents()
            Navi_Instance.DoEvents()
        Loop

    End Sub

End Module
