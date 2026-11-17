using System.Drawing;
using System.Windows.Forms;
using DailyAnimeWallpaper.Localization;

namespace DailyAnimeWallpaper.Ui;

/// <summary>Modal dialog for creating a custom anime API source or editing an existing source.</summary>
internal sealed class ProviderEditDialog : Form
{
    readonly record struct ModeOption(ProviderResponseMode Mode, string Label)
    {
        public override string ToString() => Label;
    }

    readonly WallpaperProvider? Existing;
    readonly TextBox NameTextBox = new() { Dock = DockStyle.Fill };
    readonly ComboBox ModeComboBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TextBox JsonPathTextBox = new() { Dock = DockStyle.Fill };
    readonly TextBox FallbackJsonPathTextBox = new() { Dock = DockStyle.Fill };
    readonly TextBox DefaultEndpointTextBox = new() { Dock = DockStyle.Fill };
    readonly NumericUpDown WeightNumeric = new()
    {
        Minimum = 0,
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        Width = 220
    };
    readonly CheckBox EnabledCheckBox = new() { AutoSize = true };

    public WallpaperProvider? ResultProvider { get; private set; }

    public ProviderEditDialog(WallpaperProvider? existing = null)
    {
        Existing = existing;
        Font = new Font("Segoe UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(580, existing is null ? 350 : 305);
        Text = existing is null ? Strings.DialogAddProviderTitle : Strings.DialogEditProviderTitle;

        BuildLayout();
        PopulateInitialValues();
    }

    void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = Existing is null ? 8 : 7
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;
        AddRow(root, ref row, Strings.FieldProviderName, NameTextBox);
        AddRow(root, ref row, Strings.FieldResponseMode, ModeComboBox);
        AddRow(root, ref row, Strings.FieldJsonPath, JsonPathTextBox);

        var hintLabel = new Label
        {
            Text = Strings.HintJsonPath,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 0, 3, 8)
        };
        root.Controls.Add(new Label { AutoSize = true }, 0, row);
        root.Controls.Add(hintLabel, 1, row++);

        AddRow(root, ref row, Strings.FieldFallbackJsonPath, FallbackJsonPathTextBox);

        if (Existing is null)
            AddRow(root, ref row, Strings.FieldDefaultEndpoint, DefaultEndpointTextBox);

        var optionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        EnabledCheckBox.Text = Strings.FieldEnabled;
        EnabledCheckBox.Margin = new Padding(14, 4, 3, 3);
        optionsFlow.Controls.Add(WeightNumeric);
        optionsFlow.Controls.Add(EnabledCheckBox);
        AddRow(root, ref row, Strings.LabelWeight, optionsFlow);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0)
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

        AcceptButton = okBtn;
        CancelButton = cancelBtn;
        ModeComboBox.SelectedIndexChanged += (_, _) => UpdateJsonFieldsState();
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

    void PopulateInitialValues()
    {
        ModeComboBox.Items.Add(new ModeOption(ProviderResponseMode.JsonElementPath, Strings.ModeJsonPath));
        ModeComboBox.Items.Add(new ModeOption(ProviderResponseMode.DirectUrlOrImage, Strings.ModeDirectUrlOrImage));

        if (Existing is not null &&
            Existing.ResponseMode is not ProviderResponseMode.JsonElementPath and not ProviderResponseMode.DirectUrlOrImage)
        {
            ModeComboBox.Items.Add(new ModeOption(Existing.ResponseMode, $"{Strings.ModeBuiltInSpecial} ({Existing.ResponseMode})"));
        }

        if (Existing is null)
        {
            ModeComboBox.SelectedIndex = 0;
            JsonPathTextBox.Text = "url";
            WeightNumeric.Value = DefaultWeight;
            EnabledCheckBox.Checked = true;
        }
        else
        {
            NameTextBox.Text = Existing.Name;
            JsonPathTextBox.Text = Existing.JsonPath;
            FallbackJsonPathTextBox.Text = Existing.FallbackJsonPath;
            WeightNumeric.Value = Math.Clamp(Existing.Weight, 0, int.MaxValue);
            EnabledCheckBox.Checked = Existing.Enabled;

            int selectedIdx = 0;
            for (int i = 0; i < ModeComboBox.Items.Count; i++)
            {
                if (ModeComboBox.Items[i] is ModeOption opt && opt.Mode == Existing.ResponseMode)
                {
                    selectedIdx = i;
                    break;
                }
            }
            ModeComboBox.SelectedIndex = selectedIdx;
        }

        UpdateJsonFieldsState();
    }

    void UpdateJsonFieldsState()
    {
        bool isJson = ModeComboBox.SelectedItem is ModeOption { Mode: ProviderResponseMode.JsonElementPath };
        JsonPathTextBox.Enabled = isJson;
        FallbackJsonPathTextBox.Enabled = isJson;
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

        ProviderResponseMode selectedMode = ModeComboBox.SelectedItem is ModeOption opt
            ? opt.Mode
            : ProviderResponseMode.JsonElementPath;

        string jsonPath = JsonPathTextBox.Text.Trim();
        if (selectedMode == ProviderResponseMode.JsonElementPath && string.IsNullOrWhiteSpace(jsonPath))
        {
            MessageBox.Show(this, Strings.ValidationJsonPathRequired, Strings.SettingsTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            JsonPathTextBox.Focus();
            return;
        }

        if (Existing is null)
        {
            string endpoint = DefaultEndpointTextBox.Text.Trim();
            if (!SubcategoryEditDialog.IsValidEndpointSpec(endpoint))
            {
                MessageBox.Show(this, Strings.ValidationEndpointRequired, Strings.SettingsTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DefaultEndpointTextBox.Focus();
                return;
            }

            ResultProvider = new WallpaperProvider
            {
                Id = $"custom-{Guid.NewGuid():N}",
                Name = name,
                IsBuiltIn = false,
                Enabled = EnabledCheckBox.Checked,
                Weight = (int)WeightNumeric.Value,
                ResponseMode = selectedMode,
                JsonPath = selectedMode == ProviderResponseMode.JsonElementPath ? jsonPath : string.Empty,
                FallbackJsonPath = selectedMode == ProviderResponseMode.JsonElementPath ? FallbackJsonPathTextBox.Text.Trim() : string.Empty,
                Subcategories =
                [
                    new WallpaperSubcategory
                    {
                        Id = "general",
                        Name = "General",
                        Endpoint = endpoint,
                        IsNsfw = false,
                        Enabled = true,
                        Weight = DefaultWeight
                    }
                ]
            };
        }
        else
        {
            WallpaperProvider updated = Existing.Clone();
            updated.Name = name;
            updated.Enabled = EnabledCheckBox.Checked;
            updated.Weight = (int)WeightNumeric.Value;
            updated.ResponseMode = selectedMode;
            updated.JsonPath = selectedMode == ProviderResponseMode.JsonElementPath ? jsonPath : string.Empty;
            updated.FallbackJsonPath = selectedMode == ProviderResponseMode.JsonElementPath ? FallbackJsonPathTextBox.Text.Trim() : string.Empty;
            ResultProvider = updated;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
