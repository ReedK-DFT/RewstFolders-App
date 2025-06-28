Imports System.Globalization
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json.Nodes
Imports RewstFolders.Folders

Public Class Form1
    Private workflowJson As JsonObject
    Private nameIndex As New Dictionary(Of String, List(Of TreeNode))

    Private Function LoadFolders() As FolderCollection
        Dim folders As New Folders.FolderCollection

        If workflowJson IsNot Nothing Then
            Dim items = workflowJson.Item("result").AsArray()
            For Each item In items
                Dim itemName = item.Item("name").AsValue.ToString.Trim
                If itemName.StartsWith("[") Then
                    Dim newtag = NormalizeName(itemName.Substring(1, itemName.IndexOf("]") - 1))
                    For Each t In item.Item("tags").AsArray()
                        If NormalizeName(t.Item("name").AsValue.ToString) = newtag Then
                            newtag = String.Empty
                            Exit For
                        End If
                    Next
                    If Not String.IsNullOrEmpty(newtag) Then
                        Dim itemArray = item.Item("tags").AsArray()
                        itemArray.Add(New JsonObject From {{"name", newtag}})
                        If newtag.ToLower.StartsWith("rewst") Then
                            itemArray.Insert(0, New JsonObject From {{"name", "Rewst"}})
                        End If
                    Else
                        If itemName.Trim.ToLower.StartsWith("rewst") Then
                            Dim itemTags = item.Item("tags").AsArray()
                            itemTags.Insert(0, New JsonObject From {{"name", "Rewst"}})
                        End If
                    End If
                End If

                Dim tags = (From t In item.Item("tags").AsArray Select NormalizeName(t.Item("name").AsValue.ToString))
                Dim index As New TagIndex(tags)
                Dim folder As Folders.Folder
                If folders.Contains(index) Then
                    folder = folders(index)
                Else
                    folder = New Folders.Folder
                    folder.Name = itemName
                    folder.Index = index
                    folders.Add(folder)
                End If
            Next

            If folders.Count > 0 Then
                TreeView1.Nodes.Clear()

                For Each item In items
                    Dim itemTags = (From t In item.Item("tags").AsArray Select NormalizeName(t.Item("name").AsValue.ToString))
                    Dim itemIndex As New TagIndex(itemTags)
                    Dim nodeName = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Item("name").AsValue.ToJsonString))
                    If itemIndex.Count = 0 Then
                        Dim workflowNode As New TreeNode(item.Item("name").AsValue.ToString) With {.ImageIndex = 2, .SelectedImageIndex = 2, .Tag = item, .Name = nodeName}
                        TreeView1.Nodes.Add(workflowNode)
                        AddToIndex(workflowNode)
                    Else
                        For Each folder In folders
                            If folder.Index.Equals(itemIndex) Then
                                Dim curNodes = TreeView1.Nodes
                                Dim tags = folder.Index.ToList()
                                For Each t In tags
                                    Dim keyName = Convert.ToBase64String(Encoding.UTF8.GetBytes(t))
                                    If Not curNodes.ContainsKey(keyName) Then
                                        Dim newNode = New TreeNode(t) With {.ImageIndex = 0, .SelectedImageIndex = 0, .Name = keyName}
                                        curNodes.Add(newNode)
                                        curNodes = newNode.Nodes
                                    Else
                                        curNodes = curNodes(keyName).Nodes
                                    End If
                                Next
                                If Not curNodes.ContainsKey(nodeName) Then
                                    Dim workflowNode As New TreeNode(item.Item("name").AsValue.ToString) With {.ImageIndex = 2, .SelectedImageIndex = 2, .Tag = item, .Name = nodeName}
                                    curNodes.Add(workflowNode)
                                    AddToIndex(workflowNode)
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        End If
        Return folders
    End Function

    Private Async Function LoadWorkflows() As Task
        nameIndex.Clear()
        workflowJson = Nothing
        Dim client As New HttpClient()
        Dim request As New HttpRequestMessage(HttpMethod.Get, My.Settings.Webhook)
        request.Headers.Add("x-rewst-secret", My.Settings.Rewst)
        Try
            Using result = Await client.SendAsync(request)
                Dim resultStream = result.Content.ReadAsStream
                workflowJson = Await JsonObject.ParseAsync(resultStream)
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading workflows: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            client.Dispose()
        End Try
    End Function

    Private Function NormalizeName(name As String) As String
        Dim normalized = name.Trim().ToLower()
        Dim parts = normalized.Split(" "c, StringSplitOptions.RemoveEmptyEntries Or StringSplitOptions.TrimEntries)
        For i = 0 To parts.Length - 1
            parts(i) = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts(i))
        Next
        normalized = String.Join(" ", parts)
        If normalized.StartsWith("[") AndAlso Not normalized.Contains("]") Then normalized = normalized.Substring(1)
        Return normalized
    End Function

    Private Sub AddToIndex(node As TreeNode)
        If Not nameIndex.ContainsKey(node.Text) Then
            nameIndex(node.Text) = New List(Of TreeNode)()
        End If
        nameIndex(node.Text).Add(node)
    End Sub

    Private Async Sub LoadFoldersToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadFoldersToolStripMenuItem.Click
        LoadFoldersToolStripMenuItem.Enabled = False
        Me.Cursor = Cursors.WaitCursor

        Await LoadWorkflows()
        Dim folders = LoadFolders()

        Dim nodeSorter As Action(Of TreeNodeCollection) = Sub(nodes As TreeNodeCollection)
                                                              Dim folderNodes As New List(Of TreeNode)()
                                                              For i = nodes.Count - 1 To 0 Step -1
                                                                  Dim node = nodes(i)
                                                                  If node.Nodes.Count > 0 Then
                                                                      folderNodes.Add(node)
                                                                      nodes.RemoveAt(i)
                                                                  End If
                                                              Next
                                                              folderNodes.Sort(Function(a, b) String.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase))
                                                              For i = folderNodes.Count - 1 To 0 Step -1
                                                                  nodes.Insert(0, folderNodes(i))
                                                              Next
                                                              For Each node In folderNodes
                                                                  nodeSorter(node.Nodes)
                                                              Next
                                                          End Sub

        nodeSorter(TreeView1.Nodes)
        StatusLabel.Text = $"Loaded {folders.Count} workflows."
        Me.Cursor = Cursors.Default
        LoadFoldersToolStripMenuItem.Enabled = True
    End Sub

    Private Sub TreeView1_DoubleClick(sender As Object, e As EventArgs) Handles TreeView1.DoubleClick
        Dim node = TreeView1.SelectedNode
        If node?.Tag IsNot Nothing AndAlso TypeOf node?.Tag Is JsonObject Then
            Dim item As JsonObject = CType(node.Tag, JsonObject)
            Dim url As String = $"https://app.rewst.io/organizations/{item.Item("org").Item("id").AsValue.ToString}/workflows/{item.Item("id").AsValue.ToString}"
            Process.Start("cmd.exe", $"/c start {url}")
        End If
    End Sub

    Private Sub SettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SettingsToolStripMenuItem.Click
        Using dlg As New SettingsDialog
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Me.Font = My.Settings.Font
                If My.Settings.DarkMode Then
                    SettingsDialog.SetDarkMode(Me, True)
                Else
                    SettingsDialog.SetDarkMode(Me, False)
                End If
            End If
        End Using
    End Sub

    Private Sub SearchToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SearchToolStripMenuItem.Click
        SplitContainer1.Panel1Collapsed = Not SearchToolStripMenuItem.Checked
    End Sub

    Private Sub SearchTextBox_KeyUp(sender As Object, e As KeyEventArgs) Handles SearchTextBox.KeyUp
        If e.KeyCode = Keys.Enter Then
            SearchTextBox.Text = SearchTextBox.Text.Trim()
            Button1.PerformClick()
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If SearchTextBox.TextLength > 0 Then
            For Each key In nameIndex.Keys
                If key.ToLower().Contains(SearchTextBox.Text.ToLower()) Then
                    For i = 0 To nameIndex(key).Count - 1
                        ResultsListBox.Items.Add(nameIndex(key)(i))
                    Next
                End If
            Next
            ResultsListBox.DisplayMember = "Text"
        End If

        If ResultsListBox.Items.Count > 0 Then
            SplitContainer2.Panel2Collapsed = False
            StatusLabel.Text = $"{ResultsListBox.Items.Count} results listed."
        Else
            SplitContainer2.Panel2Collapsed = True
            StatusLabel.Text = "No results found."
        End If
        SearchTextBox.Clear()
    End Sub

    Private Sub CloseResultsButton_Click(sender As Object, e As EventArgs) Handles CloseResultsButton.Click
        SplitContainer2.Panel2Collapsed = True
    End Sub

    Private Sub ClearResultsButton_Click(sender As Object, e As EventArgs) Handles ClearResultsButton.Click
        ResultsListBox.Items.Clear()
        SplitContainer2.Panel2Collapsed = True
    End Sub

    Private Sub ResultsListBox_DoubleClick(sender As Object, e As EventArgs) Handles ResultsListBox.DoubleClick
        TreeView1.SelectedNode = CType(ResultsListBox.SelectedItem, TreeNode)
        TreeView1.SelectedNode.EnsureVisible()
    End Sub

    Private Sub TreeView1_AfterExpand(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterExpand
        e.Node.ImageIndex = 1
        e.Node.SelectedImageIndex = 1
    End Sub

    Private Sub TreeView1_AfterCollapse(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterCollapse
        e.Node.ImageIndex = 0
        e.Node.SelectedImageIndex = 0
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Font = My.Settings.Font
        If String.IsNullOrEmpty(My.Settings.Webhook) OrElse String.IsNullOrEmpty(My.Settings.Rewst) Then
            Using dlg As New SettingsDialog
                If dlg.ShowDialog(Me) <> DialogResult.OK Then
                    Application.Exit()
                End If
            End Using
        End If
    End Sub

    Private Sub CollapseAllToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CollapseAllToolStripMenuItem.Click
        TreeView1.CollapseAll()
    End Sub

    Private Sub ExpandAllToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExpandAllToolStripMenuItem.Click
        TreeView1.ExpandAll()
    End Sub
End Class
