namespace ManualKickoff
{
    partial class FormKickoff
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
            this.BtnWin10Test = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            //BtnWin10Test


            this.BtnWin10Test.Location = new System.Drawing.Point(118, 197);
            this.BtnWin10Test.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.BtnWin10Test.Name = "BtnWin10Test";
            this.BtnWin10Test.Size = new System.Drawing.Size(350, 51);
            this.BtnWin10Test.TabIndex = 3;
            this.BtnWin10Test.Text = "Kick Off";
            this.BtnWin10Test.UseVisualStyleBackColor = true;
            this.BtnWin10Test.Click += new System.EventHandler(this.BtnWin10Test_Click);

            // button1
            // 
            //this.button1.Location = new System.Drawing.Point(449, 482);
            //this.button1.Name = "button1";
            //this.button1.Size = new System.Drawing.Size(350, 50);
            //this.button1.TabIndex = 4;
            //this.button1.Text = "Kick Off Local Test";
            //this.button1.UseVisualStyleBackColor = true;
            //this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(125, 449);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(261, 29);
            this.label1.TabIndex = 6;
            this.label1.Text = "Work Item ID for testing";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(151, 497);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(211, 35);
            this.textBox1.TabIndex = 7;
            // 
            // FormKickoff
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(905, 685);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.BtnWin10Test);
            this.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.Name = "FormKickoff";
            this.Text = "Manual Kickoff";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button BtnWin10Test;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
    }
}

