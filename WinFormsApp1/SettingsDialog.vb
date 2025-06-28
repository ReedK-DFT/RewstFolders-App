Imports System.Windows.Forms

Public Class SettingsDialog

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click
        If String.IsNullOrWhiteSpace(TextBox1.Text) Then
            MessageBox.Show("Please enter a valid Webhook URL.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If String.IsNullOrWhiteSpace(TextBox2.Text) Then
            MessageBox.Show("Please enter a valid Rewst Secret.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        My.Settings.Webhook = TextBox1.Text.Trim()
        My.Settings.Rewst = TextBox2.Text.Trim()
        My.Settings.Font = Me.Font
        My.Settings.DarkMode = DarkModeCheckBox.Checked
        My.Settings.Save()
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub SettingsDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox1.Text = My.Settings.Webhook
        TextBox2.Text = My.Settings.Rewst
        Me.Font = My.Settings.Font
        DarkModeCheckBox.Checked = My.Settings.DarkMode
        If My.Settings.DarkMode Then
            Me.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            Me.ForeColor = System.Drawing.Color.AntiqueWhite
        Else
            Me.BackColor = SystemColors.Window
            Me.ForeColor = SystemColors.WindowText
        End If
    End Sub

    Private Sub FontButton_Click(sender As Object, e As EventArgs) Handles FontButton.Click
        If FontDialog1.ShowDialog = DialogResult.OK Then
            Me.Font = FontDialog1.Font
        End If
    End Sub

    Private Sub DarkModeCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DarkModeCheckBox.CheckedChanged
        If DarkModeCheckBox.Checked Then
            SetDarkMode(Me, True)
        Else
            SetDarkMode(Me, False)
        End If
    End Sub

    Public Shared Sub SetDarkMode(target As Control, dark As Boolean)

        If dark Then
            Dim originalBackColor As Color = target.BackColor
            Dim originalForeColor As Color = target.ForeColor
            target.BackColor = originalForeColor
            target.ForeColor = originalBackColor
            target.Tag = New Tuple(Of Color, Color)(originalBackColor, originalForeColor)
        Else
            If TypeOf target.Tag Is Tuple(Of Color, Color) Then
                Dim colors As Tuple(Of Color, Color) = CType(target.Tag, Tuple(Of Color, Color))
                target.BackColor = colors.Item1
                target.ForeColor = colors.Item2
                target.Tag = Nothing
            End If
        End If
        For Each child As Control In target.Controls
            If Not TypeOf child Is TableLayoutPanel AndAlso
                Not TypeOf child Is MenuStrip AndAlso
                Not TypeOf child Is StatusStrip Then
                If TypeOf child Is SplitContainer Then
                    Dim sc = CType(child, SplitContainer)
                    SetDarkMode(sc.Panel1, dark)
                    SetDarkMode(sc.Panel2, dark)
                Else
                    SetDarkMode(child, dark)
                End If

            End If

            'If TypeOf child Is Button Or TypeOf child Is CheckBox Or TypeOf child Is TextBox Or TypeOf child Is StatusStrip Or TypeOf child Is TreeView Or TypeOf child Is ListBox Then

            'End If
        Next
    End Sub
End Class
