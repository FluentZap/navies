Public Class NaviForm
    Public pressedkeys As New HashSet(Of Windows.Forms.Keys)
    Private Sub NaviForm_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        If pressedkeys.Contains(e.KeyCode) Then
        Else
            pressedkeys.Add(e.KeyCode)
        End If
    End Sub

    Private Sub NaviForm_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyUp
        If pressedkeys.Contains(e.KeyCode) Then
            pressedkeys.Remove(e.KeyCode)
        End If
    End Sub

    Private Sub NaviForm_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LostFocus
        pressedkeys.Clear()
    End Sub
End Class