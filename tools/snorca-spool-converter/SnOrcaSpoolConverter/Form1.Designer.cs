#nullable disable
namespace SnOrcaSpoolConverter;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        labelInput = new Label();
        textInput = new TextBox();
        buttonBrowseInput = new Button();
        labelOutput = new Label();
        textOutput = new TextBox();
        buttonBrowseOutput = new Button();
        checkOverrideVendor = new CheckBox();
        textVendorOverride = new TextBox();
        checkOverwrite = new CheckBox();
        checkDedupById = new CheckBox();
        buttonConvert = new Button();
        labelStatus = new Label();
        SuspendLayout();
        // 
        // labelInput
        // 
        labelInput.AutoSize = true;
        labelInput.Location = new Point(12, 15);
        labelInput.Name = "labelInput";
        labelInput.Size = new Size(106, 15);
        labelInput.TabIndex = 0;
        labelInput.Text = "Input (CSV / JSON)";
        // 
        // textInput
        // 
        textInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        textInput.Location = new Point(12, 33);
        textInput.Name = "textInput";
        textInput.Size = new Size(676, 23);
        textInput.TabIndex = 1;
        // 
        // buttonBrowseInput
        // 
        buttonBrowseInput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonBrowseInput.Location = new Point(694, 32);
        buttonBrowseInput.Name = "buttonBrowseInput";
        buttonBrowseInput.Size = new Size(94, 25);
        buttonBrowseInput.TabIndex = 2;
        buttonBrowseInput.Text = "Browse...";
        buttonBrowseInput.UseVisualStyleBackColor = true;
        buttonBrowseInput.Click += buttonBrowseInput_Click;
        // 
        // labelOutput
        // 
        labelOutput.AutoSize = true;
        labelOutput.Location = new Point(12, 70);
        labelOutput.Name = "labelOutput";
        labelOutput.Size = new Size(79, 15);
        labelOutput.TabIndex = 3;
        labelOutput.Text = "Output folder";
        // 
        // textOutput
        // 
        textOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        textOutput.Location = new Point(12, 88);
        textOutput.Name = "textOutput";
        textOutput.Size = new Size(676, 23);
        textOutput.TabIndex = 4;
        // 
        // buttonBrowseOutput
        // 
        buttonBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonBrowseOutput.Location = new Point(694, 87);
        buttonBrowseOutput.Name = "buttonBrowseOutput";
        buttonBrowseOutput.Size = new Size(94, 25);
        buttonBrowseOutput.TabIndex = 5;
        buttonBrowseOutput.Text = "Browse...";
        buttonBrowseOutput.UseVisualStyleBackColor = true;
        buttonBrowseOutput.Click += buttonBrowseOutput_Click;
        // 
        // checkOverrideVendor
        // 
        checkOverrideVendor.AutoSize = true;
        checkOverrideVendor.Location = new Point(12, 130);
        checkOverrideVendor.Name = "checkOverrideVendor";
        checkOverrideVendor.Size = new Size(157, 19);
        checkOverrideVendor.TabIndex = 6;
        checkOverrideVendor.Text = "Override vendor/brand:";
        checkOverrideVendor.UseVisualStyleBackColor = true;
        checkOverrideVendor.CheckedChanged += checkOverrideVendor_CheckedChanged;
        // 
        // textVendorOverride
        // 
        textVendorOverride.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        textVendorOverride.Enabled = false;
        textVendorOverride.Location = new Point(175, 127);
        textVendorOverride.Name = "textVendorOverride";
        textVendorOverride.Size = new Size(613, 23);
        textVendorOverride.TabIndex = 7;
        textVendorOverride.Text = "PatLabs";
        // 
        // checkOverwrite
        // 
        checkOverwrite.AutoSize = true;
        checkOverwrite.Checked = true;
        checkOverwrite.CheckState = CheckState.Checked;
        checkOverwrite.Location = new Point(12, 165);
        checkOverwrite.Name = "checkOverwrite";
        checkOverwrite.Size = new Size(158, 19);
        checkOverwrite.TabIndex = 8;
        checkOverwrite.Text = "Overwrite existing files";
        checkOverwrite.UseVisualStyleBackColor = true;
        // 
        // checkDedupById
        // 
        checkDedupById.AutoSize = true;
        checkDedupById.Checked = true;
        checkDedupById.CheckState = CheckState.Checked;
        checkDedupById.Location = new Point(12, 190);
        checkDedupById.Name = "checkDedupById";
        checkDedupById.Size = new Size(257, 19);
        checkDedupById.TabIndex = 9;
        checkDedupById.Text = "Deduplicate by id (keep last occurrence)";
        checkDedupById.UseVisualStyleBackColor = true;
        // 
        // buttonConvert
        // 
        buttonConvert.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonConvert.Location = new Point(694, 220);
        buttonConvert.Name = "buttonConvert";
        buttonConvert.Size = new Size(94, 32);
        buttonConvert.TabIndex = 10;
        buttonConvert.Text = "Convert";
        buttonConvert.UseVisualStyleBackColor = true;
        buttonConvert.Click += buttonConvert_Click;
        // 
        // labelStatus
        // 
        labelStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        labelStatus.BorderStyle = BorderStyle.FixedSingle;
        labelStatus.Location = new Point(12, 270);
        labelStatus.Name = "labelStatus";
        labelStatus.Padding = new Padding(8);
        labelStatus.Size = new Size(776, 168);
        labelStatus.TabIndex = 11;
        labelStatus.Text = "Select an input file and click Convert.";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(labelStatus);
        Controls.Add(buttonConvert);
        Controls.Add(checkDedupById);
        Controls.Add(checkOverwrite);
        Controls.Add(textVendorOverride);
        Controls.Add(checkOverrideVendor);
        Controls.Add(buttonBrowseOutput);
        Controls.Add(textOutput);
        Controls.Add(labelOutput);
        Controls.Add(buttonBrowseInput);
        Controls.Add(textInput);
        Controls.Add(labelInput);
        MinimumSize = new Size(820, 489);
        Name = "Form1";
        Text = "snOrca spool → filament profile converter";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label labelInput = null!;
    private TextBox textInput = null!;
    private Button buttonBrowseInput = null!;
    private Label labelOutput = null!;
    private TextBox textOutput = null!;
    private Button buttonBrowseOutput = null!;
    private CheckBox checkOverrideVendor = null!;
    private TextBox textVendorOverride = null!;
    private CheckBox checkOverwrite = null!;
    private CheckBox checkDedupById = null!;
    private Button buttonConvert = null!;
    private Label labelStatus = null!;
}
