using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Services;
using BeshoyFanous.XrmToolBox.SolutionSherlock.ViewModels;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock
{
    /// <summary>
    /// Main tool UI, split into two tabs:
    /// - Search Components: free-text search across every solution.
    /// - Browse Solutions: page through every solution (sortable by name or date),
    ///   drill into any one's full component roster, export that roster to CSV, or
    ///   export the solution itself as a deployable .zip via ExportSolutionRequest.
    ///
    /// Threading note: this control deliberately uses TWO different background-work
    /// patterns side by side, on purpose rather than by accident:
    /// - Search, Load All Solutions, and View Components use XrmToolBox's own
    ///   WorkAsync/BackgroundWorker mechanism - the native, already-integrated way
    ///   this host runs background work, with built-in progress/cancellation.
    /// - Solution export uses genuine async/await (BrowseSolutionsViewModel and the
    ///   services below it are Task-based throughout). This is a WinForms UserControl,
    ///   so "async void" event handlers are the correct, idiomatic entry point (the
    ///   WinForms message loop installs a SynchronizationContext, so awaited
    ///   continuations correctly resume on the UI thread with no manual Invoke/
    ///   BeginInvoke needed) - this is the standard, accepted exception to the
    ///   "async void is bad" rule, which otherwise applies to library code.
    /// Business/orchestration logic for the export flow lives in
    /// BrowseSolutionsViewModel and the Services/Interfaces layer, not here - this
    /// class's job for that flow is strictly UI: wire clicks, show progress, render
    /// results.
    /// </summary>
    public partial class SolutionSherlockControl : PluginControlBase, IGitHubPlugin, IHelpPlugin
    {
        private const string OpenSolutionColumnName = "colOpenSolution";
        private const string SolutionActionsColumnName = "colSolutionActions";

        private SolutionCache _solutionCache;
        private MetadataResolver _metadataResolver;
        private SearchEngine _searchEngine;
        private SolutionExplorerService _solutionExplorerService;
        private ISolutionExportService _solutionExportService;
        private BrowseSolutionsViewModel _browseViewModel;

        // Constructed once - neither depends on the Dataverse connection.
        private readonly IFileSystemService _fileSystemService;
        private readonly IDialogService _dialogService;

        private List<SearchResult> _lastResults = new List<SearchResult>();
        private int _rightClickedRowIndex = -1;

        private List<SolutionComponentDetail> _lastComponentDetails;
        private readonly HashSet<string> _collapsedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _componentsSolutionFriendlyName;

        // Dock-panel pin state (Browse Solutions tab): pinned panels are never
        // auto-collapsed by View Component or a connection reset - see
        // AutoCollapseSolutions/AutoCollapseComponents.
        private bool _solutionsPinned;
        private bool _componentsPinned;

        // Cancellation + timing for whichever WorkAsync-based operation (search, load
        // solutions, or view components) is currently running or most recently ran.
        private CancellationTokenSource _operationCancellation;
        private Stopwatch _operationStopwatch;

        // IGitHubPlugin / IHelpPlugin - update these before distributing the tool.
        public string RepositoryName => "SolutionSherlock";
        public string UserName => "beshofk";
        public string HelpUrl => "https://github.com/beshofk/SolutionSherlock";

        public SolutionSherlockControl()
        {
            InitializeComponent();

            // Manual composition instead of a DI container: XrmToolBox instantiates
            // plugin controls itself via MEF with a parameterless constructor
            // (PluginBase.GetControl() => new SolutionSherlockControl()), so
            // there's no host-provided container to register services with. These two
            // have no dependency on the Dataverse connection, so they're built once
            // here; everything connection-scoped is (re)built in UpdateConnection.
            _fileSystemService = new FileSystemService();
            _dialogService = new DialogService();

            InitializeSearchControls();
            InitializeBrowseControls();
            ApplyModernTheme();
        }

        /// <summary>
        /// Cosmetic-only pass applying the shared color/typography system (UiTheme) to
        /// both tabs - grids, buttons, toolstrip, section headers, backgrounds. Runs
        /// once after both tabs' controls/columns/event handlers already exist; never
        /// touches data sources, layout (Dock/Anchor), or event wiring.
        /// </summary>
        private void ApplyModernTheme()
        {
            BackColor = UiTheme.AppBackground;
            pnlBrowseRoot.BackColor = UiTheme.AppBackground;
            pnlSearchRoot.BackColor = UiTheme.AppBackground;
            pnlConnectionStatus.BackColor = UiTheme.Surface;
            pnlLog.BackColor = UiTheme.Surface;

            UiTheme.StyleGrid(dgvResults);
            UiTheme.StyleGrid(dgvSolutions);
            UiTheme.StyleGrid(dgvSolutionComponents);

            UiTheme.StyleSectionHeader(grpComponentDetails);
            UiTheme.StyleSectionHeader(grpSearchCriteria);
            UiTheme.StyleSectionHeader(grpSearchResults);

            UiTheme.StylePrimaryButton(btnSearch);
            UiTheme.StyleSecondaryButton(btnAbort);
            UiTheme.StyleSecondaryButton(btnClear);
            UiTheme.StyleSecondaryButton(btnPrevPage);
            UiTheme.StyleSecondaryButton(btnNextPage);
            UiTheme.StylePrimaryButton(btnExportComponents);

            tsBrowseActions.Renderer = UiTheme.CreateToolStripRenderer();
            tsBrowseActions.BackColor = UiTheme.Surface;
            btnLoadAllSolutions.ForeColor = UiTheme.TextPrimary;
            btnAbortBrowse.ForeColor = UiTheme.Error;

            lblSolutionsSearch.ForeColor = UiTheme.TextSecondary;

            // Solutions/Components dock-panel headers - Visual Studio tool-window look
            // (accent edge + bold caption + flat collapse glyph) - plus the matching
            // compact auto-hide tabs shown only while a panel is collapsed.
            UiTheme.StyleDockHeader(pnlSolutionsDockHeader, pnlSolutionsHeaderAccent, lblSolutionsDockTitle, btnSolutionsCollapseToggle);
            UiTheme.StyleDockHeader(pnlComponentsDockHeader, pnlComponentsHeaderAccent, lblComponentsDockTitle, btnComponentsCollapseToggle);
            UiTheme.StyleDockTab(lblSolutionsCollapsedTab);
            UiTheme.StyleDockTab(lblComponentsCollapsedTab);
        }

        // ============================================================
        // Search Components tab
        // ============================================================

        private void InitializeSearchControls()
        {
            cboMainType.DisplayMember = nameof(ComponentTypeInfo.DisplayName);
            foreach (var typeInfo in ComponentTypeMap.MainTypes)
                cboMainType.Items.Add(typeInfo);
            cboMainType.SelectedIndex = 0; // Entity, per ComponentTypeMap.MainTypes ordering
            cboMainType.SelectedIndexChanged += (s, e) => RefreshSubTypeOptions();

            RefreshSubTypeOptions();
            ConfigureResultsGridColumns();

            btnSearch.Click += BtnSearch_Click;
            btnAbort.Click += BtnAbort_Click;
            btnClear.Click += BtnClear_Click;

            dgvResults.CellMouseDown += DgvResults_CellMouseDown;
            dgvResults.CellContentClick += DgvResults_CellContentClick;
            miCopySolutionName.Click += (s, e) => CopySelectedValue(r => r.SolutionFriendlyName);
            miCopyComponentName.Click += (s, e) => CopySelectedValue(r => r.ComponentLogicalName);
            miOpenInMaker.Click += (s, e) => OpenSelectedSolution();
        }

        private void ConfigureResultsGridColumns()
        {
            dgvResults.Columns.Clear();

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSolution", HeaderText = "Solution",
                DataPropertyName = nameof(SearchResultRow.Solution), FillWeight = 130
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colType", HeaderText = "Type",
                DataPropertyName = nameof(SearchResultRow.Type), FillWeight = 65
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPublisher", HeaderText = "Publisher",
                DataPropertyName = nameof(SearchResultRow.Publisher), FillWeight = 90
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colComponentType", HeaderText = "Component Type",
                DataPropertyName = nameof(SearchResultRow.Component), FillWeight = 100
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colComponentName", HeaderText = "Component",
                DataPropertyName = nameof(SearchResultRow.Name), FillWeight = 160
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colParent", HeaderText = "Parent Entity",
                DataPropertyName = nameof(SearchResultRow.Parent), FillWeight = 90
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colVersion", HeaderText = "Version",
                DataPropertyName = nameof(SearchResultRow.Version), FillWeight = 55
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colComponentGuid", HeaderText = "GUID",
                DataPropertyName = nameof(SearchResultRow.ComponentGuid), FillWeight = 95,
                DefaultCellStyle = { ForeColor = SystemColors.GrayText, Font = new Font("Consolas", 8f) }
            });
            dgvResults.Columns.Add(new DataGridViewLinkColumn
            {
                Name = OpenSolutionColumnName, HeaderText = string.Empty,
                DataPropertyName = nameof(SearchResultRow.SolutionUrl),
                UseColumnTextForLinkValue = true, Text = "Open solution \u2197",
                TrackVisitedState = false, FillWeight = 85
            });
        }

        private void RefreshSubTypeOptions()
        {
            cboSubType.SelectedIndexChanged -= CboSubType_SelectedIndexChanged;
            cboSubType.Items.Clear();

            var mainKind = ((ComponentTypeInfo)cboMainType.SelectedItem).Kind;

            if (mainKind == ComponentTypeKind.Workflow)
            {
                lblSubType.Text = "Category:";
                cboSubType.Items.Add("— All categories —");
                foreach (var category in ProcessCategories.All)
                    cboSubType.Items.Add(category);
                cboSubType.SelectedIndex = 0;
                cboSubType.Enabled = true;

                lblSubSearch.Visible = false;
                txtSubSearch.Visible = false;
                txtSubSearch.Enabled = false;
                txtSubSearch.Text = string.Empty;
            }
            else
            {
                lblSubType.Text = "Sub-component:";
                lblSubSearch.Visible = true;
                txtSubSearch.Visible = true;

                cboSubType.Items.Add("— None —");
                var hasSubTypes = ComponentTypeMap.SubTypesByMain.TryGetValue(mainKind, out var subKinds) && subKinds.Length > 0;
                if (hasSubTypes)
                {
                    foreach (var subKind in subKinds)
                        cboSubType.Items.Add(ComponentTypeMap.Get(subKind));
                }

                cboSubType.SelectedIndex = 0;
                cboSubType.Enabled = hasSubTypes;
                txtSubSearch.Enabled = false;
                txtSubSearch.Text = string.Empty;
            }

            cboSubType.SelectedIndexChanged += CboSubType_SelectedIndexChanged;
        }

        private void CboSubType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mainKind = ((ComponentTypeInfo)cboMainType.SelectedItem).Kind;
            if (mainKind == ComponentTypeKind.Workflow)
                return;

            txtSubSearch.Enabled = cboSubType.Enabled && cboSubType.SelectedIndex > 0;
            if (!txtSubSearch.Enabled) txtSubSearch.Text = string.Empty;
        }

        /// <summary>
        /// Called by XrmToolBox whenever the active connection changes. Everything
        /// connection-scoped is rebuilt here, including BrowseSolutionsViewModel,
        /// which is deliberately recreated (not just re-pointed) so its internal
        /// paging/sort/selection state resets cleanly on reconnect.
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            _solutionCache = new SolutionCache(newService);
            _metadataResolver = new MetadataResolver(newService);
            _searchEngine = new SearchEngine(newService, _solutionCache, _metadataResolver);
            _solutionExplorerService = new SolutionExplorerService(newService, _metadataResolver);
            _solutionExportService = new SolutionExportService(newService);
            _browseViewModel = new BrowseSolutionsViewModel(_solutionCache, _solutionExportService, _fileSystemService);

            LogInfo($"Connection updated: {detail?.OrganizationFriendlyName} ({(detail?.UseOnline == true ? "Online" : "On-Premises")})");

            dgvSolutions.DataSource = null;
            dgvSolutionComponents.DataSource = null;
            _lastComponentDetails = null;
            _collapsedGroupKeys.Clear();
            ResetComponentDetailsPanel();
            txtSolutionSearch.Text = string.Empty;
            lblPageIndicator.Text = "No solutions loaded yet — click Load All Solutions.";
            lblComponentsHeader.Text = "Select a solution above, then click View Components.";
            lblExportStatus.Text = string.Empty;
            SetAllActionButtonsEnabled(true);
            btnExportComponents.Enabled = false;
            _solutionsPinned = false;
            _componentsPinned = false;
            ApplySolutionsPinnedState();
            ApplyComponentsPinnedState();
            SetSolutionsGridCollapsed(false);
            SetComponentsGridCollapsed(true);

            lblConnectionInfo.Text = detail != null
                ? $"Connected to: {detail.OrganizationFriendlyName} ({(detail.UseOnline ? "Online" : "On-Premises")}, v{detail.OrganizationVersion})"
                : string.Empty;
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show(this, "Please connect to an organization first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mainType = ((ComponentTypeInfo)cboMainType.SelectedItem).Kind;
            var mainText = txtMainSearch.Text?.Trim();

            ComponentTypeKind? subType = null;
            string subText = null;
            int? processCategory = null;

            if (mainType == ComponentTypeKind.Workflow)
            {
                if (cboSubType.SelectedIndex > 0 && cboSubType.SelectedItem is ProcessCategoryOption categoryOption)
                    processCategory = categoryOption.Value;
            }
            else
            {
                subType = cboSubType.Enabled && cboSubType.SelectedIndex > 0
                    ? ((ComponentTypeInfo)cboSubType.SelectedItem).Kind
                    : (ComponentTypeKind?)null;
                subText = txtSubSearch.Text?.Trim();
            }

            if (string.IsNullOrEmpty(mainText) && string.IsNullOrEmpty(subText) && processCategory == null)
            {
                MessageBox.Show(this,
                    "Enter at least a main or sub-component search term (or pick a Process " +
                    "category). Leaving everything blank can return a very large result set " +
                    "in bigger environments.",
                    "Refine your search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var criteria = new SearchCriteria
            {
                MainType = mainType,
                MainSearchText = mainText,
                SubType = subType,
                SubSearchText = subText,
                ProcessCategory = processCategory,
                ManagedOnly = chkManagedOnly.Checked,
                UnmanagedOnly = chkUnmanagedOnly.Checked
            };

            dgvResults.DataSource = null;
            var cancellationToken = BeginAsyncOperation("Search started.");

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Searching solutions for matching components...",
                Work = (worker, args) =>
                {
                    var progress = new SearchProgress(cancellationToken, message => worker.ReportProgress(0, message));
                    try
                    {
                        if (_solutionCache.LastRefreshedUtc == DateTime.MinValue)
                        {
                            progress.Log("Loading solution list...");
                            _solutionCache.Refresh(progress);
                        }

                        args.Result = _searchEngine.Search(criteria, progress);
                    }
                    catch (OperationCanceledException)
                    {
                        args.Cancel = true;
                    }
                },
                ProgressChanged = args => AppendLog(args.UserState as string),
                PostWorkCallBack = args =>
                {
                    EndAsyncOperation();

                    if (args.Cancelled)
                    {
                        lblSummary.ForeColor = UiTheme.Warning;
                        lblSummary.Text = "Search cancelled.";
                        AppendLog($"Search cancelled by user after {_operationStopwatch.ElapsedMilliseconds}ms.");
                        return;
                    }

                    if (args.Error != null)
                    {
                        lblSummary.ForeColor = UiTheme.Error;
                        lblSummary.Text = "Search failed.";
                        AppendLog($"Search failed after {_operationStopwatch.ElapsedMilliseconds}ms: {args.Error.Message}");
                        MessageBox.Show(this, "The search could not be completed: " + args.Error.Message,
                            "Search failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogError(args.Error.ToString());
                        return;
                    }

                    _lastResults = (List<SearchResult>)args.Result;
                    RenderResults(_lastResults);
                }
            });
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtMainSearch.Text = string.Empty;
            txtSubSearch.Text = string.Empty;
            cboSubType.SelectedIndex = 0;
            chkManagedOnly.Checked = false;
            chkUnmanagedOnly.Checked = false;

            _lastResults = new List<SearchResult>();
            RenderResults(_lastResults);
        }

        private void RenderResults(List<SearchResult> results)
        {
            if (results == null || results.Count == 0)
            {
                dgvResults.DataSource = null;
                lblSummary.Text = results == null
                    ? "Enter search criteria and click Search."
                    : "No matching components found. Try broadening your search.";
                return;
            }

            var solutionCount = results.Select(r => r.SolutionId).Distinct().Count();
            var componentCount = results.Select(r => r.ComponentObjectId).Distinct().Count();
            lblSummary.ForeColor = UiTheme.TextPrimary;
            lblSummary.Text = $"{componentCount} component(s) found across {solutionCount} solution(s)";

            var ordered = OrderResults(results);

            dgvResults.DataSource = ordered.Select(r => new SearchResultRow
            {
                Solution = r.SolutionFriendlyName,
                Type = r.IsManaged ? "Managed" : "Unmanaged",
                Publisher = r.Publisher,
                Component = r.ComponentTypeDisplay,
                Name = $"{r.ComponentDisplayName} ({r.ComponentLogicalName})",
                Parent = r.ParentEntityLogicalName,
                Version = r.Version,
                SolutionUrl = BuildSolutionUrl(r.SolutionId),
                ComponentGuid = r.ComponentObjectId
            }).ToList();
        }

        private List<SearchResult> OrderResults(List<SearchResult> results)
        {
            return results.OrderBy(r => r.SolutionFriendlyName).ThenBy(r => r.ComponentDisplayName).ToList();
        }

        private void DgvResults_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
            dgvResults.ClearSelection();
            dgvResults.Rows[e.RowIndex].Selected = true;
            _rightClickedRowIndex = e.RowIndex;
        }

        private void DgvResults_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvResults.Columns[e.ColumnIndex].Name != OpenSolutionColumnName) return;

            var row = dgvResults.Rows[e.RowIndex];

            // Ensure the expected column exists before accessing by name
            if (!dgvResults.Columns.Contains(OpenSolutionColumnName))
            {
                MessageBox.Show(this, "No solution column defined.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var cell = row.Cells[OpenSolutionColumnName];

            // Prefer a real URL stored in the cell.Tag, fall back to the cell.Value or the bound item
            string url = null;
            if (cell?.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                url = tag;
            }
            else if (cell?.Value is string valueStr && !string.IsNullOrWhiteSpace(valueStr))
            {
                // If the displayed text is actually a URL, use it. Otherwise we'll try other sources below.
                if (Uri.IsWellFormedUriString(valueStr, UriKind.Absolute) || System.IO.File.Exists(valueStr))
                    url = valueStr;
            }

            // If still not found, try common property names on the bound data item
            if (string.IsNullOrWhiteSpace(url) && row.DataBoundItem != null)
            {
                var item = row.DataBoundItem;
                var type = item.GetType();
                foreach (var propName in new[] { "SolutionUrl", "Url", "Link", "OpenSolutionUrl" })
                {
                    var prop = type.GetProperty(propName);
                    if (prop != null)
                    {
                        var propVal = prop.GetValue(item) as string;
                        if (!string.IsNullOrWhiteSpace(propVal))
                        {
                            url = propVal;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(this, "No solution link is available for this row.", "Not available",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            TrySwapSearchAndBrowseTabs();
        }

        private void TrySwapSearchAndBrowseTabs()
        {
            // tabMain now sits inside splitMain rather than directly under this
            // control, so it's referenced directly rather than located via
            // this.Controls (which only enumerates direct children).
            var tab = tabMain;
            if (tab == null) return;

            int searchIndex = -1, browseIndex = -1;
            for (int i = 0; i < tab.TabPages.Count; i++)
            {
                var text = tab.TabPages[i].Text ?? string.Empty;
                if (text.IndexOf("search", StringComparison.OrdinalIgnoreCase) >= 0)
                    searchIndex = i;
                if (text.IndexOf("browse", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("solution", StringComparison.OrdinalIgnoreCase) >= 0)
                    browseIndex = i;
            }

            if (searchIndex < 0 || browseIndex < 0 || searchIndex == browseIndex) return;

            // Swap pages by removing the one with the larger index and inserting it at the smaller index
            if (searchIndex < browseIndex)
            {
                var browsePage = tab.TabPages[browseIndex];
                tab.TabPages.RemoveAt(browseIndex);
                tab.TabPages.Insert(searchIndex, browsePage);
            }
            else
            {
                var searchPage = tab.TabPages[searchIndex];
                tab.TabPages.RemoveAt(searchIndex);
                tab.TabPages.Insert(browseIndex, searchPage);
            }
        }

        private SearchResult GetSelectedResult()
        {
            if (_rightClickedRowIndex < 0 || _lastResults.Count == 0) return null;
            var ordered = OrderResults(_lastResults);
            return _rightClickedRowIndex < ordered.Count ? ordered[_rightClickedRowIndex] : null;
        }

        private void CopySelectedValue(Func<SearchResult, string> selector)
        {
            var row = GetSelectedResult();
            if (row == null) return;
            var value = selector(row);
            if (!string.IsNullOrEmpty(value))
                Clipboard.SetText(value);
        }

        private void OpenSelectedSolution()
        {
            var row = GetSelectedResult();
            if (row == null) return;
            OpenUrl(BuildSolutionUrl(row.SolutionId));
        }

        // ============================================================
        // Browse Solutions tab
        // ============================================================

        private void InitializeBrowseControls()
        {
            ConfigureSolutionsGridColumns();
            ConfigureSolutionComponentsGridColumns();

            btnLoadAllSolutions.Click += BtnLoadAllSolutions_Click;
            btnPrevPage.Click += (s, e) => { _browseViewModel.PreviousPage(); RenderSolutionsPage(); };
            btnNextPage.Click += (s, e) => { _browseViewModel.NextPage(); RenderSolutionsPage(); };
            dgvSolutions.SelectionChanged += DgvSolutions_SelectionChanged;
            dgvSolutions.ColumnHeaderMouseClick += DgvSolutions_ColumnHeaderMouseClick;
            dgvSolutions.CellDoubleClick += DgvSolutions_CellDoubleClick;
            dgvSolutions.CellContentClick += DgvSolutions_CellContentClick;
            txtSolutionSearch.TextChanged += (s, e) => { _browseViewModel.SetSearchText(txtSolutionSearch.Text); RenderSolutionsPage(); };
            miActionsViewComponents.Click += BtnViewComponents_Click;
            miActionsOpenInBrowser.Click += (s, e) =>
            {
                var selected = GetSelectedSolutionRow();
                if (selected == null)
                {
                    MessageBox.Show(this, "Select a solution first.", "No solution selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                OpenUrl(BuildSolutionUrl(selected.SolutionId));
            };
            miActionsExportSolution.Click += async (s, e) => await ExportSelectedSolutionAsync();
            btnExportComponents.Click += BtnExportComponents_Click;
            btnAbortBrowse.Click += BtnAbort_Click;
            btnSolutionsPinToggle.Click += (s, e) => ToggleSolutionsPinned();
            btnComponentsPinToggle.Click += (s, e) => ToggleComponentsPinned();
            btnSolutionsCollapseToggle.Click += (s, e) => OnSolutionsArrowClicked();
            lblSolutionsCollapsedTab.Click += (s, e) => SetSolutionsGridCollapsed(false);
            btnComponentsCollapseToggle.Click += (s, e) => OnComponentsArrowClicked();
            lblComponentsCollapsedTab.Click += (s, e) => SetComponentsGridCollapsed(false);

            // Subtle hover feedback for the compact auto-hide tabs (Visual Studio style).
            lblSolutionsCollapsedTab.MouseEnter += (s, e) => lblSolutionsCollapsedTab.BackColor = UiTheme.PrimaryLight;
            lblSolutionsCollapsedTab.MouseLeave += (s, e) => lblSolutionsCollapsedTab.BackColor = UiTheme.NestedGroupHeaderBackColor;
            lblComponentsCollapsedTab.MouseEnter += (s, e) => lblComponentsCollapsedTab.BackColor = UiTheme.PrimaryLight;
            lblComponentsCollapsedTab.MouseLeave += (s, e) => lblComponentsCollapsedTab.BackColor = UiTheme.NestedGroupHeaderBackColor;

            dgvSolutionComponents.CellFormatting += DgvSolutionComponents_CellFormatting;
            dgvSolutionComponents.SelectionChanged += DgvSolutionComponents_SelectionChanged;
            dgvSolutionComponents.CellClick += DgvSolutionComponents_CellClick;

            // Initial layout: Solutions expanded, Components collapsed until View Component is run.
            ApplySolutionsPinnedState();
            ApplyComponentsPinnedState();
            SetSolutionsGridCollapsed(false);
            SetComponentsGridCollapsed(true);
        }

        /// <summary>Pure layout toggle - never touches dgvSolutions' data source or selection.</summary>
        private void SetSolutionsGridCollapsed(bool collapsed)
        {
            splitBrowseTop.Panel1Collapsed = collapsed;
            lblSolutionsCollapsedTab.Visible = collapsed;
            UpdateDockTabStripVisibility();
        }

        /// <summary>Pure layout toggle - never touches dgvSolutionComponents' data source or selection.</summary>
        private void SetComponentsGridCollapsed(bool collapsed)
        {
            splitBrowseTop.Panel2Collapsed = collapsed;
            lblComponentsCollapsedTab.Visible = collapsed;
            UpdateDockTabStripVisibility();
        }

        /// <summary>Keeps the auto-hide strip collapsed to zero height whenever neither dock panel is hidden.</summary>
        private void UpdateDockTabStripVisibility()
        {
            pnlDockTabStrip.Visible = lblSolutionsCollapsedTab.Visible || lblComponentsCollapsedTab.Visible;
        }

        private void ToggleSolutionsPinned()
        {
            _solutionsPinned = !_solutionsPinned;
            ApplySolutionsPinnedState();
        }

        private void ToggleComponentsPinned()
        {
            _componentsPinned = !_componentsPinned;
            ApplyComponentsPinnedState();
        }

        /// <summary>Pin only ever forces the panel visible - it never touches the arrow's visibility/enabled state.</summary>
        private void ApplySolutionsPinnedState()
        {
            UiTheme.StyleDockPinButton(btnSolutionsPinToggle, _solutionsPinned);
            if (_solutionsPinned) SetSolutionsGridCollapsed(false);
        }

        /// <summary>Pin only ever forces the panel visible - it never touches the arrow's visibility/enabled state.</summary>
        private void ApplyComponentsPinnedState()
        {
            UiTheme.StyleDockPinButton(btnComponentsPinToggle, _componentsPinned);
            if (_componentsPinned) SetComponentsGridCollapsed(false);
        }

        /// <summary>Arrow click: collapses the panel when unpinned, or - since a pinned panel may never collapse - reveals the other panel beside it instead.</summary>
        private void OnSolutionsArrowClicked()
        {
            if (_solutionsPinned) ShowBothPanelsSideBySide();
            else SetSolutionsGridCollapsed(true);
        }

        /// <summary>Arrow click: collapses the panel when unpinned, or - since a pinned panel may never collapse - reveals the other panel beside it instead.</summary>
        private void OnComponentsArrowClicked()
        {
            if (_componentsPinned) ShowBothPanelsSideBySide();
            else SetComponentsGridCollapsed(true);
        }

        /// <summary>Expands both panels and resets the splitter to an even ~50/50 split, clamped to each panel's minimum size.</summary>
        private void ShowBothPanelsSideBySide()
        {
            SetSolutionsGridCollapsed(false);
            SetComponentsGridCollapsed(false);

            var available = splitBrowseTop.Width - splitBrowseTop.SplitterWidth;
            var min = splitBrowseTop.Panel1MinSize;
            var max = available - splitBrowseTop.Panel2MinSize;
            if (max > min) splitBrowseTop.SplitterDistance = Math.Max(min, Math.Min(available / 2, max));
        }

        /// <summary>Automatic layout (View Component, connection reset) must never collapse a pinned panel; expanding is always allowed.</summary>
        private void AutoCollapseSolutions(bool collapsed)
        {
            if (collapsed && _solutionsPinned) return;
            SetSolutionsGridCollapsed(collapsed);
        }

        /// <summary>Automatic layout (View Component, connection reset) must never collapse a pinned panel; expanding is always allowed.</summary>
        private void AutoCollapseComponents(bool collapsed)
        {
            if (collapsed && _componentsPinned) return;
            SetComponentsGridCollapsed(collapsed);
        }

        private void ConfigureSolutionsGridColumns()
        {
            dgvSolutions.AutoGenerateColumns = false;
            dgvSolutions.Columns.Clear();
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colFriendlyName", HeaderText = "Friendly Name", DataPropertyName = nameof(SolutionListRow.FriendlyName), FillWeight = 150 });
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colUniqueName", HeaderText = "Unique Name", DataPropertyName = nameof(SolutionListRow.UniqueName), FillWeight = 130 });
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colSolutionType", HeaderText = "Type", DataPropertyName = nameof(SolutionListRow.Type), FillWeight = 65 });
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colSolutionPublisher", HeaderText = "Publisher", DataPropertyName = nameof(SolutionListRow.Publisher), FillWeight = 100 });
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colSolutionVersion", HeaderText = "Version", DataPropertyName = nameof(SolutionListRow.Version), FillWeight = 65 });
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colModifiedOn", HeaderText = "Modified On", DataPropertyName = nameof(SolutionListRow.ModifiedOnDisplay), FillWeight = 100 });
            dgvSolutions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSolutionGuid", HeaderText = "GUID", DataPropertyName = nameof(SolutionListRow.SolutionId), FillWeight = 95,
                DefaultCellStyle = { ForeColor = SystemColors.GrayText, Font = new Font("Consolas", 8f) }
            });
            dgvSolutions.Columns.Add(new DataGridViewButtonColumn
            {
                Name = SolutionActionsColumnName, HeaderText = string.Empty, Text = "Actions \u25be",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, FillWeight = 80,
                DefaultCellStyle = { ForeColor = UiTheme.Primary, Font = new Font(dgvSolutions.Font, FontStyle.Bold) }
            });
        }

        private void ConfigureSolutionComponentsGridColumns()
        {
            dgvSolutionComponents.AutoGenerateColumns = false;
            dgvSolutionComponents.Columns.Clear();
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colCompDisplayName", HeaderText = "Display Name", DataPropertyName = nameof(SolutionComponentDisplayRow.DisplayName), FillWeight = 160 });
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colCompName", HeaderText = "Name", DataPropertyName = nameof(SolutionComponentDisplayRow.Name), FillWeight = 140 });
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colCompType", HeaderText = "Type", DataPropertyName = nameof(SolutionComponentDisplayRow.TypeDisplay), FillWeight = 110 });
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colCompState", HeaderText = "State", DataPropertyName = nameof(SolutionComponentDisplayRow.State), FillWeight = 70 });
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colCompCustomizable", HeaderText = "Customizable", DataPropertyName = nameof(SolutionComponentDisplayRow.Customizable), FillWeight = 80 });
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "colCompDescription", HeaderText = "Description", DataPropertyName = nameof(SolutionComponentDisplayRow.Description), FillWeight = 180 });
            dgvSolutionComponents.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCompGuid", HeaderText = "GUID", DataPropertyName = nameof(SolutionComponentDisplayRow.ObjectId), FillWeight = 95,
                DefaultCellStyle = { ForeColor = SystemColors.GrayText, Font = new Font("Consolas", 8f) }
            });
        }

        /// <summary>
        /// Groups components for display: entity-scoped rows (Field, Form, View,
        /// Chart, Business Rule, and the Entity component itself) group under their
        /// parent entity's display name; everything else groups by its own component
        /// type, same as before this change. Within each group, the Entity's own row
        /// (if present) sorts first, then everything else alphabetically by
        /// DisplayName - this ordering is established once here (not by the caller),
        /// and LINQ's GroupBy is documented to preserve it, so grouping never re-sorts
        /// on top of it. A group with zero matching components never appears here in
        /// the first place, since groups are derived only from rows that exist.
        /// </summary>
        /// <summary>Synthetic top-level group key for the "Entity" super-group - namespaced so it can never collide with a real component type name.</summary>
        private const string EntitiesTopGroupKey = "__Entities__";

        /// <summary>
        /// Canonical display order for component-type sub-groups within an entity.
        /// Anything not listed here (shouldn't normally happen for entity-scoped rows)
        /// is appended alphabetically after these, so nothing is ever silently dropped.
        /// </summary>
        private static readonly string[] EntitySubComponentTypeOrder =
        {
            "Form", "View", "Chart", "Field", "Key",
            "1:N Relationship", "N:1 Relationship", "N:N Relationship",
            "Business Rule", "Hierarchy Settings", "Dashboard"
        };

        /// <summary>Pluralized header text for an entity sub-component type group, e.g. "Form" -> "Forms".</summary>
        private static string PluralizeTypeDisplay(string typeDisplay)
        {
            switch (typeDisplay)
            {
                case "Form": return "Forms";
                case "View": return "Views";
                case "Chart": return "Charts";
                case "Field": return "Fields";
                case "Key": return "Keys";
                case "1:N Relationship": return "1:N Relationships";
                case "N:1 Relationship": return "N:1 Relationships";
                case "N:N Relationship": return "N:N Relationships";
                case "Business Rule": return "Business Rules";
                case "Hierarchy Settings": return "Hierarchy Settings";
                case "Dashboard": return "Dashboards";
                default: return typeDisplay + "s";
            }
        }

        /// <summary>
        /// Builds the full grouped/collapsible row list for dgvSolutionComponents:
        /// - An "Entity" super-group (only added when at least one row has a known
        ///   parent entity), containing one nested header per distinct entity. Within
        ///   each entity, the entity's own row (if present) anchors the top, followed
        ///   by one nested header per sub-component type (Forms, Views, Fields, ...)
        ///   in a fixed canonical order, each containing its own member rows - this is
        ///   what "Entity -> Entity-specific Components" means at the UI level.
        /// - Everything else grouped by component type exactly as before, as
        ///   sibling top-level groups alongside "Entity".
        /// All rows are always produced (nothing is pre-filtered for a collapsed
        /// state) - ApplyGroupVisibility is what actually hides collapsed content,
        /// so toggling never needs to rebuild or rebind this list.
        /// </summary>
        private static List<SolutionComponentDisplayRow> BuildGroupedComponentRows(List<SolutionComponentDetail> details)
        {
            var rows = new List<SolutionComponentDisplayRow>();

            var entityScoped = details.Where(d => !string.IsNullOrEmpty(d.ParentEntityDisplayName)).ToList();
            var typeScoped = details.Where(d => string.IsNullOrEmpty(d.ParentEntityDisplayName)).ToList();

            if (entityScoped.Count > 0)
            {
                var distinctEntityCount = entityScoped
                    .Select(d => d.ParentEntityDisplayName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                rows.Add(new SolutionComponentDisplayRow
                {
                    IsGroupHeader = true,
                    GroupLevel = 0,
                    GroupKey = EntitiesTopGroupKey,
                    TopGroupKey = EntitiesTopGroupKey,
                    GroupHeaderText = $"Entity ({distinctEntityCount})"
                });

                var entityGroups = entityScoped
                    .GroupBy(d => d.ParentEntityDisplayName, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var entityGroup in entityGroups)
                {
                    var entityGroupKey = "entity::" + entityGroup.Key;

                    rows.Add(new SolutionComponentDisplayRow
                    {
                        IsGroupHeader = true,
                        GroupLevel = 1,
                        GroupKey = entityGroupKey,
                        TopGroupKey = EntitiesTopGroupKey,
                        GroupHeaderText = $"{entityGroup.Key} ({entityGroup.Count()})"
                    });

                    // Entity's own row (ComponentTypeCode == 1) anchors the top of its
                    // group, unlabeled, matching "entity, then its children".
                    var entityOwnRows = entityGroup.Where(d => d.ComponentTypeCode == 1);
                    foreach (var item in entityOwnRows)
                    {
                        rows.Add(ToMemberRow(item, EntitiesTopGroupKey, entityGroupKey, null));
                    }

                    // Everything else nests under a per-type header (Forms, Views,
                    // Fields, Keys, 1:N/N:1/N:N Relationships, Business Rules,
                    // Hierarchy Settings, Dashboards), in the canonical order above.
                    var subComponentTypeGroups = entityGroup
                        .Where(d => d.ComponentTypeCode != 1)
                        .GroupBy(d => d.TypeDisplay)
                        .OrderBy(g =>
                        {
                            var idx = Array.IndexOf(EntitySubComponentTypeOrder, g.Key);
                            return idx >= 0 ? idx : int.MaxValue;
                        })
                        .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                    foreach (var typeGroup in subComponentTypeGroups)
                    {
                        var typeGroupKey = entityGroupKey + "::" + typeGroup.Key;

                        rows.Add(new SolutionComponentDisplayRow
                        {
                            IsGroupHeader = true,
                            GroupLevel = 2,
                            GroupKey = typeGroupKey,
                            TopGroupKey = EntitiesTopGroupKey,
                            EntityGroupKey = entityGroupKey,
                            GroupHeaderText = $"{PluralizeTypeDisplay(typeGroup.Key)} ({typeGroup.Count()})"
                        });

                        foreach (var item in typeGroup.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                        {
                            rows.Add(ToMemberRow(item, EntitiesTopGroupKey, entityGroupKey, typeGroupKey));
                        }
                    }
                }
            }

            var typeGroups = typeScoped
                .GroupBy(d => d.TypeDisplay)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in typeGroups)
            {
                rows.Add(new SolutionComponentDisplayRow
                {
                    IsGroupHeader = true,
                    GroupLevel = 0,
                    GroupKey = group.Key,
                    TopGroupKey = group.Key,
                    GroupHeaderText = $"{group.Key} ({group.Count()})"
                });

                foreach (var item in group.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    rows.Add(ToMemberRow(item, group.Key, null, null));
                }
            }

            return rows;
        }

        private static SolutionComponentDisplayRow ToMemberRow(SolutionComponentDetail item, string topGroupKey, string entityGroupKey, string typeGroupKey)
        {
            return new SolutionComponentDisplayRow
            {
                TopGroupKey = topGroupKey,
                EntityGroupKey = entityGroupKey,
                TypeGroupKey = typeGroupKey,
                DisplayName = item.DisplayName,
                Name = item.Name,
                TypeDisplay = item.TypeDisplay,
                State = item.State,
                Customizable = item.Customizable,
                Description = item.Description,
                CreatedOn = item.CreatedOn,
                ModifiedOn = item.ModifiedOn,
                ObjectId = item.ObjectId
            };
        }

        /// <summary>
        /// Renders group header rows as a bold, shaded band spanning the row instead
        /// of six separate cell values, with a ▼/▶ collapse indicator computed live
        /// from _collapsedGroupKeys (so toggling never needs to rebuild the bound
        /// list - only repaint). Nested entity headers get a slightly lighter shade
        /// and an indent to read as "inside" the Entity super-group; member rows
        /// nested under an entity sub-group get the same indent for the same reason.
        /// </summary>
        private void DgvSolutionComponents_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!(dgvSolutionComponents.Rows[e.RowIndex].DataBoundItem is SolutionComponentDisplayRow row)) return;

            if (row.IsGroupHeader)
            {
                e.CellStyle.BackColor = row.GroupLevel == 0 ? SystemColors.ControlDark
                    : row.GroupLevel == 1 ? SystemColors.ControlLight
                    : UiTheme.NestedGroupHeaderBackColor;
                e.CellStyle.Font = new Font(dgvSolutionComponents.Font, FontStyle.Bold);

                if (e.ColumnIndex == 0)
                {
                    var isCollapsed = _collapsedGroupKeys.Contains(row.GroupKey);
                    var indent = row.GroupLevel == 1 ? "     " : row.GroupLevel == 2 ? "          " : string.Empty;
                    e.Value = $"{indent}{(isCollapsed ? "\u25B6" : "\u25BC")} {row.GroupHeaderText}";
                    e.FormattingApplied = true;
                }
                else
                {
                    // Leave e.Value untouched (null) so DateTime? columns don't get
                    // an incompatible string.Empty forced into them.
                    e.FormattingApplied = false;
                }
                return;
            }

            if (e.ColumnIndex == 0 && !string.IsNullOrEmpty(row.EntityGroupKey))
            {
                var indent = !string.IsNullOrEmpty(row.TypeGroupKey) ? "          " : "     ";
                e.Value = indent + e.Value;
                e.FormattingApplied = true;
            }
        }


        /// <summary>
        /// Toggles collapse state when a group header row is clicked, then re-applies
        /// row visibility. Clicking anywhere in a header row toggles it - simplest,
        /// most discoverable interaction for a plain DataGridView (no dedicated
        /// expand/collapse glyph cell to hit precisely).
        /// </summary>
        private void DgvSolutionComponents_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!(dgvSolutionComponents.Rows[e.RowIndex].DataBoundItem is SolutionComponentDisplayRow row) || !row.IsGroupHeader)
                return;

            if (!_collapsedGroupKeys.Add(row.GroupKey))
                _collapsedGroupKeys.Remove(row.GroupKey); // Add() returned false => was already collapsed => this click expands it

            ApplyGroupVisibility();
            dgvSolutionComponents.InvalidateRow(e.RowIndex);
        }

        /// <summary>
        /// Shows/hides every row in dgvSolutionComponents based on _collapsedGroupKeys.
        /// Top-level headers are always visible (collapsing a group hides its
        /// children, never the header itself, so there's always something to click
        /// to re-expand it). Called after every toggle and right after a fresh
        /// roster is bound.
        /// </summary>
        private void ApplyGroupVisibility()
        {
            foreach (DataGridViewRow gridRow in dgvSolutionComponents.Rows)
            {
                if (!(gridRow.DataBoundItem is SolutionComponentDisplayRow row)) continue;

                if (row.IsGroupHeader && row.GroupLevel == 0)
                {
                    gridRow.Visible = true;
                    continue;
                }

                var underCollapsedTop = !string.IsNullOrEmpty(row.TopGroupKey) && _collapsedGroupKeys.Contains(row.TopGroupKey);
                var underCollapsedEntity = !string.IsNullOrEmpty(row.EntityGroupKey) && _collapsedGroupKeys.Contains(row.EntityGroupKey);
                var underCollapsedType = !string.IsNullOrEmpty(row.TypeGroupKey) && _collapsedGroupKeys.Contains(row.TypeGroupKey);
                gridRow.Visible = !underCollapsedTop && !underCollapsedEntity && !underCollapsedType;
            }
        }

        /// <summary>
        /// Populates the Component Details panel from the selected row, or resets it
        /// to placeholders when nothing selectable is highlighted. Group header rows
        /// are skipped entirely (selecting one clears the grid selection instead) -
        /// they carry no component data to show.
        /// </summary>
        private void DgvSolutionComponents_SelectionChanged(object sender, EventArgs e)
        {
            var boundItem = dgvSolutionComponents.CurrentRow?.DataBoundItem as SolutionComponentDisplayRow;

            if (boundItem != null && boundItem.IsGroupHeader)
            {
                dgvSolutionComponents.ClearSelection();
                return;
            }

            if (boundItem == null)
            {
                ResetComponentDetailsPanel();
                return;
            }

            lblDetailNameValue.Text = ValueOrPlaceholder(boundItem.DisplayName);
            lblDetailLogicalNameValue.Text = ValueOrPlaceholder(boundItem.Name);
            lblDetailCreatedOnValue.Text = FormatComponentDate(boundItem.CreatedOn);
            lblDetailModifiedOnValue.Text = FormatComponentDate(boundItem.ModifiedOn);
        }

        private void ResetComponentDetailsPanel()
        {
            lblDetailNameValue.Text = "—";
            lblDetailLogicalNameValue.Text = "—";
            lblDetailCreatedOnValue.Text = "—";
            lblDetailModifiedOnValue.Text = "—";
        }


        private static string ValueOrPlaceholder(string value) => string.IsNullOrEmpty(value) ? "—" : value;

        /// <summary>Same date format already used for solutions' Modified On column (RenderSolutionsPage), for consistency.</summary>
        private static string FormatComponentDate(DateTime? value) =>
            value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "—";

        /// <summary>
        /// Loads (or reloads) the full solution list. Uses genuine async/await via
        /// BrowseSolutionsViewModel.LoadSolutionsAsync rather than WorkAsync, per the
        /// explicit LoadSolutionsAsync() requirement - see the class-level threading
        /// note for why this coexists with WorkAsync elsewhere in this control.
        /// BeginAsyncOperation/EndAsyncOperation are still reused here (Load All
        /// Solutions IS genuinely cancellable - it's a paginated query with many
        /// checkpoints, same as Search), so Abort works for this operation.
        /// </summary>
        private async void BtnLoadAllSolutions_Click(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show(this, "Please connect to an organization first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cancellationToken = BeginAsyncOperation("Loading all solutions...");

            // Progress<T> captures the UI thread's SynchronizationContext at
            // construction time (right now, since we're on the UI thread inside this
            // event handler) - its Report() calls are guaranteed to marshal back to
            // this thread even when invoked from LoadSolutionsAsync's background
            // (Task.Run) thread. This is what lets SearchProgress.Log safely reach
            // AppendLog (a UI control update) without any manual Invoke/BeginInvoke.


            System.IProgress<string> uiProgress = new System.Progress<string>(AppendLog);
            var progress = new SearchProgress(cancellationToken, message => uiProgress.Report(message));

            //try
            //{
            //    await _browseViewModel.LoadSolutionsAsync(progress, cancellationToken);
            //    RenderSolutionsPage();
            //}
            try
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Loading solutions...",
                    Work = (worker, args) =>
                    {
                        var progress = new SearchProgress(
                            cancellationToken,
                            message => worker.ReportProgress(0, message));

                        try
                        {
                            _browseViewModel.LoadSolutionsAsync(progress, cancellationToken)
                                .GetAwaiter()
                                .GetResult();
                        }
                        catch (OperationCanceledException)
                        {
                            args.Cancel = true;
                        }
                    },
                    ProgressChanged = args =>
                    {
                        AppendLog(args.UserState as string);
                    },
                    PostWorkCallBack = args =>
                    {
                        EndAsyncOperation();

                        if (args.Cancelled)
                        {
                            AppendLog("Solution load cancelled.");
                            return;
                        }

                        if (args.Error != null)
                        {
                            AppendLog($"Solution load failed: {args.Error.Message}");

                            MessageBox.Show(
                                this,
                                "Could not load solutions: " + args.Error.Message,
                                "Load failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            LogError(args.Error.ToString());
                            return;
                        }

                        RenderSolutionsPage();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                lblPageIndicator.Text = "Solution load cancelled.";
                AppendLog($"Solution load cancelled after {_operationStopwatch.ElapsedMilliseconds}ms.");
            }
            catch (Exception ex)
            {
                lblPageIndicator.Text = "Failed to load solutions.";
                AppendLog($"Solution load failed after {_operationStopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                MessageBox.Show(this, "Could not load solutions: " + ex.Message, "Load failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError(ex.ToString());
            }
            finally
            {
                EndAsyncOperation();
            }
        }

        /// <summary>Renders the current 20-row page (sorted per BrowseSolutionsViewModel's current sort state).</summary>
        private void RenderSolutionsPage()
        {
            var (pageItems, totalPages, totalCount) = _browseViewModel.GetCurrentPage();

            dgvSolutions.DataSource = pageItems.Select(s => new SolutionListRow
            {
                FriendlyName = s.FriendlyName,
                UniqueName = s.UniqueName,
                Type = s.IsManaged ? "Managed" : "Unmanaged",
                Publisher = s.PublisherName,
                Version = s.Version,
                ModifiedOnDisplay = s.ModifiedOn.HasValue ? s.ModifiedOn.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "—",
                SolutionId = s.SolutionId
            }).ToList();

            UpdateSolutionsSortHeaders();

            lblPageIndicator.Text = totalCount == 0
                ? (string.IsNullOrWhiteSpace(_browseViewModel.SearchText) ? "No solutions found." : $"No solutions match '{_browseViewModel.SearchText}'.")
                : $"Page {_browseViewModel.PageIndex + 1} of {totalPages}  ({totalCount} solution(s), {BrowseSolutionsViewModel.PageSize} per page)";

            btnPrevPage.Enabled = _browseViewModel.PageIndex > 0;
            btnNextPage.Enabled = _browseViewModel.PageIndex < totalPages - 1;

            dgvSolutionComponents.DataSource = null;
            _lastComponentDetails = null;
            _collapsedGroupKeys.Clear();
            ResetComponentDetailsPanel();
            btnExportComponents.Enabled = false;
            lblComponentsHeader.Text = "Select a solution above, then click View Components.";
        }

        /// <summary>Appends a ▲/▼ indicator to whichever column is the active sort column.</summary>
        private void UpdateSolutionsSortHeaders()
        {
            const string upArrow = " \u25B2";
            const string downArrow = " \u25BC";

            dgvSolutions.Columns["colFriendlyName"].HeaderText = "Friendly Name" +
                (_browseViewModel.SortColumn == SolutionSortColumn.Name ? (_browseViewModel.SortDescending ? downArrow : upArrow) : string.Empty);

            dgvSolutions.Columns["colModifiedOn"].HeaderText = "Modified On" +
                (_browseViewModel.SortColumn == SolutionSortColumn.Date ? (_browseViewModel.SortDescending ? downArrow : upArrow) : string.Empty);
        }

        /// <summary>Sort by clicking "Friendly Name" (name sort) or "Modified On" (date sort) column headers; click again to reverse direction.</summary>
        private void DgvSolutions_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            var columnName = dgvSolutions.Columns[e.ColumnIndex].Name;

            if (columnName == "colFriendlyName")
                _browseViewModel.SetSortColumn(SolutionSortColumn.Name);
            else if (columnName == "colModifiedOn")
                _browseViewModel.SetSortColumn(SolutionSortColumn.Date);
            else
                return;

            RenderSolutionsPage();
        }

        private SolutionListRow GetSelectedSolutionRow()
        {
            return dgvSolutions.CurrentRow?.DataBoundItem as SolutionListRow;
        }

        /// <summary>Per-row "Actions" button in the Solutions grid - selects that row, then shows the View Component/Export Solution menu anchored to the clicked cell.</summary>
        private void DgvSolutions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvSolutions.Columns[e.ColumnIndex].Name != SolutionActionsColumnName) return;

            dgvSolutions.CurrentCell = dgvSolutions.Rows[e.RowIndex].Cells[e.ColumnIndex];

            var cellRect = dgvSolutions.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            cmsSolutionActions.Show(dgvSolutions, new Point(cellRect.Left, cellRect.Bottom));
        }

        private void DgvSolutions_SelectionChanged(object sender, EventArgs e)
        {
            // Force a fresh "View Components" click before exporting the component
            // CSV whenever the selected row changes, so that export can never send a
            // stale solution's data.
            btnExportComponents.Enabled = false;
            dgvSolutionComponents.DataSource = null;
            _lastComponentDetails = null;
            _collapsedGroupKeys.Clear();
            ResetComponentDetailsPanel();
            lblComponentsHeader.Text = "Select a solution above, then click View Components.";
        }

        private void BtnViewComponents_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedSolutionRow();
            if (selected == null)
            {
                MessageBox.Show(this, "Select a solution first.", "No solution selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var solutionInfo = _solutionCache.GetById(selected.SolutionId);
            if (solutionInfo == null) return;

            var cancellationToken = BeginAsyncOperation($"Loading components for '{solutionInfo.FriendlyName}'...");

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solution components...",
                Work = (worker, args) =>
                {
                    var progress = new SearchProgress(cancellationToken, message => worker.ReportProgress(0, message));
                    try
                    {
                        args.Result = _solutionExplorerService.ListComponents(solutionInfo, progress);
                    }
                    catch (OperationCanceledException)
                    {
                        args.Cancel = true;
                    }
                },
                ProgressChanged = args => AppendLog(args.UserState as string),
                PostWorkCallBack = args =>
                {
                    EndAsyncOperation();

                    if (args.Cancelled)
                    {
                        lblComponentsHeader.ForeColor = UiTheme.Warning;
                        lblComponentsHeader.Text = "Component load cancelled.";
                        AppendLog($"Component load cancelled after {_operationStopwatch.ElapsedMilliseconds}ms.");
                        return;
                    }

                    if (args.Error != null)
                    {
                        lblComponentsHeader.ForeColor = UiTheme.Error;
                        lblComponentsHeader.Text = "Failed to load components.";
                        AppendLog($"Component load failed after {_operationStopwatch.ElapsedMilliseconds}ms: {args.Error.Message}");
                        MessageBox.Show(this, "Could not load components: " + args.Error.Message, "Load failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogError(args.Error.ToString());
                        return;
                    }

                    _lastComponentDetails = (List<SolutionComponentDetail>)args.Result;
                    _componentsSolutionFriendlyName = solutionInfo.FriendlyName;

                    // _lastComponentDetails itself stays flat and untouched (Export
                    // Components/CSV reads directly from it) - only the grid's
                    // DataSource gets the grouped-with-headers projection, sorted
                    // internally by BuildGroupedComponentRows.
                    _collapsedGroupKeys.Clear(); // every group starts expanded for a freshly loaded roster
                    dgvSolutionComponents.DataSource = BuildGroupedComponentRows(_lastComponentDetails);
                    ApplyGroupVisibility();
                    ResetComponentDetailsPanel();

                    lblComponentsHeader.Text = $"{_lastComponentDetails.Count} component(s) in '{solutionInfo.FriendlyName}'";
                    lblComponentsHeader.ForeColor = UiTheme.TextPrimary;
                    // Export Components reflects data presence only, independent of
                    // selection or grid visibility (see SetAllActionButtonsEnabled).
                    btnExportComponents.Enabled = _lastComponentDetails.Count > 0;

                    // Bring the newly loaded components into view without losing the
                    // Solutions grid's data - a pure layout change (see SetXxxGridCollapsed).
                    // Pinned panels are never auto-collapsed (see AutoCollapseSolutions/AutoCollapseComponents).
                    AutoCollapseComponents(false);
                    AutoCollapseSolutions(true);
                }
            });
        }

        private void BtnExportComponents_Click(object sender, EventArgs e)
        {
            if (_lastComponentDetails == null || _lastComponentDetails.Count == 0)
            {
                MessageBox.Show(this, "Nothing to export yet - click View Components for the selected solution first.",
                    "Nothing to export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = SanitizeFileName(_componentsSolutionFriendlyName) + "_components.csv"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    ExportComponentsToCsv(dialog.FileName, _lastComponentDetails);
                    AppendLog($"Exported {_lastComponentDetails.Count} component row(s) to {dialog.FileName}.");
                    MessageBox.Show(this,
                        $"Exported {_lastComponentDetails.Count} component(s) to:\n{dialog.FileName}",
                        "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not write the export file: " + ex.Message, "Export failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogError(ex.ToString());
                }
            }
        }

        private static void ExportComponentsToCsv(string path, List<SolutionComponentDetail> rows)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine(string.Join(",", new[] { "Display Name", "Name", "Type", "State", "Customizable", "Description" }.Select(CsvEscape)));

                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        row.DisplayName, row.Name, row.TypeDisplay, row.State, row.Customizable, row.Description
                    }.Select(CsvEscape)));
                }
            }
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var needsQuoting = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            var escaped = value.Replace("\"", "\"\"");
            return needsQuoting ? $"\"{escaped}\"" : escaped;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "solution";
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
            return string.IsNullOrEmpty(cleaned) ? "solution" : cleaned;
        }

        // ============================================================
        // Solution export (.zip) - genuine async/await, see class-level note
        // ============================================================

        private async void DgvSolutions_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            await ExportSelectedSolutionAsync();
        }

        /// <summary>
        /// UI orchestration only: gather inputs, show the Save dialog, delegate the
        /// actual export/validation/error-shaping work to BrowseSolutionsViewModel,
        /// and render whatever comes back (success or a specific exception type).
        /// </summary>
        private async Task ExportSelectedSolutionAsync()
        {
            if (Service == null)
            {
                MessageBox.Show(this, "Please connect to an organization first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = GetSelectedSolutionRow();
            if (selected == null)
            {
                MessageBox.Show(this, "Select a solution first.", "No solution selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var solutionInfo = _solutionCache.GetById(selected.SolutionId);
            if (solutionInfo == null) return;

            _browseViewModel.SelectedSolution = solutionInfo;

            var suggestedName = BrowseSolutionsViewModel.BuildSuggestedFileName(solutionInfo);
            var destinationPath = _dialogService.ShowSaveFileDialog(
                this, "Solution package (*.zip)|*.zip|All files (*.*)|*.*", suggestedName, _browseViewModel.LastExportFolder);

            if (string.IsNullOrEmpty(destinationPath)) return; // user cancelled the dialog

            // Deliberately does NOT go through BeginAsyncOperation/the Abort buttons -
            // ExportSolutionRequest can't be meaningfully cancelled once issued (see
            // ISolutionExportService), so an enabled Abort button here would promise
            // something that doesn't actually work.
            SetAllActionButtonsEnabled(false);
            prgExport.Style = ProgressBarStyle.Marquee;
            prgExport.Visible = true;
            lblExportStatus.ForeColor = UiTheme.TextSecondary;
            lblExportStatus.Text = "Preparing export...";
            LogInfo($"Export started: solution='{solutionInfo.UniqueName}', managed={chkExportManaged.Checked}, destination='{destinationPath}'");

            // Same Progress<T> pattern as Load All Solutions: captures this UI
            // thread's context now, so .Report() calls from SolutionExportService's
            // Task.Run background thread land back here safely.
// after
            var uiProgress = new System.Progress<ExportStageProgress>(p => lblExportStatus.Text = p.Message);

            try
            {
                var result = await _browseViewModel.ExportSelectedSolutionAsync(
                    destinationPath, chkExportManaged.Checked, uiProgress, CancellationToken.None);

                var sizeText = FormatFileSize(result.FileSizeBytes);
                lblExportStatus.ForeColor = UiTheme.Success;
                lblExportStatus.Text = $"Completed: {sizeText} in {result.Duration.TotalSeconds:0.0}s";
                LogInfo($"Export finished: '{solutionInfo.UniqueName}' -> {result.FilePath} ({result.FileSizeBytes} bytes) in {result.Duration.TotalMilliseconds:0}ms");

                _dialogService.ShowInfo(this, "Export complete",
                    $"Exported '{solutionInfo.FriendlyName}' to:\n{result.FilePath}\n\nSize: {sizeText}\nTime: {result.Duration.TotalSeconds:0.0}s");
            }
            catch (InvalidOperationException validationEx)
            {
                // Raised by BrowseSolutionsViewModel's own validation (no solution
                // selected, destination folder missing) - not an SDK/IO fault.
                lblExportStatus.ForeColor = UiTheme.Error;
                lblExportStatus.Text = "Export failed: " + validationEx.Message;
                _dialogService.ShowError(this, "Cannot export", validationEx.Message);
                LogWarning("Export validation failed: " + validationEx.Message);
            }
            catch (FaultException<OrganizationServiceFault> faultEx)
            {
                // Server-side Dataverse error - insufficient privileges, the solution
                // no longer exists, etc.
                lblExportStatus.ForeColor = UiTheme.Error;
                lblExportStatus.Text = "Export failed.";
                _dialogService.ShowError(this, "Export failed", "The server rejected the export request: " + faultEx.Message);
                LogError(faultEx.ToString());
            }
            catch (TimeoutException timeoutEx)
            {
                lblExportStatus.ForeColor = UiTheme.Error;
                lblExportStatus.Text = "Export failed: connection timed out.";
                _dialogService.ShowError(this, "Export failed",
                    "The connection timed out while exporting. Check your network connection and try again.");
                LogError(timeoutEx.ToString());
            }
            catch (IOException ioEx)
            {
                // Writing the file failed - disk full, path too long, file locked, etc.
                lblExportStatus.ForeColor = UiTheme.Error;
                lblExportStatus.Text = "Export failed: could not write the file.";
                _dialogService.ShowError(this, "Export failed", "Could not write the export file: " + ioEx.Message);
                LogError(ioEx.ToString());
            }
            catch (Exception ex)
            {
                // Catch-all so a genuinely unexpected error still surfaces as a
                // readable message and gets logged, rather than crashing the plugin.
                lblExportStatus.ForeColor = UiTheme.Error;
                lblExportStatus.Text = "Export failed.";
                _dialogService.ShowError(this, "Export failed", "An unexpected error occurred: " + ex.Message);
                LogError(ex.ToString());
            }
            finally
            {
                prgExport.Style = ProgressBarStyle.Blocks;
                prgExport.Visible = false;
                SetAllActionButtonsEnabled(true);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return $"{size:0.##} {units[unitIndex]}";
        }

        // ============================================================
        // Shared: async operation lifecycle, logging, cancellation, URLs
        // ============================================================

        /// <summary>Enables/disables every action button across both tabs at once - the single place that list is maintained.</summary>
        private void SetAllActionButtonsEnabled(bool enabled)
        {
            btnSearch.Enabled = enabled;
            btnLoadAllSolutions.Enabled = enabled;
            dgvSolutions.Enabled = enabled;
            // Reflects data presence only (see BtnViewComponents_Click/RenderSolutionsPage/
            // DgvSolutions_SelectionChanged) - never selection or grid visibility.
            btnExportComponents.Enabled = enabled && _lastComponentDetails != null && _lastComponentDetails.Count > 0;
            chkExportManaged.Enabled = enabled;
        }

        /// <summary>
        /// Common setup for a cancellable, logged WorkAsync operation (search, load
        /// solutions, or view components): fresh cancellation token, fresh stopwatch,
        /// cleared log, every action button locked, both tabs' Abort buttons enabled.
        /// </summary>
        private CancellationToken BeginAsyncOperation(string initialLogMessage)
        {
            _operationCancellation?.Dispose();
            _operationCancellation = new CancellationTokenSource();
            _operationStopwatch = Stopwatch.StartNew();

            lstLog.Items.Clear();
            AppendLog(initialLogMessage);

            SetAllActionButtonsEnabled(false);
            btnAbort.Enabled = true;
            btnAbortBrowse.Enabled = true;

            return _operationCancellation.Token;
        }

        private void EndAsyncOperation()
        {
            SetAllActionButtonsEnabled(true);
            btnAbort.Enabled = false;
            btnAbortBrowse.Enabled = false;
        }

        /// <summary>
        /// Requests cancellation of whichever WorkAsync operation is running, from
        /// either tab's Abort button. Because IOrganizationService calls are
        /// synchronous, this can't abort a request already in flight - it stops the
        /// operation from issuing its NEXT server call. No partial results are
        /// cached: SolutionCache/MetadataResolver only ever commit a cache update
        /// after a full response is retrieved.
        /// </summary>
        private void BtnAbort_Click(object sender, EventArgs e)
        {
            if (_operationCancellation == null || _operationCancellation.IsCancellationRequested) return;

            btnAbort.Enabled = false;
            btnAbortBrowse.Enabled = false;
            AppendLog("Abort requested — stopping after the current operation completes...");
            _operationCancellation.Cancel();
        }

        private void AppendLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var elapsed = _operationStopwatch?.Elapsed ?? TimeSpan.Zero;
            var stamp = elapsed.ToString(@"mm\:ss\.fff");
            lstLog.Items.Add($"[{stamp}] {message}");
            lstLog.TopIndex = Math.Max(0, lstLog.Items.Count - 1);
        }

        /// <summary>
        /// Online: Maker Portal URL, which takes the solution's GUID (confirmed
        /// against Microsoft's documentation - the unique name does not work here).
        /// On-Premises: the classic solution editor
        /// {orgUrl}/tools/solution/edit.aspx?id={solutionId}, GUID in curly braces.
        /// Returns null when there isn't enough connection info to build either.
        /// </summary>
        private string BuildSolutionUrl(Guid solutionId)
        {
            if (ConnectionDetail == null) return null;

            if (ConnectionDetail.UseOnline && !string.IsNullOrEmpty(ConnectionDetail.EnvironmentId))
            {
                return $"https://make.powerapps.com/environments/{ConnectionDetail.EnvironmentId}/solutions/{solutionId}";
            }

            var webAppUrl = ConnectionDetail.WebApplicationUrl?.TrimEnd('/');
            if (string.IsNullOrEmpty(webAppUrl)) return null;

            return $"{webAppUrl}/tools/solution/edit.aspx?id={{{solutionId}}}";
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show(this, "No solution link is available for this connection.", "Not available",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Process.Start(url);
        }
    }
}
