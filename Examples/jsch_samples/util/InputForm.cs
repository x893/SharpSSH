using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class InputForm : System.Windows.Forms.Form
	{
		private static InputForm inForm;
		bool btnOKClicked = false;

		private System.Windows.Forms.TextBox textBox;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		public InputForm()
		{
			InitializeComponent();
		}

		private static InputForm Instance
		{
			get
			{
				if (inForm == null)
					inForm = new InputForm();
				return inForm;
			}
		}

		public static string GetUserInput(string title, string devaultValue)
		{
			return GetUserInput(title, devaultValue, false);
		}

		public static string GetUserInput(string title, bool password)
		{
			return GetUserInput(title, "", password);
		}


		public static string GetUserInput(string title, string devaultValue, bool password)
		{
			Instance.Text = title;
			Instance.textBox.Text = devaultValue;
			Instance.PasswordField = password;

			if (Instance.PromptForInput())
				return Instance.textBox.Text;
			else
				throw new Exception("Canceled by user");
		}

		public static string GetFileFromUser(string msg)
		{
			OpenFileDialog chooser = new OpenFileDialog();
			chooser.Title = msg;
			DialogResult returnVal = chooser.ShowDialog();
			if (returnVal == DialogResult.OK)
				return chooser.FileName;
			else
				throw new Exception("Canceled by user");
		}

		public static bool PromptYesNo(string message)
		{
			return (DialogResult.Yes == MessageBox.Show(message, "SharpSSH", MessageBoxButtons.YesNo, MessageBoxIcon.Warning));
		}

		public static void ShowMessage(string message)
		{
			MessageBox.Show(message, "SharpSSH", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.textBox = new System.Windows.Forms.TextBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// textBox
			// 
			this.textBox.Location = new System.Drawing.Point(67, 28);
			this.textBox.Name = "textBox";
			this.textBox.Size = new System.Drawing.Size(192, 22);
			this.textBox.TabIndex = 0;
			// 
			// btnOK
			// 
			this.btnOK.Location = new System.Drawing.Point(67, 74);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(90, 26);
			this.btnOK.TabIndex = 1;
			this.btnOK.Text = "OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(173, 74);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(90, 26);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// InputForm
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(264, 110);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.textBox);
			this.Name = "InputForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "InputForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void btnOK_Click(object sender, System.EventArgs e)
		{
			btnOKClicked = true;
			Hide();
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			btnOKClicked = false;
			textBox.Text = "";
			Hide();
		}

		public bool PromptForInput()
		{
			ShowDialog();
			return btnOKClicked;
		}

		public string GetText()
		{
			return textBox.Text;
		}

		public void SetText(string text)
		{
			textBox.Text = text;
		}

		public bool PasswordField
		{
			get
			{
				return (textBox.PasswordChar.Equals(0));
			}
			set
			{
				if (value)
					textBox.PasswordChar = '*';
				else
					textBox.PasswordChar = '\0';
			}
		}
	}
}
