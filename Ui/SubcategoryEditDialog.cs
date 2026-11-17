using System.Drawing;
using System.Windows.Forms;
using DailyAnimeWallpaper.Localization;

namespace DailyAnimeWallpaper.Ui;

/// <summary>Modal dialog for adding or editing a subcategory under an anime wallpaper API source.</summary>
internal sealed class SubcategoryEditDialog : Form
{
    readonly WallpaperSubcategory? Existing;
    readonly TextBox NameTextBox = new() { Dock = DockStyle.Fill };
    readonly TextBox EndpointTextBox = new() { Dock = DockStyle.Fill };
    readonly NumericUpDown WeightNumeric = new()
    {
        Minimum = 0,
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        Width = 200
    };
    readonly CheckBox NsfwCheckBox = new() { AutoSize = true };
    readonly CheckBox EnabledCheckBox = new() { AutoSize = true };
    bool UserTouchedEnabled;

    public WallpaperSubcategory? ResultSubcategory { get; private set; }

    public SubcategoryEditDialog(WallpaperSubcategory? existing = null)
    {
        Existing = existing;
        Font = new Font("Segoe UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 235);
        Text = existing is null ? Strings.DialogAddSubcategoryTitle : Strings.DialogEditSubcategoryTitle;

        BuildLayout();
        PopulateValues();
    }

    /// <summary>Checks that an endpoint specification contains one or more pipe-separated HTTP/HTTPS URLs.</summary>
    /// <param name="endpointSpec">Raw endpoint input.</param>
    /// <returns>True when every segment is a valid absolute HTTP/HTTPS URI.</returns>
    public static bool IsValidEndpointSpec(string? endpointSpec)
    {
        if (string.IsNullOrWhiteSpace(endpointSpec))
            return false;

        string[] parts = endpointSpec.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        foreach (string part in parts)
        {
            if (!Uri.TryCreate(part, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                return false;
            }
        }

        return true;
    }

    void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 5
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;
        AddRow(root, ref row, Strings.FieldSubcategoryName, NameTextBox);
        AddRow(root, ref row, Strings.FieldEndpointUrl, EndpointTextBox);

        var optionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        EnabledCheckBox.Text = Strings.FieldEnabled;
        EnabledCheckBox.Margin = new Padding(14, 4, 8, 3);
        NsfwCheckBox.Text = Strings.LabelNsfwTag;
        NsfwCheckBox.Margin = new Padding(8, 4, 3, 3);

        optionsFlow.Controls.Add(WeightNumeric);
        optionsFlow.Controls.Add(EnabledCheckBox);
        optionsFlow.Controls.Add(NsfwCheckBox);
        AddRow(root, ref row, Strings.LabelWeight, optionsFlow);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0)
        };

        var cancelBtn = new Button
        {
            Text = Strings.ButtonCancel,
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            Padding = new Padding(10, 3, 10, 3)
        };
        var okBtn = new Button
        {
            Text = Strings.ButtonSave,
            AutoSize = true,
            Padding = new Padding(10, 3, 10, 3)
        };
        okBtn.Click += OnConfirmClick;

        buttonsPanel.Controls.Add(cancelBtn);
        buttonsPanel.Controls.Add(okBtn);
        root.Controls.Add(buttonsPanel, 0, row);
        root.SetColumnSpan(buttonsPanel, 2);

        EnabledCheckBox.Click += (_, _) => UserTouchedEnabled = true;
        NsfwCheckBox.CheckedChanged += (_, _) =>
        {
            if (Existing is null && !UserTouchedEnabled)
                EnabledCheckBox.Checked = !NsfwCheckBox.Checked;
        };

        AcceptButton = okBtn;
        CancelButton = cancelBtn;
        Controls.Add(root);
    }

    static void AddRow(TableLayoutPanel table, ref int row, string labelText, Control editor)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 6, 10, 6)
        };
        editor.Margin = new Padding(3, 4, 3, 4);
        table.Controls.Add(label, 0, row);
        table.Controls.Add(editor, 1, row);
        row++;
    }

    void PopulateValues()
    {
        if (Existing is null)
        {
            WeightNumeric.Value = DefaultWeight;
            IsNsfwDefaultSetup();
        }
        else
        {
            NameTextBox.Text = Existing.Name;
            EndpointTextBox.Text = Existing.Endpoint;
            WeightNumeric.Value = Math.Clamp(Existing.Weight, 0, int.MaxValue);
            UserTouchedEnabled = true;
            NsfwCheckBox.Checked = Existing.IsNsfw;
            EnabledCheckBox.Checked = Existing.Enabled;
        }
    }

    void IsNsfwDefaultSetup()
    {
        NsfwCheckBox.Checked = false;
        EnabledCheckBox.Checked = true;
    }

    void OnConfirmClick(object? sender, EventArgs e)
    {
        string name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, Strings.ValidationNameRequired, Strings.SettingsTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            NameTextBox.Focus();
            return;
        }

        string endpoint = EndpointTextBox.Text.Trim();
        if (!IsValidEndpointSpec(endpoint))
        {
            MessageBox.Show(this, Strings.ValidationEndpointRequired, Strings.SettingsTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            EndpointTextBox.Focus();
            return;
        }

        if (Existing is null)
        {
            ResultSubcategory = new WallpaperSubcategory
            {
                Id = $"sub-{Guid.NewGuid():N}",
                Name = name,
                Endpoint = endpoint,
                IsNsfw = NsfwCheckBox.Checked,
                Enabled = EnabledCheckBox.Checked,
                Weight = (int)WeightNumeric.Value
            };
        }
        else
        {
            WallpaperSubcategory updated = Existing.Clone();
            updated.Name = name;
            updated.Endpoint = endpoint;
            updated.IsNsfw = NsfwCheckBox.Checked;
            updated.Enabled = EnabledCheckBox.Checked;
            updated.Weight = (int)WeightNumeric.Value;
            ResultSubcategory = updated;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
