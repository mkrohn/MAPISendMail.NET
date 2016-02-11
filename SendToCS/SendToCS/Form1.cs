using System;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void OnBrowseClicked(object sender, MouseEventArgs e)
        {
            if (DialogResult.OK == openFileDialog1.ShowDialog())
                txtAttachment.Text = openFileDialog1.FileName;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendFileTo.MAPI mapi;
            SendFileTo.MapiRecipDesc recipient;

            mapi = new SendFileTo.MAPI();
            recipient = new SendFileTo.MapiRecipDesc();

            if (! mapi.ResolveName(txtTo.Text, ref recipient) )
                return;

            mapi.AddRecipient(recipient);

            if (txtAttachment.Text != "" )
                mapi.AddAttachment(txtAttachment.Text);

            if ( mapi.SendMail(txtSubject.Text, txtBody.Text) )
                if (txtTo.Text != "" && (txtAttachment.Text != "" || (txtBody.Text != "" && txtSubject.Text != "")) )
                {
                    //message sent directly
                    MessageBox.Show("Message sent", "Send Mail", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } else {
                    //will open browser window to edit mail and send manually
                }
        }
    }
}