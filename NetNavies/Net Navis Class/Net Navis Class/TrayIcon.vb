Class NaviTrayIcon
    Inherits ApplicationContext
    Public tray As New NotifyIcon
    Public WithEvents MainMenu As ContextMenuStrip
    Public WithEvents mnuHideNavi As ToolStripMenuItem
    Public WithEvents mnuSep1 As ToolStripSeparator
    Public WithEvents mnuExit As ToolStripMenuItem


    Sub Initialise(ByVal Navi As NetNavi_Type)
        mnuHideNavi = New ToolStripMenuItem("Hide Navi")
        mnuSep1 = New ToolStripSeparator()
        mnuExit = New ToolStripMenuItem("Close")
        MainMenu = New ContextMenuStrip
        MainMenu.Items.AddRange(New ToolStripItem() {mnuHideNavi, mnuSep1, mnuExit})

        tray = New NotifyIcon
        tray.Icon = Navi.Icon
        tray.ContextMenuStrip = MainMenu
        tray.Text = Navi.Navi_Name + " Tray Icon Service"
        tray.Visible = True
    End Sub


    Private Sub AppContext_ThreadExit(ByVal sender As Object, ByVal e As System.EventArgs) _
    Handles Me.ThreadExit
        'Guarantees that the icon will not linger.
        tray.Visible = False
    End Sub


    Private Sub mnuDisplayForm_Click(ByVal sender As Object, ByVal e As System.EventArgs) _
    Handles mnuHideNavi.Click

    End Sub

End Class
