using System.Drawing;
using System.Windows.Forms;
using DailyAnimeWallpaper.Localization;

namespace DailyAnimeWallpaper.Ui;

/// <summary>Graphical configuration window for managing anime wallpaper APIs, subcategories, weights, and filters.</summary>
internal sealed class SettingsForm : Form
{
    const bool IsLogEnabled = true;

    readonly record struct LanguageItem(string Code, string Display)
    {
        public override string ToString() => Display;
    }

    readonly record struct FilterItem(NsfwFilterMode Mode, string Display)
    {
        public override string ToString() => Display;
    }

    List<WallpaperProvider> WorkingProviders = [];
    HashSet<string> WorkingRemovedBuiltInIds = new(StringComparer.OrdinalIgnoreCase);
    bool SuppressUiEvents;
    bool BusyRunning;
    CancellationTokenSource? ActiveOperationCts;

    // General controls.
    readonly GroupBox GeneralGroup = new() { Dock = DockStyle.Top, Height = 112, Padding = new Padding(10, 6, 10, 8) };
    readonly Label LanguageLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly ComboBox LanguageComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175 };
    readonly Label FilterLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly ComboBox FilterComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    readonly CheckBox LoggingCheckBox = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly Label DirectoryLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly TextBox DirectoryTextBox = new() { Dock = DockStyle.Fill };
    readonly Button BrowseDirectoryButton = new() { AutoSize = true };
    readonly Button OpenFolderButton = new() { AutoSize = true };

    // Master-detail split controls.
    readonly SplitContainer MainSplit = new() { Dock = DockStyle.Fill, SplitterDistance = 510 };
    readonly GroupBox ProvidersGroup = new() { Dock = DockStyle.Fill, Padding = new Padding(8) };
    readonly Label SearchLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly TextBox SearchTextBox = new() { Width = 200 };
    readonly Label ProvidersSummaryLabel = new() { AutoSize = true, Anchor = AnchorStyles.Right };
    readonly ListView ProvidersListView = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false,
        MultiSelect = false
    };
    readonly ColumnHeader ColApiName = new() { Width = 195 };
    readonly ColumnHeader ColApiMode = new() { Width = 115 };
    readonly ColumnHeader ColApiWeight = new() { Width = 95, TextAlign = HorizontalAlignment.Right };
    readonly ColumnHeader ColApiActiveSubs = new() { Width = 85, TextAlign = HorizontalAlignment.Right };
    readonly Label ProviderWeightLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly NumericUpDown ProviderWeightNumeric = new()
    {
        Minimum = 0,
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        Width = 175,
        Enabled = false
    };
    readonly Button AddApiButton = new() { AutoSize = true };
    readonly Button EditApiButton = new() { AutoSize = true };
    readonly Button RemoveApiButton = new() { AutoSize = true };
    readonly Button SelectAllApisButton = new() { AutoSize = true };
    readonly Button SelectNoApisButton = new() { AutoSize = true };
    readonly Button EqualizeApiWeightsButton = new() { AutoSize = true };

    // Subcategories controls.
    readonly GroupBox SubcategoriesGroup = new() { Dock = DockStyle.Fill, Padding = new Padding(8) };
    readonly Label SubcategoriesSummaryLabel = new() { AutoSize = true, Anchor = AnchorStyles.Right };
    readonly ListView SubcategoriesListView = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false,
        MultiSelect = false
    };
    readonly ColumnHeader ColSubName = new() { Width = 155 };
    readonly ColumnHeader ColSubRating = new() { Width = 65 };
    readonly ColumnHeader ColSubWeight = new() { Width = 95, TextAlign = HorizontalAlignment.Right };
    readonly ColumnHeader ColSubEndpoint = new() { Width = 260 };
    readonly Label SubWeightLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly NumericUpDown SubWeightNumeric = new()
    {
        Minimum = 0,
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        Width = 175,
        Enabled = false
    };
    readonly CheckBox SubNsfwCheckBox = new() { AutoSize = true, Anchor = AnchorStyles.Left, Enabled = false };
    readonly Button AddSubButton = new() { AutoSize = true };
    readonly Button EditSubButton = new() { AutoSize = true };
    readonly Button RemoveSubButton = new() { AutoSize = true };
    readonly Button SelectSfwButton = new() { AutoSize = true };
    readonly Button UncheckNsfwButton = new() { AutoSize = true };
    readonly Button EqualizeSubWeightsButton = new() { AutoSize = true };

    // Bottom status and action buttons.
    readonly Label StatusLabel = new()
    {
        AutoEllipsis = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };
    readonly Button RestoreDefaultsButton = new() { AutoSize = true };
    readonly Button TestSelectedButton = new() { AutoSize = true };
    readonly Button ApplyNowButton = new() { AutoSize = true };
    readonly Button SaveButton = new() { AutoSize = true };
    readonly Button CloseButton = new() { AutoSize = true };

    public SettingsForm()
    {
        Font = new Font("Segoe UI", 9.25F);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        MinimumSize = new Size(980, 620);
        Size = new Size(1180, 740);
        StartPosition = FormStartPosition.CenterScreen;
        TryAssignWindowIcon();

        BuildUiLayout();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LocManager.LanguageChanged += OnLanguageChanged;
        LoadStateIntoForm();
        ApplyLocalizedText();
        RefreshProvidersList(preserveSelectionId: WorkingProviders.FirstOrDefault()?.Id);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        LocManager.LanguageChanged -= OnLanguageChanged;
        ActiveOperationCts?.Cancel();
        ActiveOperationCts?.Dispose();
        base.OnFormClosed(e);
    }

    void TryAssignWindowIcon()
    {
        try
        {
            if (File.Exists(ThisExePath))
                Icon = Icon.ExtractAssociatedIcon(ThisExePath);
        }
        catch
        {
            // Keep default WinForms icon if extraction fails.
        }
    }

    void BuildUiLayout()
    {
        var outerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 1,
            RowCount = 3
        };
        outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        outerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        BuildGeneralGroup();
        BuildProvidersPane();
        BuildSubcategoriesPane();
        MainSplit.Panel1.Controls.Add(ProvidersGroup);
        MainSplit.Panel2.Controls.Add(SubcategoriesGroup);

        var bottomBar = BuildBottomBar();

        outerLayout.Controls.Add(GeneralGroup, 0, 0);
        outerLayout.Controls.Add(MainSplit, 0, 1);
        outerLayout.Controls.Add(bottomBar, 0, 2);
        Controls.Add(outerLayout);
    }

    void BuildGeneralGroup()
    {
        GeneralGroup.AutoSize = true;
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 6,
            RowCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        LanguageLabel.Margin = new Padding(3, 6, 6, 6);
        LanguageComboBox.Margin = new Padding(3, 3, 16, 3);
        FilterLabel.Margin = new Padding(3, 6, 6, 6);
        FilterComboBox.Margin = new Padding(3, 3, 16, 3);
        LoggingCheckBox.Margin = new Padding(6, 5, 3, 3);

        var topOptionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false
        };
        topOptionsFlow.Controls.Add(FilterComboBox);
        topOptionsFlow.Controls.Add(LoggingCheckBox);

        grid.Controls.Add(LanguageLabel, 0, 0);
        grid.Controls.Add(LanguageComboBox, 1, 0);
        grid.Controls.Add(FilterLabel, 2, 0);
        grid.Controls.Add(topOptionsFlow, 3, 0);
        grid.SetColumnSpan(topOptionsFlow, 3);

        DirectoryLabel.Margin = new Padding(3, 7, 6, 4);
        DirectoryTextBox.Margin = new Padding(3, 4, 6, 4);
        BrowseDirectoryButton.Margin = new Padding(3, 3, 4, 3);
        OpenFolderButton.Margin = new Padding(3, 3, 3, 3);

        grid.Controls.Add(DirectoryLabel, 0, 1);
        grid.Controls.Add(DirectoryTextBox, 1, 1);
        grid.SetColumnSpan(DirectoryTextBox, 3);
        grid.Controls.Add(BrowseDirectoryButton, 4, 1);
        grid.Controls.Add(OpenFolderButton, 5, 1);

        GeneralGroup.Controls.Add(grid);
    }

    void BuildProvidersPane()
    {
        ProvidersListView.Columns.AddRange([ColApiName, ColApiMode, ColApiWeight, ColApiActiveSubs]);

        var paneGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        paneGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paneGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        paneGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paneGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var searchRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3
        };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        SearchTextBox.Dock = DockStyle.Fill;
        SearchLabel.Margin = new Padding(2, 6, 6, 4);
        ProvidersSummaryLabel.Margin = new Padding(8, 6, 2, 4);
        searchRow.Controls.Add(SearchLabel, 0, 0);
        searchRow.Controls.Add(SearchTextBox, 1, 0);
        searchRow.Controls.Add(ProvidersSummaryLabel, 2, 0);

        var weightRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 2)
        };
        ProviderWeightLabel.Margin = new Padding(2, 6, 6, 2);
        weightRow.Controls.Add(ProviderWeightLabel);
        weightRow.Controls.Add(ProviderWeightNumeric);

        var buttonsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 0)
        };
        buttonsRow.Controls.AddRange(
        [
            AddApiButton,
            EditApiButton,
            RemoveApiButton,
            SelectAllApisButton,
            SelectNoApisButton,
            EqualizeApiWeightsButton
        ]);

        paneGrid.Controls.Add(searchRow, 0, 0);
        paneGrid.Controls.Add(ProvidersListView, 0, 1);
        paneGrid.Controls.Add(weightRow, 0, 2);
        paneGrid.Controls.Add(buttonsRow, 0, 3);
        ProvidersGroup.Controls.Add(paneGrid);
    }

    void BuildSubcategoriesPane()
    {
        SubcategoriesListView.Columns.AddRange([ColSubName, ColSubRating, ColSubWeight, ColSubEndpoint]);

        var paneGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        paneGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paneGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        paneGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        paneGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var topInfoRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1
        };
        SubcategoriesSummaryLabel.Margin = new Padding(2, 6, 2, 4);
        topInfoRow.Controls.Add(SubcategoriesSummaryLabel, 0, 0);

        var weightRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 2)
        };
        SubWeightLabel.Margin = new Padding(2, 6, 6, 2);
        SubNsfwCheckBox.Margin = new Padding(14, 5, 4, 2);
        weightRow.Controls.Add(SubWeightLabel);
        weightRow.Controls.Add(SubWeightNumeric);
        weightRow.Controls.Add(SubNsfwCheckBox);

        var buttonsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 0)
        };
        buttonsRow.Controls.AddRange(
        [
            AddSubButton,
            EditSubButton,
            RemoveSubButton,
            SelectSfwButton,
            UncheckNsfwButton,
            EqualizeSubWeightsButton
        ]);

        paneGrid.Controls.Add(topInfoRow, 0, 0);
        paneGrid.Controls.Add(SubcategoriesListView, 0, 1);
        paneGrid.Controls.Add(weightRow, 0, 2);
        paneGrid.Controls.Add(buttonsRow, 0, 3);
        SubcategoriesGroup.Controls.Add(paneGrid);
    }

    TableLayoutPanel BuildBottomBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(2, 8, 2, 0)
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var actionsFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        SaveButton.Font = new Font(Font, FontStyle.Bold);
        actionsFlow.Controls.AddRange(
        [
            RestoreDefaultsButton,
            TestSelectedButton,
            ApplyNowButton,
            SaveButton,
            CloseButton
        ]);

        bar.Controls.Add(StatusLabel, 0, 0);
        bar.Controls.Add(actionsFlow, 1, 0);
        return bar;
    }

    void WireEvents()
    {
        LanguageComboBox.SelectedIndexChanged += OnLanguageSelectionChanged;
        FilterComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (!SuppressUiEvents)
                RefreshProviderRowCounts();
        };

        BrowseDirectoryButton.Click += OnBrowseDirectoryClick;
        OpenFolderButton.Click += OnOpenFolderClick;

        SearchTextBox.TextChanged += (_, _) =>
        {
            if (!SuppressUiEvents)
                RefreshProvidersList(GetSelectedProvider()?.Id);
        };

        ProvidersListView.ItemChecked += OnProviderItemChecked;
        ProvidersListView.SelectedIndexChanged += OnProviderSelectionChanged;
        ProvidersListView.DoubleClick += (_, _) => EditSelectedProvider();
        ProviderWeightNumeric.ValueChanged += OnProviderWeightChanged;

        AddApiButton.Click += (_, _) => AddNewProvider();
        EditApiButton.Click += (_, _) => EditSelectedProvider();
        RemoveApiButton.Click += (_, _) => RemoveSelectedProvider();
        SelectAllApisButton.Click += (_, _) => SetAllVisibleProvidersChecked(true);
        SelectNoApisButton.Click += (_, _) => SetAllVisibleProvidersChecked(false);
        EqualizeApiWeightsButton.Click += (_, _) => EqualizeAllProviderWeights();

        SubcategoriesListView.ItemChecked += OnSubcategoryItemChecked;
        SubcategoriesListView.SelectedIndexChanged += OnSubcategorySelectionChanged;
        SubcategoriesListView.DoubleClick += (_, _) => EditSelectedSubcategory();
        SubWeightNumeric.ValueChanged += OnSubcategoryWeightChanged;
        SubNsfwCheckBox.CheckedChanged += OnSubcategoryNsfwChanged;

        AddSubButton.Click += (_, _) => AddNewSubcategory();
        EditSubButton.Click += (_, _) => EditSelectedSubcategory();
        RemoveSubButton.Click += (_, _) => RemoveSelectedSubcategory();
        SelectSfwButton.Click += (_, _) => SetSubcategoriesByRating(checkSfw: true);
        UncheckNsfwButton.Click += (_, _) => SetSubcategoriesByRating(checkSfw: false);
        EqualizeSubWeightsButton.Click += (_, _) => EqualizeSelectedProviderSubWeights();

        RestoreDefaultsButton.Click += OnRestoreDefaultsClick;
        TestSelectedButton.Click += OnTestSelectedClick;
        ApplyNowButton.Click += OnApplyNowClick;
        SaveButton.Click += OnSaveClick;
        CloseButton.Click += (_, _) => Close();
    }

    void LoadStateIntoForm()
    {
        SuppressUiEvents = true;
        try
        {
            WorkingProviders = SourceCatalogManager.GetProvidersSnapshot();
            WorkingRemovedBuiltInIds = SourceCatalogManager.GetRemovedBuiltInIdsSnapshot();
            DirectoryTextBox.Text = Settings.ImageSaveDirectory.Value;
            LoggingCheckBox.Checked = Settings.EnableLogging.Value;
            PopulateLanguageDropdown(Settings.Language.Value);
            PopulateFilterDropdown(Settings.NsfwFilter.Value);
        }
        finally
        {
            SuppressUiEvents = false;
        }
    }

    void PopulateLanguageDropdown(string selectedLanguageCode)
    {
        LanguageComboBox.Items.Clear();
        LanguageComboBox.Items.Add(new LanguageItem(LocManager.SystemLanguage, Strings.LanguageSystem));
        foreach ((string code, string name) in LocManager.Available)
            LanguageComboBox.Items.Add(new LanguageItem(code, name));

        int index = 0;
        for (int i = 0; i < LanguageComboBox.Items.Count; i++)
        {
            if (LanguageComboBox.Items[i] is LanguageItem item &&
                string.Equals(item.Code, selectedLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }
        LanguageComboBox.SelectedIndex = index;
    }

    void PopulateFilterDropdown(NsfwFilterMode selectedMode)
    {
        FilterComboBox.Items.Clear();
        FilterComboBox.Items.Add(new FilterItem(NsfwFilterMode.UseCategorySelection, Strings.FilterUseSelection));
        FilterComboBox.Items.Add(new FilterItem(NsfwFilterMode.SfwOnly, Strings.FilterSfwOnly));
        FilterComboBox.Items.Add(new FilterItem(NsfwFilterMode.NsfwOnly, Strings.FilterNsfwOnly));

        int index = 0;
        for (int i = 0; i < FilterComboBox.Items.Count; i++)
        {
            if (FilterComboBox.Items[i] is FilterItem item && item.Mode == selectedMode)
            {
                index = i;
                break;
            }
        }
        FilterComboBox.SelectedIndex = index;
    }

    NsfwFilterMode GetSelectedFilterMode() =>
        FilterComboBox.SelectedItem is FilterItem item ? item.Mode : NsfwFilterMode.UseCategorySelection;

    string GetSelectedLanguageCode() =>
        LanguageComboBox.SelectedItem is LanguageItem item ? item.Code : LocManager.SystemLanguage;

    void OnLanguageChanged()
    {
        if (InvokeRequired)
        {
            BeginInvoke(OnLanguageChanged);
            return;
        }

        ApplyLocalizedText();
    }

    void ApplyLocalizedText()
    {
        SuppressUiEvents = true;
        try
        {
            string currentLangSelection = GetSelectedLanguageCode();
            NsfwFilterMode currentFilter = GetSelectedFilterMode();

            Text = Strings.SettingsTitle;
            GeneralGroup.Text = Strings.GroupGeneralSettings;
            LanguageLabel.Text = Strings.SettingLanguage + ":";
            FilterLabel.Text = Strings.SettingNsfwFilter + ":";
            LoggingCheckBox.Text = Strings.SettingEnableLogging;
            DirectoryLabel.Text = Strings.SettingImageSaveDirectory + ":";
            BrowseDirectoryButton.Text = Strings.ButtonBrowse;
            OpenFolderButton.Text = Strings.ButtonOpenFolder;

            ProvidersGroup.Text = Strings.GroupProviders;
            SearchLabel.Text = Strings.LabelSearchApi;
            ProviderWeightLabel.Text = Strings.LabelWeight;
            ColApiName.Text = Strings.ColumnApiName;
            ColApiMode.Text = Strings.ColumnMode;
            ColApiWeight.Text = Strings.ColumnWeight;
            ColApiActiveSubs.Text = Strings.ColumnActiveSubcategories;

            AddApiButton.Text = Strings.ButtonAddApi;
            EditApiButton.Text = Strings.ButtonEdit;
            RemoveApiButton.Text = Strings.ButtonRemove;
            SelectAllApisButton.Text = Strings.ButtonSelectAll;
            SelectNoApisButton.Text = Strings.ButtonSelectNone;
            EqualizeApiWeightsButton.Text = Strings.ButtonEqualizeWeights;

            ColSubName.Text = Strings.ColumnSubcategoryName;
            ColSubRating.Text = Strings.ColumnRating;
            ColSubWeight.Text = Strings.ColumnWeight;
            ColSubEndpoint.Text = Strings.ColumnEndpoint;
            SubWeightLabel.Text = Strings.LabelWeight;
            SubNsfwCheckBox.Text = Strings.LabelNsfwTag;

            AddSubButton.Text = Strings.ButtonAddSubcategory;
            EditSubButton.Text = Strings.ButtonEdit;
            RemoveSubButton.Text = Strings.ButtonRemove;
            SelectSfwButton.Text = Strings.ButtonSelectSfw;
            UncheckNsfwButton.Text = Strings.ButtonUncheckNsfw;
            EqualizeSubWeightsButton.Text = Strings.ButtonEqualizeWeights;

            RestoreDefaultsButton.Text = Strings.ButtonRestoreDefaults;
            TestSelectedButton.Text = Strings.ButtonTestSelected;
            ApplyNowButton.Text = Strings.ButtonApplyNow;
            SaveButton.Text = Strings.ButtonSave;
            CloseButton.Text = Strings.ButtonClose;

            if (!BusyRunning)
                StatusLabel.Text = Strings.StatusReady;

            PopulateLanguageDropdown(currentLangSelection);
            PopulateFilterDropdown(currentFilter);
        }
        finally
        {
            SuppressUiEvents = false;
        }

        RefreshProvidersList(GetSelectedProvider()?.Id);
    }

    void OnLanguageSelectionChanged(object? sender, EventArgs e)
    {
        if (SuppressUiEvents)
            return;

        string selectedCode = GetSelectedLanguageCode();
        var change = ApplicationServices.ChangeLanguage(selectedCode, persist: false);
        if (!change.Succeeded)
            StatusLabel.Text = Strings.StatusFailed;
    }

    void RefreshProvidersList(string? preserveSelectionId = null)
    {
        SuppressUiEvents = true;
        ProvidersListView.BeginUpdate();
        try
        {
            ProvidersListView.Items.Clear();
            string query = SearchTextBox.Text.Trim();
            NsfwFilterMode filter = GetSelectedFilterMode();
            ListViewItem? itemToSelect = null;

            foreach (WallpaperProvider provider in WorkingProviders)
            {
                if (!string.IsNullOrEmpty(query) &&
                    !provider.Name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    !provider.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ListViewItem item = CreateProviderListViewItem(provider, filter);
                ProvidersListView.Items.Add(item);
                if (preserveSelectionId is not null &&
                    string.Equals(provider.Id, preserveSelectionId, StringComparison.OrdinalIgnoreCase))
                {
                    itemToSelect = item;
                }
            }

            if (itemToSelect is null && ProvidersListView.Items.Count > 0)
                itemToSelect = ProvidersListView.Items[0];

            if (itemToSelect is not null)
            {
                itemToSelect.Selected = true;
                itemToSelect.Focused = true;
                itemToSelect.EnsureVisible();
            }

            UpdateProvidersSummary();
        }
        finally
        {
            ProvidersListView.EndUpdate();
            SuppressUiEvents = false;
        }

        SyncSelectedProviderDetails();
    }

    ListViewItem CreateProviderListViewItem(WallpaperProvider provider, NsfwFilterMode filter)
    {
        int activeSubs = provider.GetEligibleSubcategories(filter).Count;
        var item = new ListViewItem(provider.Name)
        {
            Checked = provider.Enabled,
            Tag = provider
        };
        item.SubItems.Add(FormatModeLabel(provider.ResponseMode));
        item.SubItems.Add(provider.Weight.ToString("N0"));
        item.SubItems.Add($"{activeSubs} / {provider.Subcategories.Count}");
        return item;
    }

    string FormatModeLabel(ProviderResponseMode mode) => mode switch
    {
        ProviderResponseMode.JsonElementPath => Strings.ModeJsonPath,
        ProviderResponseMode.DirectUrlOrImage => Strings.ModeDirectUrlOrImage,
        _ => Strings.ModeBuiltInSpecial
    };

    void RefreshProviderRowCounts()
    {
        NsfwFilterMode filter = GetSelectedFilterMode();
        foreach (ListViewItem item in ProvidersListView.Items)
        {
            if (item.Tag is WallpaperProvider provider)
            {
                int activeSubs = provider.GetEligibleSubcategories(filter).Count;
                item.SubItems[1].Text = FormatModeLabel(provider.ResponseMode);
                item.SubItems[2].Text = provider.Weight.ToString("N0");
                item.SubItems[3].Text = $"{activeSubs} / {provider.Subcategories.Count}";
            }
        }
        UpdateProvidersSummary();
        UpdateSubcategoriesSummary();
    }

    void UpdateProvidersSummary()
    {
        NsfwFilterMode filter = GetSelectedFilterMode();
        int active = WorkingProviders.Count(p => p.Enabled && p.Weight > 0 && p.GetEligibleSubcategories(filter).Count > 0);
        ProvidersSummaryLabel.Text = string.Format(Strings.SummaryActiveCount, active, WorkingProviders.Count);
    }

    WallpaperProvider? GetSelectedProvider() =>
        ProvidersListView.SelectedItems.Count > 0 ? ProvidersListView.SelectedItems[0].Tag as WallpaperProvider : null;

    WallpaperSubcategory? GetSelectedSubcategory() =>
        SubcategoriesListView.SelectedItems.Count > 0 ? SubcategoriesListView.SelectedItems[0].Tag as WallpaperSubcategory : null;

    void OnProviderItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (SuppressUiEvents)
            return;

        if (e.Item.Tag is WallpaperProvider provider)
        {
            provider.Enabled = e.Item.Checked;
            UpdateProvidersSummary();
        }
    }

    void OnProviderSelectionChanged(object? sender, EventArgs e)
    {
        if (SuppressUiEvents)
            return;

        SyncSelectedProviderDetails();
    }

    void SyncSelectedProviderDetails()
    {
        WallpaperProvider? provider = GetSelectedProvider();
        SuppressUiEvents = true;
        try
        {
            if (provider is null)
            {
                ProviderWeightNumeric.Enabled = false;
                ProviderWeightNumeric.Value = 0;
                EditApiButton.Enabled = false;
                RemoveApiButton.Enabled = false;
                SubcategoriesGroup.Text = Strings.GroupSubcategoriesNone;
                SubcategoriesSummaryLabel.Text = string.Empty;
                SubcategoriesListView.Items.Clear();
                AddSubButton.Enabled = false;
                SelectSfwButton.Enabled = false;
                UncheckNsfwButton.Enabled = false;
                EqualizeSubWeightsButton.Enabled = false;
            }
            else
            {
                ProviderWeightNumeric.Enabled = !BusyRunning;
                ProviderWeightNumeric.Value = Math.Clamp(provider.Weight, 0, int.MaxValue);
                EditApiButton.Enabled = !BusyRunning;
                RemoveApiButton.Enabled = !BusyRunning;
                SubcategoriesGroup.Text = string.Format(Strings.GroupSubcategories, provider.Name);
                AddSubButton.Enabled = !BusyRunning;
                SelectSfwButton.Enabled = !BusyRunning;
                UncheckNsfwButton.Enabled = !BusyRunning;
                EqualizeSubWeightsButton.Enabled = !BusyRunning;
                PopulateSubcategoriesList(provider, preserveSubId: null);
            }
        }
        finally
        {
            SuppressUiEvents = false;
        }

        SyncSelectedSubcategoryEditor();
    }

    void PopulateSubcategoriesList(WallpaperProvider provider, string? preserveSubId)
    {
        SuppressUiEvents = true;
        SubcategoriesListView.BeginUpdate();
        try
        {
            SubcategoriesListView.Items.Clear();
            ListViewItem? toSelect = null;

            foreach (WallpaperSubcategory sub in provider.Subcategories)
            {
                var item = new ListViewItem(sub.Name)
                {
                    Checked = sub.Enabled,
                    Tag = sub
                };
                item.SubItems.Add(sub.IsNsfw ? Strings.RatingNsfw : Strings.RatingSfw);
                item.SubItems.Add(sub.Weight.ToString("N0"));
                item.SubItems.Add(sub.Endpoint);
                SubcategoriesListView.Items.Add(item);

                if (preserveSubId is not null && string.Equals(sub.Id, preserveSubId, StringComparison.OrdinalIgnoreCase))
                    toSelect = item;
            }

            if (toSelect is null && SubcategoriesListView.Items.Count > 0)
                toSelect = SubcategoriesListView.Items[0];

            if (toSelect is not null)
            {
                toSelect.Selected = true;
                toSelect.Focused = true;
            }

            UpdateSubcategoriesSummary();
        }
        finally
        {
            SubcategoriesListView.EndUpdate();
            SuppressUiEvents = false;
        }
    }

    void UpdateSubcategoriesSummary()
    {
        WallpaperProvider? provider = GetSelectedProvider();
        if (provider is null)
        {
            SubcategoriesSummaryLabel.Text = string.Empty;
            return;
        }

        int active = provider.GetEligibleSubcategories(GetSelectedFilterMode()).Count;
        SubcategoriesSummaryLabel.Text = string.Format(Strings.SummaryActiveCount, active, provider.Subcategories.Count);
    }

    void OnProviderWeightChanged(object? sender, EventArgs e)
    {
        if (SuppressUiEvents)
            return;

        if (GetSelectedProvider() is { } provider && ProvidersListView.SelectedItems.Count > 0)
        {
            provider.Weight = (int)ProviderWeightNumeric.Value;
            ProvidersListView.SelectedItems[0].SubItems[2].Text = provider.Weight.ToString("N0");
            UpdateProvidersSummary();
        }
    }

    void OnSubcategoryItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (SuppressUiEvents)
            return;

        if (e.Item.Tag is WallpaperSubcategory sub)
        {
            sub.Enabled = e.Item.Checked;
            RefreshProviderRowCounts();
        }
    }

    void OnSubcategorySelectionChanged(object? sender, EventArgs e)
    {
        if (SuppressUiEvents)
            return;

        SyncSelectedSubcategoryEditor();
    }

    void SyncSelectedSubcategoryEditor()
    {
        WallpaperSubcategory? sub = GetSelectedSubcategory();
        SuppressUiEvents = true;
        try
        {
            if (sub is null)
            {
                SubWeightNumeric.Enabled = false;
                SubWeightNumeric.Value = 0;
                SubNsfwCheckBox.Enabled = false;
                SubNsfwCheckBox.Checked = false;
                EditSubButton.Enabled = false;
                RemoveSubButton.Enabled = false;
            }
            else
            {
                SubWeightNumeric.Enabled = !BusyRunning;
                SubWeightNumeric.Value = Math.Clamp(sub.Weight, 0, int.MaxValue);
                SubNsfwCheckBox.Enabled = !BusyRunning;
                SubNsfwCheckBox.Checked = sub.IsNsfw;
                EditSubButton.Enabled = !BusyRunning;
                RemoveSubButton.Enabled = !BusyRunning;
            }
        }
        finally
        {
            SuppressUiEvents = false;
        }
    }

    void OnSubcategoryWeightChanged(object? sender, EventArgs e)
    {
        if (SuppressUiEvents)
            return;

        if (GetSelectedSubcategory() is { } sub && SubcategoriesListView.SelectedItems.Count > 0)
        {
            sub.Weight = (int)SubWeightNumeric.Value;
            SubcategoriesListView.SelectedItems[0].SubItems[2].Text = sub.Weight.ToString("N0");
            RefreshProviderRowCounts();
        }
    }

    void OnSubcategoryNsfwChanged(object? sender, EventArgs e)
    {
        if (SuppressUiEvents)
            return;

        if (GetSelectedSubcategory() is { } sub && SubcategoriesListView.SelectedItems.Count > 0)
        {
            sub.IsNsfw = SubNsfwCheckBox.Checked;
            SubcategoriesListView.SelectedItems[0].SubItems[1].Text = sub.IsNsfw ? Strings.RatingNsfw : Strings.RatingSfw;
            RefreshProviderRowCounts();
        }
    }

    void AddNewProvider()
    {
        using var dialog = new ProviderEditDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultProvider is { } created)
        {
            WorkingProviders.Add(created);
            SearchTextBox.Clear();
            RefreshProvidersList(created.Id);
        }
    }

    void EditSelectedProvider()
    {
        if (GetSelectedProvider() is not { } selected)
            return;

        using var dialog = new ProviderEditDialog(selected);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultProvider is { } updated)
        {
            int idx = WorkingProviders.IndexOf(selected);
            if (idx >= 0)
            {
                WorkingProviders[idx] = updated;
                RefreshProvidersList(updated.Id);
            }
        }
    }

    void RemoveSelectedProvider()
    {
        if (GetSelectedProvider() is not { } selected)
            return;

        int idx = WorkingProviders.IndexOf(selected);
        if (idx < 0)
            return;

        if (selected.IsBuiltIn)
            WorkingRemovedBuiltInIds.Add(selected.Id);

        WorkingProviders.RemoveAt(idx);
        string? nextId = idx < WorkingProviders.Count ? WorkingProviders[idx].Id
            : WorkingProviders.Count > 0 ? WorkingProviders[^1].Id : null;
        RefreshProvidersList(nextId);
    }

    void SetAllVisibleProvidersChecked(bool isChecked)
    {
        SuppressUiEvents = true;
        try
        {
            foreach (ListViewItem item in ProvidersListView.Items)
            {
                item.Checked = isChecked;
                if (item.Tag is WallpaperProvider provider)
                    provider.Enabled = isChecked;
            }
        }
        finally
        {
            SuppressUiEvents = false;
        }
        UpdateProvidersSummary();
    }

    void EqualizeAllProviderWeights()
    {
        foreach (WallpaperProvider provider in WorkingProviders)
            provider.Weight = DefaultWeight;

        RefreshProviderRowCounts();
        SyncSelectedProviderDetails();
    }

    void AddNewSubcategory()
    {
        if (GetSelectedProvider() is not { } provider)
            return;

        using var dialog = new SubcategoryEditDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultSubcategory is { } created)
        {
            provider.Subcategories.Add(created);
            PopulateSubcategoriesList(provider, created.Id);
            SyncSelectedSubcategoryEditor();
            RefreshProviderRowCounts();
        }
    }

    void EditSelectedSubcategory()
    {
        if (GetSelectedProvider() is not { } provider || GetSelectedSubcategory() is not { } sub)
            return;

        using var dialog = new SubcategoryEditDialog(sub);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultSubcategory is { } updated)
        {
            int idx = provider.Subcategories.IndexOf(sub);
            if (idx >= 0)
            {
                provider.Subcategories[idx] = updated;
                PopulateSubcategoriesList(provider, updated.Id);
                SyncSelectedSubcategoryEditor();
                RefreshProviderRowCounts();
            }
        }
    }

    void RemoveSelectedSubcategory()
    {
        if (GetSelectedProvider() is not { } provider || GetSelectedSubcategory() is not { } sub)
            return;

        int idx = provider.Subcategories.IndexOf(sub);
        if (idx < 0)
            return;

        provider.Subcategories.RemoveAt(idx);
        string? nextId = idx < provider.Subcategories.Count ? provider.Subcategories[idx].Id
            : provider.Subcategories.Count > 0 ? provider.Subcategories[^1].Id : null;
        PopulateSubcategoriesList(provider, nextId);
        SyncSelectedSubcategoryEditor();
        RefreshProviderRowCounts();
    }

    void SetSubcategoriesByRating(bool checkSfw)
    {
        if (GetSelectedProvider() is not { } provider)
            return;

        foreach (WallpaperSubcategory sub in provider.Subcategories)
        {
            if (checkSfw && !sub.IsNsfw)
                sub.Enabled = true;
            else if (!checkSfw && sub.IsNsfw)
                sub.Enabled = false;
        }

        PopulateSubcategoriesList(provider, GetSelectedSubcategory()?.Id);
        SyncSelectedSubcategoryEditor();
        RefreshProviderRowCounts();
    }

    void EqualizeSelectedProviderSubWeights()
    {
        if (GetSelectedProvider() is not { } provider)
            return;

        foreach (WallpaperSubcategory sub in provider.Subcategories)
            sub.Weight = DefaultWeight;

        PopulateSubcategoriesList(provider, GetSelectedSubcategory()?.Id);
        SyncSelectedSubcategoryEditor();
        RefreshProviderRowCounts();
    }

    void OnBrowseDirectoryClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            InitialDirectory = ResolveCurrentImageDirectory()
        };
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            string chosen = Path.GetFullPath(dialog.SelectedPath);
            string relative = Path.GetRelativePath(AppPaths.Root, chosen);
            DirectoryTextBox.Text = !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative)
                ? relative
                : chosen;
        }
    }

    void OnOpenFolderClick(object? sender, EventArgs e)
    {
        var result = RunSafely(() =>
        {
            string folder = ResolveCurrentImageDirectory();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        });

        if (!result.Succeeded)
            StatusLabel.Text = Strings.StatusFailed;
    }

    string ResolveCurrentImageDirectory()
    {
        string raw = string.IsNullOrWhiteSpace(DirectoryTextBox.Text) ? "Pictures" : DirectoryTextBox.Text.Trim();
        return Path.GetFullPath(raw, AppPaths.Root);
    }

    void OnRestoreDefaultsClick(object? sender, EventArgs e)
    {
        DialogResult confirm = MessageBox.Show(
            this,
            Strings.ConfirmRestoreDefaults,
            Strings.SettingsTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        List<WallpaperProvider> customProviders = WorkingProviders.Where(p => !p.IsBuiltIn).Select(p => p.Clone()).ToList();
        WorkingProviders = BuiltInCatalog.CreateDefaultProviders();
        WorkingProviders.AddRange(customProviders);
        WorkingRemovedBuiltInIds.Clear();
        RefreshProvidersList(WorkingProviders.FirstOrDefault()?.Id);
    }

    void OnSaveClick(object? sender, EventArgs e)
    {
        var saveResult = RunSafely(() =>
        {
            string dirText = string.IsNullOrWhiteSpace(DirectoryTextBox.Text) ? "Pictures" : DirectoryTextBox.Text.Trim();
            Settings.ImageSaveDirectory.Value = dirText;
            Settings.EnableLogging.Value = LoggingCheckBox.Checked;
            Settings.NsfwFilter.Value = GetSelectedFilterMode();
            Settings.Language.Value = GetSelectedLanguageCode();

            Directory.CreateDirectory(AppPaths.UserData);
            SettingsManager.SaveSettings(ConfigFileName);
            Logger.ActivateLogging = Settings.EnableLogging.Value;
            SourceCatalogManager.SaveCatalog(WorkingProviders, WorkingRemovedBuiltInIds, SourcesFileName);
            Log($"Settings and catalog saved via GUI; providers={WorkingProviders.Count}; filter={Settings.NsfwFilter.Value}", isRun: IsLogEnabled);
        });

        StatusLabel.Text = saveResult.Succeeded ? Strings.SettingsSaved : Strings.StatusFailed;
        if (!saveResult.Succeeded && saveResult.Error is not null)
            MessageBox.Show(this, saveResult.Error.Message, Strings.SettingsTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    async void OnTestSelectedClick(object? sender, EventArgs e)
    {
        if (BusyRunning || GetSelectedProvider() is not { } selectedProvider)
            return;

        WallpaperSubcategory? selectedSub = GetSelectedSubcategory();
        await ExecuteWallpaperActionAsync(token =>
            WallpaperEngine.ApplyFromSpecificSourceAsync(
                selectedProvider.Clone(),
                selectedSub?.Clone(),
                deleteAfterSet: false,
                imageSaveDirectory: ResolveCurrentImageDirectory(),
                filterOverride: GetSelectedFilterMode(),
                cancellationToken: token));
    }

    async void OnApplyNowClick(object? sender, EventArgs e)
    {
        if (BusyRunning)
            return;

        List<WallpaperProvider> snapshot = WorkingProviders.Select(p => p.Clone()).ToList();
        NsfwFilterMode filter = GetSelectedFilterMode();
        string targetFolder = ResolveCurrentImageDirectory();

        await ExecuteWallpaperActionAsync(token =>
            WallpaperEngine.ApplyWeightedRandomWallpaperAsync(
                deleteAfterSet: false,
                imageSaveDirectory: targetFolder,
                providersOverride: snapshot,
                filterOverride: filter,
                progress: message => BeginInvoke(() => StatusLabel.Text = message),
                cancellationToken: token));
    }

    async Task ExecuteWallpaperActionAsync(Func<CancellationToken, Task<AppliedWallpaperResult>> action)
    {
        SetBusyState(true);
        StatusLabel.Text = Strings.StatusWorking;

        ActiveOperationCts?.Cancel();
        ActiveOperationCts?.Dispose();
        ActiveOperationCts = new CancellationTokenSource();

        try
        {
            var result = await RunSafelyAsync(action, ActiveOperationCts.Token).ConfigureAwait(true);
            if (result.Succeeded && result.Value is { } applied)
            {
                StatusLabel.Text = string.Format(Strings.StatusAppliedFromSource, applied.ProviderName, applied.SubcategoryName);
            }
            else if (result.Status == OperationStatus.Canceled)
            {
                StatusLabel.Text = Strings.StatusCanceled;
            }
            else
            {
                StatusLabel.Text = Strings.StatusFailed;
                string detail = result.Error is AggregateException agg && agg.InnerExceptions.Count > 0
                    ? $"{agg.Message}\n\n{agg.InnerExceptions[0].Message}"
                    : result.Error?.Message ?? Strings.StatusFailed;
                MessageBox.Show(this, detail, Strings.SettingsTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            SetBusyState(false);
        }
    }

    void SetBusyState(bool busy)
    {
        BusyRunning = busy;
        ApplyNowButton.Enabled = !busy;
        TestSelectedButton.Enabled = !busy;
        SaveButton.Enabled = !busy;
        RestoreDefaultsButton.Enabled = !busy;
        AddApiButton.Enabled = !busy;
        SyncSelectedProviderDetails();
    }
}
