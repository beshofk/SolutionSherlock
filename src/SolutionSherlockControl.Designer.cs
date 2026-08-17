namespace BeshoyFanous.XrmToolBox.SolutionSherlock
{
    partial class SolutionSherlockControl
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        // Shared, outside the tabs
        private System.Windows.Forms.Panel pnlConnectionStatus;
        private System.Windows.Forms.Label lblConnectionInfo;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlLog;
        private System.Windows.Forms.Label lblLogHeader;
        private System.Windows.Forms.ListBox lstLog;

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabSearch;
        private System.Windows.Forms.TabPage tabBrowse;

        // ---- tabSearch ----
        private System.Windows.Forms.Panel pnlSearchRoot;
        private System.Windows.Forms.GroupBox grpSearchCriteria;
        private System.Windows.Forms.TableLayoutPanel tlpSearchCriteria;
        private System.Windows.Forms.Label lblMainType;
        private System.Windows.Forms.ComboBox cboMainType;
        private System.Windows.Forms.Label lblMainSearch;
        private System.Windows.Forms.TextBox txtMainSearch;
        private System.Windows.Forms.Label lblSubType;
        private System.Windows.Forms.ComboBox cboSubType;
        private System.Windows.Forms.Label lblSubSearch;
        private System.Windows.Forms.TextBox txtSubSearch;
        private System.Windows.Forms.CheckBox chkManagedOnly;
        private System.Windows.Forms.CheckBox chkUnmanagedOnly;
        private System.Windows.Forms.FlowLayoutPanel flpSearchButtons;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.GroupBox grpSearchResults;
        private System.Windows.Forms.Panel pnlSearchStatus;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.DataGridView dgvResults;

        private System.Windows.Forms.ContextMenuStrip cmsResults;
        private System.Windows.Forms.ToolStripMenuItem miCopySolutionName;
        private System.Windows.Forms.ToolStripMenuItem miCopyComponentName;
        private System.Windows.Forms.ToolStripMenuItem miOpenInMaker;

        // ---- tabBrowse ----
        private System.Windows.Forms.Panel pnlBrowseRoot;
        private System.Windows.Forms.FlowLayoutPanel pnlDockTabStrip;
        private System.Windows.Forms.Label lblSolutionsCollapsedTab;
        private System.Windows.Forms.Label lblComponentsCollapsedTab;
        private System.Windows.Forms.ToolStrip tsBrowseActions;
        private System.Windows.Forms.ToolStripButton btnLoadAllSolutions;
        private System.Windows.Forms.ToolStripButton btnAbortBrowse;
        private System.Windows.Forms.ContextMenuStrip cmsSolutionActions;
        private System.Windows.Forms.ToolStripMenuItem miActionsViewComponents;
        private System.Windows.Forms.ToolStripMenuItem miActionsOpenInBrowser;
        private System.Windows.Forms.ToolStripMenuItem miActionsExportSolution;
        private System.Windows.Forms.Panel pnlExportOptions;
        private System.Windows.Forms.TableLayoutPanel tlpExportOptions;
        private System.Windows.Forms.CheckBox chkExportManaged;
        private System.Windows.Forms.ProgressBar prgExport;
        private System.Windows.Forms.Label lblExportStatus;

        private System.Windows.Forms.SplitContainer splitBrowseTop;

        private System.Windows.Forms.GroupBox grpSolutions;
        private System.Windows.Forms.Panel pnlSolutionsDockHeader;
        private System.Windows.Forms.Panel pnlSolutionsHeaderAccent;
        private System.Windows.Forms.Label lblSolutionsDockTitle;
        private System.Windows.Forms.Button btnSolutionsPinToggle;
        private System.Windows.Forms.Button btnSolutionsCollapseToggle;
        private System.Windows.Forms.Panel pnlSolutionsSearch;
        private System.Windows.Forms.Label lblSolutionsSearch;
        private System.Windows.Forms.TextBox txtSolutionSearch;
        private System.Windows.Forms.DataGridView dgvSolutions;
        private System.Windows.Forms.Panel pnlPaging;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Label lblPageIndicator;
        private System.Windows.Forms.Button btnNextPage;

        private System.Windows.Forms.GroupBox grpComponents;
        private System.Windows.Forms.Panel pnlComponentsDockHeader;
        private System.Windows.Forms.Panel pnlComponentsHeaderAccent;
        private System.Windows.Forms.Label lblComponentsDockTitle;
        private System.Windows.Forms.Button btnComponentsPinToggle;
        private System.Windows.Forms.Button btnComponentsCollapseToggle;
        private System.Windows.Forms.Panel pnlComponentsHeader;
        private System.Windows.Forms.Label lblComponentsHeader;
        private System.Windows.Forms.DataGridView dgvSolutionComponents;

        // Component Details panel - populated when a row in dgvSolutionComponents is selected.
        private System.Windows.Forms.GroupBox grpComponentDetails;
        private System.Windows.Forms.Panel pnlComponentDetailsActions;
        private System.Windows.Forms.Button btnExportComponents;
        private System.Windows.Forms.TableLayoutPanel tlpComponentDetails;
        private System.Windows.Forms.Label lblDetailNameCaption;
        private System.Windows.Forms.Label lblDetailNameValue;
        private System.Windows.Forms.Label lblDetailLogicalNameCaption;
        private System.Windows.Forms.Label lblDetailLogicalNameValue;
        private System.Windows.Forms.Label lblDetailCreatedOnCaption;
        private System.Windows.Forms.Label lblDetailCreatedOnValue;
        private System.Windows.Forms.Label lblDetailModifiedOnCaption;
        private System.Windows.Forms.Label lblDetailModifiedOnValue;

        /// <summary>
        /// Required method for Designer support - do not modify the contents of this
        /// method with the code editor. This is hand-authored to the same shape the
        /// Windows Forms Designer would produce; opening this control in Designer view
        /// loads it normally, and further visual edits are written back here as usual.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlConnectionStatus = new System.Windows.Forms.Panel();
            this.lblConnectionInfo = new System.Windows.Forms.Label();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabBrowse = new System.Windows.Forms.TabPage();
            this.pnlBrowseRoot = new System.Windows.Forms.Panel();
            this.splitBrowseTop = new System.Windows.Forms.SplitContainer();
            this.grpSolutions = new System.Windows.Forms.GroupBox();
            this.dgvSolutions = new System.Windows.Forms.DataGridView();
            this.pnlPaging = new System.Windows.Forms.Panel();
            this.btnNextPage = new System.Windows.Forms.Button();
            this.lblPageIndicator = new System.Windows.Forms.Label();
            this.btnPrevPage = new System.Windows.Forms.Button();
            this.pnlSolutionsSearch = new System.Windows.Forms.Panel();
            this.txtSolutionSearch = new System.Windows.Forms.TextBox();
            this.lblSolutionsSearch = new System.Windows.Forms.Label();
            this.pnlSolutionsDockHeader = new System.Windows.Forms.Panel();
            this.lblSolutionsDockTitle = new System.Windows.Forms.Label();
            this.btnSolutionsPinToggle = new System.Windows.Forms.Button();
            this.btnSolutionsCollapseToggle = new System.Windows.Forms.Button();
            this.pnlSolutionsHeaderAccent = new System.Windows.Forms.Panel();
            this.grpComponents = new System.Windows.Forms.GroupBox();
            this.dgvSolutionComponents = new System.Windows.Forms.DataGridView();
            this.pnlComponentsHeader = new System.Windows.Forms.Panel();
            this.lblComponentsHeader = new System.Windows.Forms.Label();
            this.grpComponentDetails = new System.Windows.Forms.GroupBox();
            this.tlpComponentDetails = new System.Windows.Forms.TableLayoutPanel();
            this.lblDetailNameCaption = new System.Windows.Forms.Label();
            this.lblDetailNameValue = new System.Windows.Forms.Label();
            this.lblDetailLogicalNameCaption = new System.Windows.Forms.Label();
            this.lblDetailLogicalNameValue = new System.Windows.Forms.Label();
            this.lblDetailCreatedOnCaption = new System.Windows.Forms.Label();
            this.lblDetailCreatedOnValue = new System.Windows.Forms.Label();
            this.lblDetailModifiedOnCaption = new System.Windows.Forms.Label();
            this.lblDetailModifiedOnValue = new System.Windows.Forms.Label();
            this.pnlComponentDetailsActions = new System.Windows.Forms.Panel();
            this.btnExportComponents = new System.Windows.Forms.Button();
            this.pnlComponentsDockHeader = new System.Windows.Forms.Panel();
            this.lblComponentsDockTitle = new System.Windows.Forms.Label();
            this.btnComponentsPinToggle = new System.Windows.Forms.Button();
            this.btnComponentsCollapseToggle = new System.Windows.Forms.Button();
            this.pnlComponentsHeaderAccent = new System.Windows.Forms.Panel();
            this.pnlDockTabStrip = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSolutionsCollapsedTab = new System.Windows.Forms.Label();
            this.lblComponentsCollapsedTab = new System.Windows.Forms.Label();
            this.pnlExportOptions = new System.Windows.Forms.Panel();
            this.tlpExportOptions = new System.Windows.Forms.TableLayoutPanel();
            this.chkExportManaged = new System.Windows.Forms.CheckBox();
            this.prgExport = new System.Windows.Forms.ProgressBar();
            this.lblExportStatus = new System.Windows.Forms.Label();
            this.tsBrowseActions = new System.Windows.Forms.ToolStrip();
            this.btnLoadAllSolutions = new System.Windows.Forms.ToolStripButton();
            this.btnAbortBrowse = new System.Windows.Forms.ToolStripButton();
            this.tabSearch = new System.Windows.Forms.TabPage();
            this.pnlSearchRoot = new System.Windows.Forms.Panel();
            this.grpSearchResults = new System.Windows.Forms.GroupBox();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.cmsResults = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miCopySolutionName = new System.Windows.Forms.ToolStripMenuItem();
            this.miCopyComponentName = new System.Windows.Forms.ToolStripMenuItem();
            this.miOpenInMaker = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlSearchStatus = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.grpSearchCriteria = new System.Windows.Forms.GroupBox();
            this.tlpSearchCriteria = new System.Windows.Forms.TableLayoutPanel();
            this.lblMainType = new System.Windows.Forms.Label();
            this.cboMainType = new System.Windows.Forms.ComboBox();
            this.lblMainSearch = new System.Windows.Forms.Label();
            this.txtMainSearch = new System.Windows.Forms.TextBox();
            this.lblSubType = new System.Windows.Forms.Label();
            this.cboSubType = new System.Windows.Forms.ComboBox();
            this.lblSubSearch = new System.Windows.Forms.Label();
            this.txtSubSearch = new System.Windows.Forms.TextBox();
            this.chkManagedOnly = new System.Windows.Forms.CheckBox();
            this.flpSearchButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnAbort = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.chkUnmanagedOnly = new System.Windows.Forms.CheckBox();
            this.pnlLog = new System.Windows.Forms.Panel();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.lblLogHeader = new System.Windows.Forms.Label();
            this.cmsSolutionActions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miActionsViewComponents = new System.Windows.Forms.ToolStripMenuItem();
            this.miActionsOpenInBrowser = new System.Windows.Forms.ToolStripMenuItem();
            this.miActionsExportSolution = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlConnectionStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabBrowse.SuspendLayout();
            this.pnlBrowseRoot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitBrowseTop)).BeginInit();
            this.splitBrowseTop.Panel1.SuspendLayout();
            this.splitBrowseTop.Panel2.SuspendLayout();
            this.splitBrowseTop.SuspendLayout();
            this.grpSolutions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolutions)).BeginInit();
            this.pnlPaging.SuspendLayout();
            this.pnlSolutionsSearch.SuspendLayout();
            this.pnlSolutionsDockHeader.SuspendLayout();
            this.grpComponents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolutionComponents)).BeginInit();
            this.pnlComponentsHeader.SuspendLayout();
            this.grpComponentDetails.SuspendLayout();
            this.tlpComponentDetails.SuspendLayout();
            this.pnlComponentDetailsActions.SuspendLayout();
            this.pnlComponentsDockHeader.SuspendLayout();
            this.pnlDockTabStrip.SuspendLayout();
            this.pnlExportOptions.SuspendLayout();
            this.tlpExportOptions.SuspendLayout();
            this.tsBrowseActions.SuspendLayout();
            this.tabSearch.SuspendLayout();
            this.pnlSearchRoot.SuspendLayout();
            this.grpSearchResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.cmsResults.SuspendLayout();
            this.pnlSearchStatus.SuspendLayout();
            this.grpSearchCriteria.SuspendLayout();
            this.tlpSearchCriteria.SuspendLayout();
            this.flpSearchButtons.SuspendLayout();
            this.pnlLog.SuspendLayout();
            this.cmsSolutionActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlConnectionStatus
            // 
            this.pnlConnectionStatus.Controls.Add(this.lblConnectionInfo);
            this.pnlConnectionStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlConnectionStatus.Location = new System.Drawing.Point(0, 0);
            this.pnlConnectionStatus.Name = "pnlConnectionStatus";
            this.pnlConnectionStatus.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.pnlConnectionStatus.Size = new System.Drawing.Size(1000, 26);
            this.pnlConnectionStatus.TabIndex = 0;
            // 
            // lblConnectionInfo
            // 
            this.lblConnectionInfo.AutoSize = true;
            this.lblConnectionInfo.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblConnectionInfo.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblConnectionInfo.Location = new System.Drawing.Point(12, 6);
            this.lblConnectionInfo.Name = "lblConnectionInfo";
            this.lblConnectionInfo.Size = new System.Drawing.Size(0, 15);
            this.lblConnectionInfo.TabIndex = 0;
            this.lblConnectionInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitMain.Location = new System.Drawing.Point(0, 26);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.tabMain);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.pnlLog);
            this.splitMain.Panel2MinSize = 70;
            this.splitMain.Size = new System.Drawing.Size(1000, 734);
            this.splitMain.SplitterDistance = 610;
            this.splitMain.SplitterWidth = 6;
            this.splitMain.TabIndex = 1;
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabBrowse);
            this.tabMain.Controls.Add(this.tabSearch);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.Padding = new System.Drawing.Point(12, 6);
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1000, 610);
            this.tabMain.TabIndex = 0;
            // 
            // tabBrowse
            // 
            this.tabBrowse.Controls.Add(this.pnlBrowseRoot);
            this.tabBrowse.Location = new System.Drawing.Point(4, 30);
            this.tabBrowse.Name = "tabBrowse";
            this.tabBrowse.Size = new System.Drawing.Size(992, 576);
            this.tabBrowse.TabIndex = 1;
            this.tabBrowse.Text = "Browse Solutions";
            this.tabBrowse.UseVisualStyleBackColor = true;
            // 
            // pnlBrowseRoot
            // 
            this.pnlBrowseRoot.Controls.Add(this.splitBrowseTop);
            this.pnlBrowseRoot.Controls.Add(this.pnlDockTabStrip);
            this.pnlBrowseRoot.Controls.Add(this.pnlExportOptions);
            this.pnlBrowseRoot.Controls.Add(this.tsBrowseActions);
            this.pnlBrowseRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBrowseRoot.Location = new System.Drawing.Point(0, 0);
            this.pnlBrowseRoot.Name = "pnlBrowseRoot";
            this.pnlBrowseRoot.Padding = new System.Windows.Forms.Padding(10);
            this.pnlBrowseRoot.Size = new System.Drawing.Size(992, 576);
            this.pnlBrowseRoot.TabIndex = 0;
            // 
            // splitBrowseTop
            // 
            this.splitBrowseTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitBrowseTop.Location = new System.Drawing.Point(10, 111);
            this.splitBrowseTop.Name = "splitBrowseTop";
            // 
            // splitBrowseTop.Panel1
            // 
            this.splitBrowseTop.Panel1.Controls.Add(this.grpSolutions);
            this.splitBrowseTop.Panel1MinSize = 220;
            // 
            // splitBrowseTop.Panel2
            // 
            this.splitBrowseTop.Panel2.Controls.Add(this.grpComponents);
            this.splitBrowseTop.Panel2MinSize = 260;
            this.splitBrowseTop.Size = new System.Drawing.Size(972, 455);
            this.splitBrowseTop.SplitterDistance = 452;
            this.splitBrowseTop.SplitterWidth = 6;
            this.splitBrowseTop.TabIndex = 2;
            // 
            // grpSolutions
            // 
            this.grpSolutions.Controls.Add(this.dgvSolutions);
            this.grpSolutions.Controls.Add(this.pnlPaging);
            this.grpSolutions.Controls.Add(this.pnlSolutionsSearch);
            this.grpSolutions.Controls.Add(this.pnlSolutionsDockHeader);
            this.grpSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSolutions.Location = new System.Drawing.Point(0, 0);
            this.grpSolutions.Name = "grpSolutions";
            this.grpSolutions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpSolutions.Size = new System.Drawing.Size(452, 455);
            this.grpSolutions.TabIndex = 0;
            this.grpSolutions.TabStop = false;
            // 
            // dgvSolutions
            // 
            this.dgvSolutions.AllowUserToAddRows = false;
            this.dgvSolutions.AllowUserToDeleteRows = false;
            this.dgvSolutions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvSolutions.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvSolutions.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvSolutions.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvSolutions.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(247)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.dgvSolutions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvSolutions.ColumnHeadersHeight = 30;
            this.dgvSolutions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvSolutions.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSolutions.EnableHeadersVisualStyles = false;
            this.dgvSolutions.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.dgvSolutions.Location = new System.Drawing.Point(0, 80);
            this.dgvSolutions.MultiSelect = false;
            this.dgvSolutions.Name = "dgvSolutions";
            this.dgvSolutions.ReadOnly = true;
            this.dgvSolutions.RowHeadersVisible = false;
            this.dgvSolutions.RowTemplate.Height = 24;
            this.dgvSolutions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSolutions.Size = new System.Drawing.Size(452, 329);
            this.dgvSolutions.TabIndex = 0;
            // 
            // pnlPaging
            // 
            this.pnlPaging.Controls.Add(this.btnNextPage);
            this.pnlPaging.Controls.Add(this.lblPageIndicator);
            this.pnlPaging.Controls.Add(this.btnPrevPage);
            this.pnlPaging.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlPaging.Location = new System.Drawing.Point(0, 409);
            this.pnlPaging.Name = "pnlPaging";
            this.pnlPaging.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.pnlPaging.Size = new System.Drawing.Size(452, 38);
            this.pnlPaging.TabIndex = 1;
            // 
            // btnNextPage
            // 
            this.btnNextPage.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnNextPage.Enabled = false;
            this.btnNextPage.Location = new System.Drawing.Point(377, 6);
            this.btnNextPage.Name = "btnNextPage";
            this.btnNextPage.Size = new System.Drawing.Size(75, 32);
            this.btnNextPage.TabIndex = 2;
            this.btnNextPage.Text = "Next ▶";
            this.btnNextPage.UseVisualStyleBackColor = true;
            // 
            // lblPageIndicator
            // 
            this.lblPageIndicator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageIndicator.Location = new System.Drawing.Point(75, 6);
            this.lblPageIndicator.Name = "lblPageIndicator";
            this.lblPageIndicator.Size = new System.Drawing.Size(377, 32);
            this.lblPageIndicator.TabIndex = 1;
            this.lblPageIndicator.Text = "No solutions loaded yet — click Load All Solutions.";
            this.lblPageIndicator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnPrevPage
            // 
            this.btnPrevPage.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnPrevPage.Enabled = false;
            this.btnPrevPage.Location = new System.Drawing.Point(0, 6);
            this.btnPrevPage.Name = "btnPrevPage";
            this.btnPrevPage.Size = new System.Drawing.Size(75, 32);
            this.btnPrevPage.TabIndex = 0;
            this.btnPrevPage.Text = "◀ Prev";
            this.btnPrevPage.UseVisualStyleBackColor = true;
            // 
            // pnlSolutionsSearch
            // 
            this.pnlSolutionsSearch.Controls.Add(this.txtSolutionSearch);
            this.pnlSolutionsSearch.Controls.Add(this.lblSolutionsSearch);
            this.pnlSolutionsSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSolutionsSearch.Location = new System.Drawing.Point(0, 46);
            this.pnlSolutionsSearch.Name = "pnlSolutionsSearch";
            this.pnlSolutionsSearch.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlSolutionsSearch.Size = new System.Drawing.Size(452, 34);
            this.pnlSolutionsSearch.TabIndex = 2;
            // 
            // txtSolutionSearch
            // 
            this.txtSolutionSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSolutionSearch.Location = new System.Drawing.Point(45, 0);
            this.txtSolutionSearch.Name = "txtSolutionSearch";
            this.txtSolutionSearch.Size = new System.Drawing.Size(407, 23);
            this.txtSolutionSearch.TabIndex = 1;
            // 
            // lblSolutionsSearch
            // 
            this.lblSolutionsSearch.AutoSize = true;
            this.lblSolutionsSearch.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblSolutionsSearch.Location = new System.Drawing.Point(0, 0);
            this.lblSolutionsSearch.Name = "lblSolutionsSearch";
            this.lblSolutionsSearch.Size = new System.Drawing.Size(45, 15);
            this.lblSolutionsSearch.TabIndex = 0;
            this.lblSolutionsSearch.Text = "Search:";
            this.lblSolutionsSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlSolutionsDockHeader
            // 
            this.pnlSolutionsDockHeader.Controls.Add(this.lblSolutionsDockTitle);
            this.pnlSolutionsDockHeader.Controls.Add(this.btnSolutionsPinToggle);
            this.pnlSolutionsDockHeader.Controls.Add(this.btnSolutionsCollapseToggle);
            this.pnlSolutionsDockHeader.Controls.Add(this.pnlSolutionsHeaderAccent);
            this.pnlSolutionsDockHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSolutionsDockHeader.Location = new System.Drawing.Point(0, 16);
            this.pnlSolutionsDockHeader.Name = "pnlSolutionsDockHeader";
            this.pnlSolutionsDockHeader.Size = new System.Drawing.Size(452, 30);
            this.pnlSolutionsDockHeader.TabIndex = 3;
            // 
            // lblSolutionsDockTitle
            // 
            this.lblSolutionsDockTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSolutionsDockTitle.Location = new System.Drawing.Point(3, 0);
            this.lblSolutionsDockTitle.Name = "lblSolutionsDockTitle";
            this.lblSolutionsDockTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSolutionsDockTitle.Size = new System.Drawing.Size(393, 30);
            this.lblSolutionsDockTitle.TabIndex = 1;
            this.lblSolutionsDockTitle.Text = "Solutions";
            this.lblSolutionsDockTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnSolutionsPinToggle
            // 
            this.btnSolutionsPinToggle.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSolutionsPinToggle.Location = new System.Drawing.Point(396, 0);
            this.btnSolutionsPinToggle.Name = "btnSolutionsPinToggle";
            this.btnSolutionsPinToggle.Size = new System.Drawing.Size(28, 30);
            this.btnSolutionsPinToggle.TabIndex = 2;
            this.btnSolutionsPinToggle.Text = "📌";
            this.btnSolutionsPinToggle.UseVisualStyleBackColor = true;
            // 
            // btnSolutionsCollapseToggle
            // 
            this.btnSolutionsCollapseToggle.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSolutionsCollapseToggle.Location = new System.Drawing.Point(424, 0);
            this.btnSolutionsCollapseToggle.Name = "btnSolutionsCollapseToggle";
            this.btnSolutionsCollapseToggle.Size = new System.Drawing.Size(28, 30);
            this.btnSolutionsCollapseToggle.TabIndex = 3;
            this.btnSolutionsCollapseToggle.Text = "◀";
            this.btnSolutionsCollapseToggle.UseVisualStyleBackColor = true;
            // 
            // pnlSolutionsHeaderAccent
            // 
            this.pnlSolutionsHeaderAccent.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSolutionsHeaderAccent.Location = new System.Drawing.Point(0, 0);
            this.pnlSolutionsHeaderAccent.Name = "pnlSolutionsHeaderAccent";
            this.pnlSolutionsHeaderAccent.Size = new System.Drawing.Size(3, 30);
            this.pnlSolutionsHeaderAccent.TabIndex = 0;
            // 
            // grpComponents
            // 
            this.grpComponents.Controls.Add(this.dgvSolutionComponents);
            this.grpComponents.Controls.Add(this.pnlComponentsHeader);
            this.grpComponents.Controls.Add(this.grpComponentDetails);
            this.grpComponents.Controls.Add(this.pnlComponentsDockHeader);
            this.grpComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpComponents.Location = new System.Drawing.Point(0, 0);
            this.grpComponents.Name = "grpComponents";
            this.grpComponents.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpComponents.Size = new System.Drawing.Size(514, 455);
            this.grpComponents.TabIndex = 0;
            this.grpComponents.TabStop = false;
            // 
            // dgvSolutionComponents
            // 
            this.dgvSolutionComponents.AllowUserToAddRows = false;
            this.dgvSolutionComponents.AllowUserToDeleteRows = false;
            this.dgvSolutionComponents.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvSolutionComponents.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvSolutionComponents.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvSolutionComponents.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvSolutionComponents.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(247)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.dgvSolutionComponents.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvSolutionComponents.ColumnHeadersHeight = 30;
            this.dgvSolutionComponents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvSolutionComponents.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvSolutionComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSolutionComponents.EnableHeadersVisualStyles = false;
            this.dgvSolutionComponents.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.dgvSolutionComponents.Location = new System.Drawing.Point(0, 76);
            this.dgvSolutionComponents.MultiSelect = false;
            this.dgvSolutionComponents.Name = "dgvSolutionComponents";
            this.dgvSolutionComponents.ReadOnly = true;
            this.dgvSolutionComponents.RowHeadersVisible = false;
            this.dgvSolutionComponents.RowTemplate.Height = 24;
            this.dgvSolutionComponents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSolutionComponents.Size = new System.Drawing.Size(514, 251);
            this.dgvSolutionComponents.TabIndex = 1;
            // 
            // pnlComponentsHeader
            // 
            this.pnlComponentsHeader.Controls.Add(this.lblComponentsHeader);
            this.pnlComponentsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlComponentsHeader.Location = new System.Drawing.Point(0, 46);
            this.pnlComponentsHeader.Name = "pnlComponentsHeader";
            this.pnlComponentsHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.pnlComponentsHeader.Size = new System.Drawing.Size(514, 30);
            this.pnlComponentsHeader.TabIndex = 0;
            // 
            // lblComponentsHeader
            // 
            this.lblComponentsHeader.AutoSize = true;
            this.lblComponentsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblComponentsHeader.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblComponentsHeader.Location = new System.Drawing.Point(0, 0);
            this.lblComponentsHeader.Name = "lblComponentsHeader";
            this.lblComponentsHeader.Size = new System.Drawing.Size(253, 15);
            this.lblComponentsHeader.TabIndex = 0;
            this.lblComponentsHeader.Text = "Select a solution, then click View Components.";
            // 
            // grpComponentDetails
            // 
            this.grpComponentDetails.Controls.Add(this.tlpComponentDetails);
            this.grpComponentDetails.Controls.Add(this.pnlComponentDetailsActions);
            this.grpComponentDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpComponentDetails.Location = new System.Drawing.Point(0, 327);
            this.grpComponentDetails.Name = "grpComponentDetails";
            this.grpComponentDetails.Padding = new System.Windows.Forms.Padding(10, 6, 10, 8);
            this.grpComponentDetails.Size = new System.Drawing.Size(514, 120);
            this.grpComponentDetails.TabIndex = 2;
            this.grpComponentDetails.TabStop = false;
            this.grpComponentDetails.Text = "Component Details";
            // 
            // tlpComponentDetails
            // 
            this.tlpComponentDetails.ColumnCount = 4;
            this.tlpComponentDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpComponentDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpComponentDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpComponentDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpComponentDetails.Controls.Add(this.lblDetailNameCaption, 0, 0);
            this.tlpComponentDetails.Controls.Add(this.lblDetailNameValue, 1, 0);
            this.tlpComponentDetails.Controls.Add(this.lblDetailLogicalNameCaption, 2, 0);
            this.tlpComponentDetails.Controls.Add(this.lblDetailLogicalNameValue, 3, 0);
            this.tlpComponentDetails.Controls.Add(this.lblDetailCreatedOnCaption, 0, 1);
            this.tlpComponentDetails.Controls.Add(this.lblDetailCreatedOnValue, 1, 1);
            this.tlpComponentDetails.Controls.Add(this.lblDetailModifiedOnCaption, 2, 1);
            this.tlpComponentDetails.Controls.Add(this.lblDetailModifiedOnValue, 3, 1);
            this.tlpComponentDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpComponentDetails.Location = new System.Drawing.Point(10, 58);
            this.tlpComponentDetails.Name = "tlpComponentDetails";
            this.tlpComponentDetails.RowCount = 2;
            this.tlpComponentDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tlpComponentDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tlpComponentDetails.Size = new System.Drawing.Size(494, 54);
            this.tlpComponentDetails.TabIndex = 0;
            // 
            // lblDetailNameCaption
            // 
            this.lblDetailNameCaption.AutoSize = true;
            this.lblDetailNameCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailNameCaption.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblDetailNameCaption.Location = new System.Drawing.Point(3, 0);
            this.lblDetailNameCaption.Name = "lblDetailNameCaption";
            this.lblDetailNameCaption.Size = new System.Drawing.Size(104, 26);
            this.lblDetailNameCaption.TabIndex = 0;
            this.lblDetailNameCaption.Text = "Name:";
            this.lblDetailNameCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailNameValue
            // 
            this.lblDetailNameValue.AutoSize = true;
            this.lblDetailNameValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailNameValue.Location = new System.Drawing.Point(113, 0);
            this.lblDetailNameValue.Name = "lblDetailNameValue";
            this.lblDetailNameValue.Size = new System.Drawing.Size(131, 26);
            this.lblDetailNameValue.TabIndex = 1;
            this.lblDetailNameValue.Text = "—";
            this.lblDetailNameValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailLogicalNameCaption
            // 
            this.lblDetailLogicalNameCaption.AutoSize = true;
            this.lblDetailLogicalNameCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailLogicalNameCaption.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblDetailLogicalNameCaption.Location = new System.Drawing.Point(250, 0);
            this.lblDetailLogicalNameCaption.Name = "lblDetailLogicalNameCaption";
            this.lblDetailLogicalNameCaption.Size = new System.Drawing.Size(104, 26);
            this.lblDetailLogicalNameCaption.TabIndex = 2;
            this.lblDetailLogicalNameCaption.Text = "Logical Name:";
            this.lblDetailLogicalNameCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailLogicalNameValue
            // 
            this.lblDetailLogicalNameValue.AutoSize = true;
            this.lblDetailLogicalNameValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailLogicalNameValue.Location = new System.Drawing.Point(360, 0);
            this.lblDetailLogicalNameValue.Name = "lblDetailLogicalNameValue";
            this.lblDetailLogicalNameValue.Size = new System.Drawing.Size(131, 26);
            this.lblDetailLogicalNameValue.TabIndex = 3;
            this.lblDetailLogicalNameValue.Text = "—";
            this.lblDetailLogicalNameValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailCreatedOnCaption
            // 
            this.lblDetailCreatedOnCaption.AutoSize = true;
            this.lblDetailCreatedOnCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailCreatedOnCaption.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblDetailCreatedOnCaption.Location = new System.Drawing.Point(3, 26);
            this.lblDetailCreatedOnCaption.Name = "lblDetailCreatedOnCaption";
            this.lblDetailCreatedOnCaption.Size = new System.Drawing.Size(104, 28);
            this.lblDetailCreatedOnCaption.TabIndex = 4;
            this.lblDetailCreatedOnCaption.Text = "Created On:";
            this.lblDetailCreatedOnCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailCreatedOnValue
            // 
            this.lblDetailCreatedOnValue.AutoSize = true;
            this.lblDetailCreatedOnValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailCreatedOnValue.Location = new System.Drawing.Point(113, 26);
            this.lblDetailCreatedOnValue.Name = "lblDetailCreatedOnValue";
            this.lblDetailCreatedOnValue.Size = new System.Drawing.Size(131, 28);
            this.lblDetailCreatedOnValue.TabIndex = 5;
            this.lblDetailCreatedOnValue.Text = "—";
            this.lblDetailCreatedOnValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailModifiedOnCaption
            // 
            this.lblDetailModifiedOnCaption.AutoSize = true;
            this.lblDetailModifiedOnCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailModifiedOnCaption.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblDetailModifiedOnCaption.Location = new System.Drawing.Point(250, 26);
            this.lblDetailModifiedOnCaption.Name = "lblDetailModifiedOnCaption";
            this.lblDetailModifiedOnCaption.Size = new System.Drawing.Size(104, 28);
            this.lblDetailModifiedOnCaption.TabIndex = 6;
            this.lblDetailModifiedOnCaption.Text = "Modified On:";
            this.lblDetailModifiedOnCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDetailModifiedOnValue
            // 
            this.lblDetailModifiedOnValue.AutoSize = true;
            this.lblDetailModifiedOnValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDetailModifiedOnValue.Location = new System.Drawing.Point(360, 26);
            this.lblDetailModifiedOnValue.Name = "lblDetailModifiedOnValue";
            this.lblDetailModifiedOnValue.Size = new System.Drawing.Size(131, 28);
            this.lblDetailModifiedOnValue.TabIndex = 7;
            this.lblDetailModifiedOnValue.Text = "—";
            this.lblDetailModifiedOnValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlComponentDetailsActions
            // 
            this.pnlComponentDetailsActions.Controls.Add(this.btnExportComponents);
            this.pnlComponentDetailsActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlComponentDetailsActions.Location = new System.Drawing.Point(10, 22);
            this.pnlComponentDetailsActions.Name = "pnlComponentDetailsActions";
            this.pnlComponentDetailsActions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlComponentDetailsActions.Size = new System.Drawing.Size(494, 36);
            this.pnlComponentDetailsActions.TabIndex = 1;
            // 
            // btnExportComponents
            // 
            this.btnExportComponents.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportComponents.Enabled = false;
            this.btnExportComponents.Location = new System.Drawing.Point(354, 0);
            this.btnExportComponents.Name = "btnExportComponents";
            this.btnExportComponents.Size = new System.Drawing.Size(140, 28);
            this.btnExportComponents.TabIndex = 0;
            this.btnExportComponents.Text = "Export Component";
            this.btnExportComponents.UseVisualStyleBackColor = true;
            // 
            // pnlComponentsDockHeader
            // 
            this.pnlComponentsDockHeader.Controls.Add(this.lblComponentsDockTitle);
            this.pnlComponentsDockHeader.Controls.Add(this.btnComponentsPinToggle);
            this.pnlComponentsDockHeader.Controls.Add(this.btnComponentsCollapseToggle);
            this.pnlComponentsDockHeader.Controls.Add(this.pnlComponentsHeaderAccent);
            this.pnlComponentsDockHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlComponentsDockHeader.Location = new System.Drawing.Point(0, 16);
            this.pnlComponentsDockHeader.Name = "pnlComponentsDockHeader";
            this.pnlComponentsDockHeader.Size = new System.Drawing.Size(514, 30);
            this.pnlComponentsDockHeader.TabIndex = 2;
            // 
            // lblComponentsDockTitle
            // 
            this.lblComponentsDockTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblComponentsDockTitle.Location = new System.Drawing.Point(3, 0);
            this.lblComponentsDockTitle.Name = "lblComponentsDockTitle";
            this.lblComponentsDockTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblComponentsDockTitle.Size = new System.Drawing.Size(455, 30);
            this.lblComponentsDockTitle.TabIndex = 1;
            this.lblComponentsDockTitle.Text = "Components";
            this.lblComponentsDockTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnComponentsPinToggle
            // 
            this.btnComponentsPinToggle.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnComponentsPinToggle.Location = new System.Drawing.Point(458, 0);
            this.btnComponentsPinToggle.Name = "btnComponentsPinToggle";
            this.btnComponentsPinToggle.Size = new System.Drawing.Size(28, 30);
            this.btnComponentsPinToggle.TabIndex = 2;
            this.btnComponentsPinToggle.Text = "📌";
            this.btnComponentsPinToggle.UseVisualStyleBackColor = true;
            // 
            // btnComponentsCollapseToggle
            // 
            this.btnComponentsCollapseToggle.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnComponentsCollapseToggle.Location = new System.Drawing.Point(486, 0);
            this.btnComponentsCollapseToggle.Name = "btnComponentsCollapseToggle";
            this.btnComponentsCollapseToggle.Size = new System.Drawing.Size(28, 30);
            this.btnComponentsCollapseToggle.TabIndex = 3;
            this.btnComponentsCollapseToggle.Text = "◀";
            this.btnComponentsCollapseToggle.UseVisualStyleBackColor = true;
            // 
            // pnlComponentsHeaderAccent
            // 
            this.pnlComponentsHeaderAccent.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlComponentsHeaderAccent.Location = new System.Drawing.Point(0, 0);
            this.pnlComponentsHeaderAccent.Name = "pnlComponentsHeaderAccent";
            this.pnlComponentsHeaderAccent.Size = new System.Drawing.Size(3, 30);
            this.pnlComponentsHeaderAccent.TabIndex = 0;
            // 
            // pnlDockTabStrip
            // 
            this.pnlDockTabStrip.Controls.Add(this.lblSolutionsCollapsedTab);
            this.pnlDockTabStrip.Controls.Add(this.lblComponentsCollapsedTab);
            this.pnlDockTabStrip.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlDockTabStrip.Location = new System.Drawing.Point(10, 77);
            this.pnlDockTabStrip.Name = "pnlDockTabStrip";
            this.pnlDockTabStrip.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlDockTabStrip.Size = new System.Drawing.Size(972, 34);
            this.pnlDockTabStrip.TabIndex = 2;
            this.pnlDockTabStrip.WrapContents = false;
            // 
            // lblSolutionsCollapsedTab
            // 
            this.lblSolutionsCollapsedTab.AutoSize = true;
            this.lblSolutionsCollapsedTab.Location = new System.Drawing.Point(3, 0);
            this.lblSolutionsCollapsedTab.Name = "lblSolutionsCollapsedTab";
            this.lblSolutionsCollapsedTab.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.lblSolutionsCollapsedTab.Size = new System.Drawing.Size(92, 27);
            this.lblSolutionsCollapsedTab.TabIndex = 0;
            this.lblSolutionsCollapsedTab.Text = "▶  Solutions";
            this.lblSolutionsCollapsedTab.Visible = false;
            // 
            // lblComponentsCollapsedTab
            // 
            this.lblComponentsCollapsedTab.AutoSize = true;
            this.lblComponentsCollapsedTab.Location = new System.Drawing.Point(101, 0);
            this.lblComponentsCollapsedTab.Name = "lblComponentsCollapsedTab";
            this.lblComponentsCollapsedTab.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.lblComponentsCollapsedTab.Size = new System.Drawing.Size(112, 27);
            this.lblComponentsCollapsedTab.TabIndex = 1;
            this.lblComponentsCollapsedTab.Text = "▶  Components";
            this.lblComponentsCollapsedTab.Visible = false;
            // 
            // pnlExportOptions
            // 
            this.pnlExportOptions.Controls.Add(this.tlpExportOptions);
            this.pnlExportOptions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlExportOptions.Location = new System.Drawing.Point(10, 36);
            this.pnlExportOptions.Name = "pnlExportOptions";
            this.pnlExportOptions.Padding = new System.Windows.Forms.Padding(0, 6, 0, 8);
            this.pnlExportOptions.Size = new System.Drawing.Size(972, 41);
            this.pnlExportOptions.TabIndex = 1;
            // 
            // tlpExportOptions
            // 
            this.tlpExportOptions.ColumnCount = 3;
            this.tlpExportOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpExportOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tlpExportOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpExportOptions.Controls.Add(this.chkExportManaged, 0, 0);
            this.tlpExportOptions.Controls.Add(this.prgExport, 1, 0);
            this.tlpExportOptions.Controls.Add(this.lblExportStatus, 2, 0);
            this.tlpExportOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpExportOptions.Location = new System.Drawing.Point(0, 6);
            this.tlpExportOptions.Name = "tlpExportOptions";
            this.tlpExportOptions.RowCount = 1;
            this.tlpExportOptions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpExportOptions.Size = new System.Drawing.Size(972, 27);
            this.tlpExportOptions.TabIndex = 0;
            // 
            // chkExportManaged
            // 
            this.chkExportManaged.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkExportManaged.AutoSize = true;
            this.chkExportManaged.Location = new System.Drawing.Point(3, 4);
            this.chkExportManaged.Name = "chkExportManaged";
            this.chkExportManaged.Size = new System.Drawing.Size(126, 19);
            this.chkExportManaged.TabIndex = 0;
            this.chkExportManaged.Text = "Export as Managed";
            this.chkExportManaged.UseVisualStyleBackColor = true;
            // 
            // prgExport
            // 
            this.prgExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.prgExport.Location = new System.Drawing.Point(153, 5);
            this.prgExport.Name = "prgExport";
            this.prgExport.Size = new System.Drawing.Size(174, 17);
            this.prgExport.TabIndex = 1;
            this.prgExport.Visible = false;
            // 
            // lblExportStatus
            // 
            this.lblExportStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblExportStatus.AutoSize = true;
            this.lblExportStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblExportStatus.Location = new System.Drawing.Point(333, 6);
            this.lblExportStatus.Name = "lblExportStatus";
            this.lblExportStatus.Size = new System.Drawing.Size(0, 15);
            this.lblExportStatus.TabIndex = 2;
            // 
            // tsBrowseActions
            // 
            this.tsBrowseActions.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsBrowseActions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLoadAllSolutions,
            this.btnAbortBrowse});
            this.tsBrowseActions.Location = new System.Drawing.Point(10, 10);
            this.tsBrowseActions.Name = "tsBrowseActions";
            this.tsBrowseActions.Padding = new System.Windows.Forms.Padding(2);
            this.tsBrowseActions.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.tsBrowseActions.Size = new System.Drawing.Size(972, 26);
            this.tsBrowseActions.TabIndex = 0;
            // 
            // btnLoadAllSolutions
            // 
            this.btnLoadAllSolutions.BackColor = System.Drawing.Color.Transparent;
            this.btnLoadAllSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnLoadAllSolutions.Name = "btnLoadAllSolutions";
            this.btnLoadAllSolutions.Size = new System.Drawing.Size(106, 19);
            this.btnLoadAllSolutions.Text = "Load All Solutions";
            // 
            // btnAbortBrowse
            // 
            this.btnAbortBrowse.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnAbortBrowse.Enabled = false;
            this.btnAbortBrowse.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAbortBrowse.Name = "btnAbortBrowse";
            this.btnAbortBrowse.Size = new System.Drawing.Size(43, 19);
            this.btnAbortBrowse.Text = "Abort";
            // 
            // tabSearch
            // 
            this.tabSearch.Controls.Add(this.pnlSearchRoot);
            this.tabSearch.Location = new System.Drawing.Point(4, 28);
            this.tabSearch.Name = "tabSearch";
            this.tabSearch.Padding = new System.Windows.Forms.Padding(10);
            this.tabSearch.Size = new System.Drawing.Size(992, 578);
            this.tabSearch.TabIndex = 0;
            this.tabSearch.Text = "Search Components";
            this.tabSearch.UseVisualStyleBackColor = true;
            // 
            // pnlSearchRoot
            // 
            this.pnlSearchRoot.Controls.Add(this.grpSearchResults);
            this.pnlSearchRoot.Controls.Add(this.grpSearchCriteria);
            this.pnlSearchRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSearchRoot.Location = new System.Drawing.Point(10, 10);
            this.pnlSearchRoot.Name = "pnlSearchRoot";
            this.pnlSearchRoot.Size = new System.Drawing.Size(972, 558);
            this.pnlSearchRoot.TabIndex = 0;
            // 
            // grpSearchResults
            // 
            this.grpSearchResults.Controls.Add(this.dgvResults);
            this.grpSearchResults.Controls.Add(this.pnlSearchStatus);
            this.grpSearchResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSearchResults.Location = new System.Drawing.Point(0, 171);
            this.grpSearchResults.Name = "grpSearchResults";
            this.grpSearchResults.Padding = new System.Windows.Forms.Padding(8, 6, 8, 8);
            this.grpSearchResults.Size = new System.Drawing.Size(972, 387);
            this.grpSearchResults.TabIndex = 1;
            this.grpSearchResults.TabStop = false;
            this.grpSearchResults.Text = "Search Results";
            // 
            // dgvResults
            // 
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvResults.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvResults.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvResults.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvResults.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(247)))));
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.dgvResults.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvResults.ColumnHeadersHeight = 30;
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvResults.ContextMenuStrip = this.cmsResults;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvResults.DefaultCellStyle = dataGridViewCellStyle6;
            this.dgvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvResults.EnableHeadersVisualStyles = false;
            this.dgvResults.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.dgvResults.Location = new System.Drawing.Point(8, 49);
            this.dgvResults.MultiSelect = false;
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.RowTemplate.Height = 24;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(956, 330);
            this.dgvResults.TabIndex = 1;
            // 
            // cmsResults
            // 
            this.cmsResults.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miCopySolutionName,
            this.miCopyComponentName,
            this.miOpenInMaker});
            this.cmsResults.Name = "cmsResults";
            this.cmsResults.Size = new System.Drawing.Size(246, 70);
            // 
            // miCopySolutionName
            // 
            this.miCopySolutionName.Name = "miCopySolutionName";
            this.miCopySolutionName.Size = new System.Drawing.Size(245, 22);
            this.miCopySolutionName.Text = "Copy Solution Name";
            // 
            // miCopyComponentName
            // 
            this.miCopyComponentName.Name = "miCopyComponentName";
            this.miCopyComponentName.Size = new System.Drawing.Size(245, 22);
            this.miCopyComponentName.Text = "Copy Component Logical Name";
            // 
            // miOpenInMaker
            // 
            this.miOpenInMaker.Name = "miOpenInMaker";
            this.miOpenInMaker.Size = new System.Drawing.Size(245, 22);
            this.miOpenInMaker.Text = "Open Solution";
            // 
            // pnlSearchStatus
            // 
            this.pnlSearchStatus.Controls.Add(this.lblSummary);
            this.pnlSearchStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSearchStatus.Location = new System.Drawing.Point(8, 22);
            this.pnlSearchStatus.Name = "pnlSearchStatus";
            this.pnlSearchStatus.Padding = new System.Windows.Forms.Padding(0, 4, 0, 6);
            this.pnlSearchStatus.Size = new System.Drawing.Size(956, 27);
            this.pnlSearchStatus.TabIndex = 0;
            // 
            // lblSummary
            // 
            this.lblSummary.AutoSize = true;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSummary.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblSummary.Location = new System.Drawing.Point(0, 4);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(201, 15);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = "Enter search criteria and click Search.";
            // 
            // grpSearchCriteria
            // 
            this.grpSearchCriteria.Controls.Add(this.tlpSearchCriteria);
            this.grpSearchCriteria.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSearchCriteria.Location = new System.Drawing.Point(0, 0);
            this.grpSearchCriteria.Name = "grpSearchCriteria";
            this.grpSearchCriteria.Padding = new System.Windows.Forms.Padding(10, 8, 10, 10);
            this.grpSearchCriteria.Size = new System.Drawing.Size(972, 171);
            this.grpSearchCriteria.TabIndex = 0;
            this.grpSearchCriteria.TabStop = false;
            this.grpSearchCriteria.Text = "Search Criteria";
            // 
            // tlpSearchCriteria
            // 
            this.tlpSearchCriteria.ColumnCount = 4;
            this.tlpSearchCriteria.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpSearchCriteria.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpSearchCriteria.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.tlpSearchCriteria.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpSearchCriteria.Controls.Add(this.lblMainType, 0, 0);
            this.tlpSearchCriteria.Controls.Add(this.cboMainType, 1, 0);
            this.tlpSearchCriteria.Controls.Add(this.lblMainSearch, 2, 0);
            this.tlpSearchCriteria.Controls.Add(this.txtMainSearch, 3, 0);
            this.tlpSearchCriteria.Controls.Add(this.lblSubType, 0, 1);
            this.tlpSearchCriteria.Controls.Add(this.cboSubType, 1, 1);
            this.tlpSearchCriteria.Controls.Add(this.lblSubSearch, 2, 1);
            this.tlpSearchCriteria.Controls.Add(this.txtSubSearch, 3, 1);
            this.tlpSearchCriteria.Controls.Add(this.chkManagedOnly, 0, 2);
            this.tlpSearchCriteria.Controls.Add(this.flpSearchButtons, 0, 3);
            this.tlpSearchCriteria.Controls.Add(this.chkUnmanagedOnly, 1, 2);
            this.tlpSearchCriteria.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpSearchCriteria.Location = new System.Drawing.Point(10, 24);
            this.tlpSearchCriteria.Name = "tlpSearchCriteria";
            this.tlpSearchCriteria.RowCount = 4;
            this.tlpSearchCriteria.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpSearchCriteria.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpSearchCriteria.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlpSearchCriteria.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tlpSearchCriteria.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpSearchCriteria.Size = new System.Drawing.Size(952, 137);
            this.tlpSearchCriteria.TabIndex = 0;
            // 
            // lblMainType
            // 
            this.lblMainType.AutoSize = true;
            this.lblMainType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMainType.Location = new System.Drawing.Point(3, 0);
            this.lblMainType.Name = "lblMainType";
            this.lblMainType.Size = new System.Drawing.Size(124, 34);
            this.lblMainType.TabIndex = 0;
            this.lblMainType.Text = "Component type:";
            this.lblMainType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboMainType
            // 
            this.cboMainType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboMainType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMainType.Location = new System.Drawing.Point(133, 7);
            this.cboMainType.Margin = new System.Windows.Forms.Padding(3, 6, 10, 3);
            this.cboMainType.Name = "cboMainType";
            this.cboMainType.Size = new System.Drawing.Size(328, 23);
            this.cboMainType.TabIndex = 1;
            // 
            // lblMainSearch
            // 
            this.lblMainSearch.AutoSize = true;
            this.lblMainSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMainSearch.Location = new System.Drawing.Point(474, 0);
            this.lblMainSearch.Name = "lblMainSearch";
            this.lblMainSearch.Size = new System.Drawing.Size(134, 34);
            this.lblMainSearch.TabIndex = 2;
            this.lblMainSearch.Text = "Name contains:";
            this.lblMainSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMainSearch
            // 
            this.txtMainSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMainSearch.Location = new System.Drawing.Point(614, 7);
            this.txtMainSearch.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.txtMainSearch.Name = "txtMainSearch";
            this.txtMainSearch.Size = new System.Drawing.Size(335, 23);
            this.txtMainSearch.TabIndex = 3;
            // 
            // lblSubType
            // 
            this.lblSubType.AutoSize = true;
            this.lblSubType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubType.Location = new System.Drawing.Point(3, 34);
            this.lblSubType.Name = "lblSubType";
            this.lblSubType.Size = new System.Drawing.Size(124, 34);
            this.lblSubType.TabIndex = 4;
            this.lblSubType.Text = "Sub-component:";
            this.lblSubType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboSubType
            // 
            this.cboSubType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboSubType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSubType.Location = new System.Drawing.Point(133, 41);
            this.cboSubType.Margin = new System.Windows.Forms.Padding(3, 6, 10, 3);
            this.cboSubType.Name = "cboSubType";
            this.cboSubType.Size = new System.Drawing.Size(328, 23);
            this.cboSubType.TabIndex = 5;
            // 
            // lblSubSearch
            // 
            this.lblSubSearch.AutoSize = true;
            this.lblSubSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubSearch.Location = new System.Drawing.Point(474, 34);
            this.lblSubSearch.Name = "lblSubSearch";
            this.lblSubSearch.Size = new System.Drawing.Size(134, 34);
            this.lblSubSearch.TabIndex = 6;
            this.lblSubSearch.Text = "Name contains:";
            this.lblSubSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtSubSearch
            // 
            this.txtSubSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSubSearch.Location = new System.Drawing.Point(614, 41);
            this.txtSubSearch.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.txtSubSearch.Name = "txtSubSearch";
            this.txtSubSearch.Size = new System.Drawing.Size(335, 23);
            this.txtSubSearch.TabIndex = 7;
            // 
            // chkManagedOnly
            // 
            this.chkManagedOnly.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkManagedOnly.AutoSize = true;
            this.chkManagedOnly.Location = new System.Drawing.Point(3, 76);
            this.chkManagedOnly.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.chkManagedOnly.Name = "chkManagedOnly";
            this.chkManagedOnly.Size = new System.Drawing.Size(102, 19);
            this.chkManagedOnly.TabIndex = 8;
            this.chkManagedOnly.Text = "Managed only";
            this.chkManagedOnly.UseVisualStyleBackColor = true;
            // 
            // flpSearchButtons
            // 
            this.flpSearchButtons.AutoSize = true;
            this.flpSearchButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpSearchCriteria.SetColumnSpan(this.flpSearchButtons, 4);
            this.flpSearchButtons.Controls.Add(this.btnSearch);
            this.flpSearchButtons.Controls.Add(this.btnAbort);
            this.flpSearchButtons.Controls.Add(this.btnClear);
            this.flpSearchButtons.Dock = System.Windows.Forms.DockStyle.Right;
            this.flpSearchButtons.Location = new System.Drawing.Point(676, 106);
            this.flpSearchButtons.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.flpSearchButtons.Name = "flpSearchButtons";
            this.flpSearchButtons.Size = new System.Drawing.Size(276, 36);
            this.flpSearchButtons.TabIndex = 10;
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(0, 0);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(100, 27);
            this.btnSearch.TabIndex = 0;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            // 
            // btnAbort
            // 
            this.btnAbort.Enabled = false;
            this.btnAbort.Location = new System.Drawing.Point(108, 0);
            this.btnAbort.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(80, 27);
            this.btnAbort.TabIndex = 1;
            this.btnAbort.Text = "Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(196, 0);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(80, 27);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            // 
            // chkUnmanagedOnly
            // 
            this.chkUnmanagedOnly.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkUnmanagedOnly.AutoSize = true;
            this.chkUnmanagedOnly.Location = new System.Drawing.Point(133, 76);
            this.chkUnmanagedOnly.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.chkUnmanagedOnly.Name = "chkUnmanagedOnly";
            this.chkUnmanagedOnly.Size = new System.Drawing.Size(117, 19);
            this.chkUnmanagedOnly.TabIndex = 9;
            this.chkUnmanagedOnly.Text = "Unmanaged only";
            this.chkUnmanagedOnly.UseVisualStyleBackColor = true;
            // 
            // pnlLog
            // 
            this.pnlLog.Controls.Add(this.lstLog);
            this.pnlLog.Controls.Add(this.lblLogHeader);
            this.pnlLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLog.Location = new System.Drawing.Point(0, 0);
            this.pnlLog.Name = "pnlLog";
            this.pnlLog.Padding = new System.Windows.Forms.Padding(12, 6, 12, 8);
            this.pnlLog.Size = new System.Drawing.Size(1000, 118);
            this.pnlLog.TabIndex = 0;
            // 
            // lstLog
            // 
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.IntegralHeight = false;
            this.lstLog.Location = new System.Drawing.Point(12, 28);
            this.lstLog.Name = "lstLog";
            this.lstLog.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lstLog.Size = new System.Drawing.Size(976, 82);
            this.lstLog.TabIndex = 0;
            // 
            // lblLogHeader
            // 
            this.lblLogHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLogHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblLogHeader.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblLogHeader.Location = new System.Drawing.Point(12, 6);
            this.lblLogHeader.Name = "lblLogHeader";
            this.lblLogHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblLogHeader.Size = new System.Drawing.Size(976, 22);
            this.lblLogHeader.TabIndex = 1;
            this.lblLogHeader.Text = "Activity log";
            // 
            // cmsSolutionActions
            // 
            this.cmsSolutionActions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miActionsViewComponents,
            this.miActionsOpenInBrowser,
            this.miActionsExportSolution});
            this.cmsSolutionActions.Name = "cmsSolutionActions";
            this.cmsSolutionActions.Size = new System.Drawing.Size(167, 70);
            // 
            // miActionsViewComponents
            // 
            this.miActionsViewComponents.Name = "miActionsViewComponents";
            this.miActionsViewComponents.Size = new System.Drawing.Size(166, 22);
            this.miActionsViewComponents.Text = "View Component";
            // 
            // miActionsOpenInBrowser
            // 
            this.miActionsOpenInBrowser.Name = "miActionsOpenInBrowser";
            this.miActionsOpenInBrowser.Size = new System.Drawing.Size(166, 22);
            this.miActionsOpenInBrowser.Text = "Open in Browser";
            // 
            // miActionsExportSolution
            // 
            this.miActionsExportSolution.Name = "miActionsExportSolution";
            this.miActionsExportSolution.Size = new System.Drawing.Size(166, 22);
            this.miActionsExportSolution.Text = "Export Solution";
            // 
            // SolutionSherlockControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlConnectionStatus);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "SolutionSherlockControl";
            this.Size = new System.Drawing.Size(1000, 760);
            this.pnlConnectionStatus.ResumeLayout(false);
            this.pnlConnectionStatus.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tabBrowse.ResumeLayout(false);
            this.pnlBrowseRoot.ResumeLayout(false);
            this.pnlBrowseRoot.PerformLayout();
            this.splitBrowseTop.Panel1.ResumeLayout(false);
            this.splitBrowseTop.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitBrowseTop)).EndInit();
            this.splitBrowseTop.ResumeLayout(false);
            this.grpSolutions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolutions)).EndInit();
            this.pnlPaging.ResumeLayout(false);
            this.pnlSolutionsSearch.ResumeLayout(false);
            this.pnlSolutionsSearch.PerformLayout();
            this.pnlSolutionsDockHeader.ResumeLayout(false);
            this.grpComponents.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolutionComponents)).EndInit();
            this.pnlComponentsHeader.ResumeLayout(false);
            this.pnlComponentsHeader.PerformLayout();
            this.grpComponentDetails.ResumeLayout(false);
            this.tlpComponentDetails.ResumeLayout(false);
            this.tlpComponentDetails.PerformLayout();
            this.pnlComponentDetailsActions.ResumeLayout(false);
            this.pnlComponentsDockHeader.ResumeLayout(false);
            this.pnlDockTabStrip.ResumeLayout(false);
            this.pnlDockTabStrip.PerformLayout();
            this.pnlExportOptions.ResumeLayout(false);
            this.tlpExportOptions.ResumeLayout(false);
            this.tlpExportOptions.PerformLayout();
            this.tsBrowseActions.ResumeLayout(false);
            this.tsBrowseActions.PerformLayout();
            this.tabSearch.ResumeLayout(false);
            this.pnlSearchRoot.ResumeLayout(false);
            this.grpSearchResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.cmsResults.ResumeLayout(false);
            this.pnlSearchStatus.ResumeLayout(false);
            this.pnlSearchStatus.PerformLayout();
            this.grpSearchCriteria.ResumeLayout(false);
            this.tlpSearchCriteria.ResumeLayout(false);
            this.tlpSearchCriteria.PerformLayout();
            this.flpSearchButtons.ResumeLayout(false);
            this.pnlLog.ResumeLayout(false);
            this.cmsSolutionActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnAbort;
        private System.Windows.Forms.Button btnClear;
    }
}
