using System;
using System.Windows.Forms;

namespace FIleSearch
{
    partial class FileSearchForm
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
            this.directoryRoot = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numberOfFiles = new System.Windows.Forms.TextBox();
            this.results = new System.Windows.Forms.TextBox();
            this.start = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.filesFound = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // directoryRoot
            // 
            this.directoryRoot.Location = new System.Drawing.Point(117, 6);
            this.directoryRoot.Name = "directoryRoot";
            this.directoryRoot.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.directoryRoot.Size = new System.Drawing.Size(789, 22);
            this.directoryRoot.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Directory Root";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Number of Files";
            // 
            // numberOfFiles
            // 
            this.numberOfFiles.Location = new System.Drawing.Point(117, 56);
            this.numberOfFiles.Name = "numberOfFiles";
            this.numberOfFiles.Size = new System.Drawing.Size(100, 22);
            this.numberOfFiles.TabIndex = 3;
            // 
            // results
            // 
            this.results.AcceptsReturn = true;
            this.results.Location = new System.Drawing.Point(12, 99);
            this.results.Multiline = true;
            this.results.Name = "results";
            this.results.ReadOnly = true;
            this.results.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.results.Size = new System.Drawing.Size(894, 231);
            this.results.TabIndex = 4;
            this.results.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            // 
            // start
            // 
            this.start.Location = new System.Drawing.Point(40, 346);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(143, 41);
            this.start.TabIndex = 5;
            this.start.Text = "Start";
            this.start.UseVisualStyleBackColor = true;
            this.start.Click += new System.EventHandler(this.Start_Click);
            // 
            // cancel
            // 
            this.cancel.Enabled = false;
            this.cancel.Location = new System.Drawing.Point(723, 346);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(143, 41);
            this.cancel.TabIndex = 6;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(445, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "Files Found";
            // 
            // filesFound
            // 
            this.filesFound.Location = new System.Drawing.Point(532, 57);
            this.filesFound.Name = "filesFound";
            this.filesFound.ReadOnly = true;
            this.filesFound.Size = new System.Drawing.Size(100, 22);
            this.filesFound.TabIndex = 8;
            // 
            // FileSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(918, 565);
            this.Controls.Add(this.filesFound);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.start);
            this.Controls.Add(this.results);
            this.Controls.Add(this.numberOfFiles);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.directoryRoot);
            this.Name = "FileSearchForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox directoryRoot;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox numberOfFiles;
        private System.Windows.Forms.TextBox results;
        private System.Windows.Forms.Button start;
        private System.Windows.Forms.Button cancel;
        private Label label3;
        private TextBox filesFound;
    }
}

