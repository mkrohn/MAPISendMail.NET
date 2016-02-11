Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace SendFileTo
    Class MAPI

        Private Const MAPI_LOGON_UI As Integer = &H1
        Private Const MAPI_DIALOG As Integer = &H8
        Private Const maxAttachments As Integer = 200

        ReadOnly errors() As String
        Dim m_lastError As Integer

        Dim m_recipients As List(Of MapiRecipDesc)
        Dim m_attachments As List(Of String)
        
        Enum howTo
            MAPI_ORIG = 0
            MAPI_TO
            MAPI_CC  'not supported
            MAPI_BCC 'not supported
        End Enum

        Public Sub New()
            errors = New String() {"OK [0]", "User abort [1]", "General MAPI failure [2]", "MAPI login failure [3]", _
                                "Disk full [4]", "Insufficient memory [5]", "Access denied [6]", "-unknown- [7]", _
                                "Too many sessions [8]", "Too many files were specified [9]", _
                                "Too many recipients were specified [10]", _
                                "A specified attachment was not found [11]", "Attachment open failure [12]", _
                                "Attachment write failure [13]", "Unknown recipient [14]", _
                                "Bad recipient type [15]", "No messages [16]", "Invalid message [17]", _
                                "Text too large [18]", "Invalid session [19]", "Type not supported [20]", _
                                "A recipient was specified ambiguously [21]", "Message in use [22]", "Network failure [23]", _
                                "Invalid edit fields [24]", "Invalid recipients [25]", "Not supported [26]"}

            m_recipients = New List(Of MapiRecipDesc)
            m_attachments = New List(Of String)
            m_lastError = 0
        End Sub

        Public Sub AddAttachment(ByVal strAttachmentFileName As String)
            m_attachments.Add(strAttachmentFileName)
        End Sub

        <DllImport("MAPI32.DLL")> _
            Private Shared Function MAPISendMail(ByVal sess As IntPtr, ByVal hwnd As IntPtr, ByVal message As MapiMessage, ByVal flg As Integer, ByVal rsv As Integer) As Integer
        End Function

        <DllImport("MAPI32.DLL")> _
            Private Shared Function MAPIResolveName(ByVal sess As IntPtr, ByVal hwnd As IntPtr, ByVal name As String, ByVal flg As Integer, ByVal rsv As Integer, ByRef recipient As MapiRecipDesc) As Integer
        End Function

        Public Function SendMail(ByVal strSubject As String, ByVal strBody As String) As Boolean
            Dim msg As MapiMessage

            msg = New MapiMessage
            msg.subject = strSubject
            msg.noteText = strBody
            msg.recips = GetRecipients(msg.recipCount)
            msg.files = GetAttachments(msg.fileCount)

            m_lastError = MAPISendMail(0, 0, msg, MAPI_LOGON_UI + MAPI_DIALOG, 0)
            If m_lastError > 1 Then
                MessageBox.Show("MAPISendMail failed! " + GetLastError(), "MAPISendMail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Cleanup(msg)
                Return False
            End If

            Cleanup(msg)
            Return True
        End Function

        Public Function AddRecipient(ByVal sEmail As String, Optional ByVal sName As String = "") As Boolean
            Dim recipient As MapiRecipDesc

            recipient = New MapiRecipDesc()
            recipient.recipClass = howTo.MAPI_TO
            recipient.address = sEmail
            recipient.name = sName

            m_recipients.Add(recipient)
            Return True
        End Function

        Public Function AddRecipient(ByRef recipient As MapiRecipDesc) As Boolean
            m_recipients.Add(recipient)
            Return True
        End Function

        Public Function ResolveName(ByVal sName As String, ByRef recipient As MapiRecipDesc) As Boolean

            m_lastError = MAPIResolveName(0, 0, sName, 0, 0, recipient)

            If m_lastError > 1 Then
                MessageBox.Show("MAPIResolveName failed! " + GetLastError(), "ResolveName", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return False
            End If

            Return True

        End Function

        Private Function GetRecipients(ByRef recipCount As Integer) As IntPtr
            Dim size As Integer
            Dim intPtr As IntPtr
            Dim ptr As Integer
            Dim mapiDesc As MapiRecipDesc

            If m_recipients.Count = 0 Then
                recipCount = 0
                Return 0
            End If

            size = Marshal.SizeOf(GetType(MapiRecipDesc))
            intPtr = Marshal.AllocHGlobal(m_recipients.Count * size)

            ptr = CType(intPtr, Integer)

            For Each mapiDesc In m_recipients
                Marshal.StructureToPtr(mapiDesc, CType(ptr, IntPtr), False)
                ptr += Size
            Next

            recipCount = m_recipients.Count
            Return intPtr
        End Function

        Private Function GetAttachments(ByRef fileCount As Integer) As IntPtr
            Dim mapiFileDesc As MapiFileDesc
            Dim size As Integer
            Dim intPtr As IntPtr
            Dim ptr As Integer
            Dim strAttachment As String

            If m_attachments Is Nothing Then
                fileCount = 0
                Return 0
            End If

            If (m_attachments.Count <= 0) Or (m_attachments.Count > maxAttachments) Then
                fileCount = 0
                Return 0
            End If

            size = Marshal.SizeOf(GetType(MapiFileDesc))
            intPtr = Marshal.AllocHGlobal(m_attachments.Count * size)

            MapiFileDesc = New MapiFileDesc()
            MapiFileDesc.position = -1
            ptr = CType(intPtr, Integer)

            For Each strAttachment In m_attachments
                MapiFileDesc.name = Path.GetFileName(strAttachment)
                MapiFileDesc.path = strAttachment
                Marshal.StructureToPtr(MapiFileDesc, CType(ptr, IntPtr), False)
                ptr += size
            Next

            fileCount = m_attachments.Count
            Return intPtr

        End Function

        Private Sub Cleanup(ByRef msg As MapiMessage)
            Dim size As Integer
            Dim ptr As Integer
            Dim i As Integer

            If msg.recips <> IntPtr.Zero Then
                size = Marshal.SizeOf(GetType(MapiRecipDesc))
                ptr = CType(msg.recips, Integer)

                For i = 0 To msg.recipCount - 1 Step i + 1
                    Marshal.DestroyStructure(CType(ptr, IntPtr), GetType(MapiRecipDesc))
                    ptr += size
                Next
                Marshal.FreeHGlobal(msg.recips)
            End If

            If msg.files <> IntPtr.Zero Then
                size = Marshal.SizeOf(GetType(MapiFileDesc))
                ptr = CType(msg.files, Integer)

                For i = 0 To msg.fileCount - 1 Step i + 1
                    Marshal.DestroyStructure(CType(ptr, IntPtr), GetType(MapiFileDesc))
                    ptr += size
                Next
                Marshal.FreeHGlobal(msg.files)
            End If

            m_recipients.Clear()
            m_attachments.Clear()
            m_lastError = 0
        End Sub

        Public Function GetLastError() As String
            If m_lastError <= 26 Then
                Return errors(m_lastError)
            End If
            Return "MAPI error [" + m_lastError.ToString() + "]"
        End Function

    End Class

    <StructLayout(LayoutKind.Sequential)> _
    Public Class MapiMessage
        Public reserved As Integer
        Public subject As String
        Public noteText As String
        Public messageType As String
        Public dateReceived As String
        Public conversationID As String
        Public flags As Integer
        Public originator As IntPtr
        Public recipCount As Integer
        Public recips As IntPtr
        Public fileCount As Integer
        Public files As IntPtr
    End Class

    <StructLayout(LayoutKind.Sequential)> _
    Public Class MapiFileDesc
        Public reserved As Integer
        Public flags As Integer
        Public position As Integer
        Public path As String
        Public name As String
        Public type As IntPtr
    End Class

    <StructLayout(LayoutKind.Sequential)> _
    Public Class MapiRecipDesc
        Public reserved As Integer
        Public recipClass As Integer
        Public name As String
        Public address As String
        Public eIDSize As Integer
        Public enTryID As IntPtr
    End Class
End Namespace
