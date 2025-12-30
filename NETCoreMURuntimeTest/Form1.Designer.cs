namespace NETCoreMURuntimeTest
{
    partial class NETCoreMUForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Release = new System.Windows.Forms.TextBox();
            this.textBox_Title = new System.Windows.Forms.TextBox();
            this.textBox_GUID = new System.Windows.Forms.TextBox();
            this.button_Start = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Release";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(31, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(27, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Title";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(31, 110);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "BundleGUID";
            // 
            // textBox_Release
            // 
            this.textBox_Release.Location = new System.Drawing.Point(110, 22);
            this.textBox_Release.Name = "textBox_Release";
            this.textBox_Release.Size = new System.Drawing.Size(436, 20);
            this.textBox_Release.TabIndex = 3;
            // 
            // textBox_Title
            // 
            this.textBox_Title.Location = new System.Drawing.Point(110, 64);
            this.textBox_Title.Name = "textBox_Title";
            this.textBox_Title.Size = new System.Drawing.Size(436, 20);
            this.textBox_Title.TabIndex = 4;
            // 
            // textBox_GUID
            // 
            this.textBox_GUID.Location = new System.Drawing.Point(110, 106);
            this.textBox_GUID.Name = "textBox_GUID";
            this.textBox_GUID.Size = new System.Drawing.Size(436, 20);
            this.textBox_GUID.TabIndex = 5;
            // 
            // button_Start
            // 
            this.button_Start.Location = new System.Drawing.Point(322, 193);
            this.button_Start.Name = "button_Start";
            this.button_Start.Size = new System.Drawing.Size(103, 23);
            this.button_Start.TabIndex = 6;
            this.button_Start.Text = "Start!!";
            this.button_Start.UseVisualStyleBackColor = true;
            this.button_Start.Click += new System.EventHandler(this.button_Start_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.Location = new System.Drawing.Point(443, 193);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(103, 23);
            this.button_Cancel.TabIndex = 6;
            this.button_Cancel.Text = "Cancel";
            this.button_Cancel.UseVisualStyleBackColor = true;
            this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // NETCoreMUForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(558, 228);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_Start);
            this.Controls.Add(this.textBox_GUID);
            this.Controls.Add(this.textBox_Title);
            this.Controls.Add(this.textBox_Release);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "NETCoreMUForm";
            this.Text = "Kickoff Runtime test for .NET Core MU";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Release;
        private System.Windows.Forms.TextBox textBox_Title;
        private System.Windows.Forms.TextBox textBox_GUID;
        private System.Windows.Forms.Button button_Start;
        private System.Windows.Forms.Button button_Cancel;
    }
}

