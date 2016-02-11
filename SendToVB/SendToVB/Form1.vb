Public Class Form1

    Private Sub btnBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBrowse.Click
        If OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            txtAttachment.Text = OpenFileDialog1.FileName
        End If
    End Sub

    Private Sub btnSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSend.Click
        Dim mapi As SendFileTo.MAPI
        Dim recipient As SendFileTo.MapiRecipDesc

        mapi = New SendFileTo.MAPI()
        recipient = New SendFileTo.MapiRecipDesc()

        If Not mapi.ResolveName(txtTo.Text, recipient) Then
            Exit Sub
        End If

        mapi.AddRecipient(recipient)

        If txtAttachment.Text <> "" Then
            mapi.AddAttachment(txtAttachment.Text)
        End If

        If mapi.SendMail(txtSubject.Text, txtBody.Text) Then
            If txtTo.Text <> "" And (txtAttachment.Text <> "" Or (txtBody.Text <> "" And txtSubject.Text <> "")) Then
                'message sent directly
                MessageBox.Show("Message sent", "Send Mail", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                'will open browser window to edit mail and send manually
            End If
        End If
    End Sub

End Class
