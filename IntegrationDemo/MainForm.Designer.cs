namespace IntegrationDemo
{
    partial class MainForm
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeViewTasks = new System.Windows.Forms.TreeView();
            this.panelRight = new System.Windows.Forms.Panel();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.btnRunTask = new System.Windows.Forms.Button();
            this.panelParameters = new System.Windows.Forms.Panel();
            this.lblTaskDescription = new System.Windows.Forms.Label();
            this.lblTaskTitle = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblSpring = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnShowJson = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnClearOutput = new System.Windows.Forms.ToolStripDropDownButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panelRight.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.treeViewTasks);
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.panelRight);
            this.splitContainer1.Size = new System.Drawing.Size(1024, 646);
            this.splitContainer1.SplitterDistance = 280;
            this.splitContainer1.TabIndex = 0;
            //
            // treeViewTasks
            //
            this.treeViewTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewTasks.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeViewTasks.HideSelection = false;
            this.treeViewTasks.Location = new System.Drawing.Point(0, 0);
            this.treeViewTasks.Name = "treeViewTasks";
            this.treeViewTasks.Size = new System.Drawing.Size(280, 646);
            this.treeViewTasks.TabIndex = 0;
            this.treeViewTasks.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewTasks_AfterSelect);
            //
            // panelRight
            //
            this.panelRight.Controls.Add(this.txtOutput);
            this.panelRight.Controls.Add(this.btnRunTask);
            this.panelRight.Controls.Add(this.panelParameters);
            this.panelRight.Controls.Add(this.lblTaskDescription);
            this.panelRight.Controls.Add(this.lblTaskTitle);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(0, 0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Padding = new System.Windows.Forms.Padding(10);
            this.panelRight.Size = new System.Drawing.Size(740, 646);
            this.panelRight.TabIndex = 0;
            //
            // txtOutput
            //
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtOutput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutput.ForeColor = System.Drawing.Color.LightGreen;
            this.txtOutput.Location = new System.Drawing.Point(13, 280);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(714, 353);
            this.txtOutput.TabIndex = 4;
            this.txtOutput.Text = "";
            this.txtOutput.WordWrap = false;
            //
            // btnRunTask
            //
            this.btnRunTask.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRunTask.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnRunTask.Enabled = false;
            this.btnRunTask.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRunTask.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunTask.ForeColor = System.Drawing.Color.White;
            this.btnRunTask.Location = new System.Drawing.Point(13, 230);
            this.btnRunTask.Name = "btnRunTask";
            this.btnRunTask.Size = new System.Drawing.Size(714, 40);
            this.btnRunTask.TabIndex = 3;
            this.btnRunTask.Text = "▶ Run Task";
            this.btnRunTask.UseVisualStyleBackColor = false;
            this.btnRunTask.Click += new System.EventHandler(this.btnRunTask_Click);
            //
            // panelParameters
            //
            this.panelParameters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelParameters.AutoScroll = true;
            this.panelParameters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelParameters.Location = new System.Drawing.Point(13, 110);
            this.panelParameters.Name = "panelParameters";
            this.panelParameters.Padding = new System.Windows.Forms.Padding(5);
            this.panelParameters.Size = new System.Drawing.Size(714, 110);
            this.panelParameters.TabIndex = 2;
            //
            // lblTaskDescription
            //
            this.lblTaskDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTaskDescription.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTaskDescription.Location = new System.Drawing.Point(13, 50);
            this.lblTaskDescription.Name = "lblTaskDescription";
            this.lblTaskDescription.Size = new System.Drawing.Size(714, 55);
            this.lblTaskDescription.TabIndex = 1;
            this.lblTaskDescription.Text = "Select a task from the list on the left to see its description and run it.";
            //
            // lblTaskTitle
            //
            this.lblTaskTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTaskTitle.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTaskTitle.Location = new System.Drawing.Point(13, 10);
            this.lblTaskTitle.Name = "lblTaskTitle";
            this.lblTaskTitle.Size = new System.Drawing.Size(714, 30);
            this.lblTaskTitle.TabIndex = 0;
            this.lblTaskTitle.Text = "HSMAdvisor.Integration Demo";
            //
            // statusStrip1
            //
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblSpring,
            this.btnShowJson,
            this.btnClearOutput});
            this.statusStrip1.Location = new System.Drawing.Point(0, 646);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1024, 22);
            this.statusStrip1.TabIndex = 1;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(140, 17);
            this.lblStatus.Text = "HSMAdvisor: checking...";
            //
            // lblSpring
            //
            this.lblSpring.Name = "lblSpring";
            this.lblSpring.Size = new System.Drawing.Size(700, 17);
            this.lblSpring.Spring = true;
            //
            // btnShowJson
            //
            this.btnShowJson.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnShowJson.Name = "btnShowJson";
            this.btnShowJson.ShowDropDownArrow = false;
            this.btnShowJson.Size = new System.Drawing.Size(105, 20);
            this.btnShowJson.Text = "☐ Show JSON";
            this.btnShowJson.ToolTipText = "Print the JSON payload of every request and response";
            this.btnShowJson.Click += new System.EventHandler(this.btnShowJson_Click);
            //
            // btnClearOutput
            //
            this.btnClearOutput.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnClearOutput.Name = "btnClearOutput";
            this.btnClearOutput.ShowDropDownArrow = false;
            this.btnClearOutput.Size = new System.Drawing.Size(80, 20);
            this.btnClearOutput.Text = "Clear output";
            this.btnClearOutput.Click += new System.EventHandler(this.btnClearOutput_Click);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 668);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HSMAdvisor.Integration Demo - Interactive Task Explorer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panelRight.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeViewTasks;
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.Label lblTaskTitle;
        private System.Windows.Forms.Label lblTaskDescription;
        private System.Windows.Forms.Panel panelParameters;
        private System.Windows.Forms.Button btnRunTask;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblSpring;
        private System.Windows.Forms.ToolStripDropDownButton btnShowJson;
        private System.Windows.Forms.ToolStripDropDownButton btnClearOutput;
    }
}
