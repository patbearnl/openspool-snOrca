using System.Text;

namespace SnOrcaSpoolConverter;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var defaultOut = Path.Combine(appData, "Snapmaker_Orca", "user", "default", "filament");
        textOutput.Text = defaultOut;
    }

    private void buttonBrowseInput_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog();
        dlg.Title = "Select 3D Filament Profiles export (CSV/JSON)";
        dlg.Filter = "CSV or JSON (*.csv;*.json)|*.csv;*.json|All files (*.*)|*.*";
        dlg.CheckFileExists = true;
        dlg.Multiselect = false;
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        textInput.Text = dlg.FileName;
    }

    private void buttonBrowseOutput_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        dlg.Description = "Select output folder (usually %APPDATA%\\Snapmaker_Orca\\user\\default\\filament)";
        dlg.UseDescriptionForTitle = true;
        dlg.ShowNewFolderButton = true;
        if (Directory.Exists(textOutput.Text)) dlg.SelectedPath = textOutput.Text;
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        textOutput.Text = dlg.SelectedPath;
    }

    private void checkOverrideVendor_CheckedChanged(object? sender, EventArgs e)
    {
        textVendorOverride.Enabled = checkOverrideVendor.Checked;
    }

    private void buttonConvert_Click(object? sender, EventArgs e)
    {
        var inputPath = textInput.Text.Trim();
        var outputDir = textOutput.Text.Trim();

        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            MessageBox.Show(this, "Select a valid input file first.", "Missing input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputDir))
        {
            MessageBox.Show(this, "Select a valid output folder first.", "Missing output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(outputDir);

        var overrideVendor = checkOverrideVendor.Checked ? textVendorOverride.Text.Trim() : "";
        if (checkOverrideVendor.Checked && string.IsNullOrWhiteSpace(overrideVendor))
        {
            MessageBox.Show(this, "Vendor override is enabled but empty.", "Invalid vendor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        labelStatus.Text = "Reading input...";
        Refresh();

        List<SpoolRecord> records;
        try
        {
            records = SpoolImporters.ReadFile(inputPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (checkDedupById.Checked)
        {
            records = records.DeduplicateByIdKeepLast();
        }

        if (records.Count == 0)
        {
            labelStatus.Text = "No spools found in input.";
            return;
        }

        var overwrite = checkOverwrite.Checked;
        var results = new ConversionSummary();

        foreach (var record in records)
        {
            try
            {
                var profile = SnOrcaProfileFactory.FromSpool(record, overrideVendor);
                var fileName = FileNameUtils.SanitizeFileName(profile.Name) + ".json";
                var filePath = Path.Combine(outputDir, fileName);

                if (!overwrite && File.Exists(filePath))
                {
                    results.Skipped++;
                    continue;
                }

                File.WriteAllText(filePath, profile.ToJson(indented: true), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                results.Written++;
            }
            catch (Exception ex)
            {
                results.Errors.Add($"{record.Id}: {ex.Message}");
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Converted {records.Count} spool(s)");
        sb.AppendLine($"Written: {results.Written}");
        sb.AppendLine($"Skipped: {results.Skipped}");
        sb.AppendLine($"Errors:  {results.Errors.Count}");
        sb.AppendLine();
        sb.AppendLine($"Output: {outputDir}");

        if (results.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Errors:");
            foreach (var err in results.Errors.Take(50))
            {
                sb.AppendLine($"- {err}");
            }
            if (results.Errors.Count > 50)
            {
                sb.AppendLine($"(and {results.Errors.Count - 50} more)");
            }
        }

        labelStatus.Text = sb.ToString();
    }
}
