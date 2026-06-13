using ClassroomComputerTracker.Data;
using ClassroomComputerTracker.Models;

namespace ClassroomComputerTracker
{
    public partial class MainForm : Form
    {
        // ── Colours (Catppuccin Mocha) ────────────────────────────
        static readonly Color BgBase    = Color.FromArgb(30,  30,  46);
        static readonly Color BgMantle  = Color.FromArgb(24,  24,  37);
        static readonly Color BgSurface = Color.FromArgb(49,  50,  68);
        static readonly Color BgOverlay = Color.FromArgb(69,  71,  90);
        static readonly Color Accent    = Color.FromArgb(203, 166, 247);
        static readonly Color FgMain    = Color.FromArgb(205, 214, 244);
        static readonly Color FgMuted   = Color.FromArgb(166, 173, 200);
        static readonly Color Green     = Color.FromArgb(166, 227, 161);
        static readonly Color Orange    = Color.FromArgb(250, 179, 135);
        static readonly Color Red       = Color.FromArgb(243, 139, 168);
        static readonly Color Blue      = Color.FromArgb(137, 180, 250);
        static readonly Color Yellow    = Color.FromArgb(249, 226, 175);

        // ── Core ─────────────────────────────────────────────────
        private readonly DatabaseManager _db;

        // ── State ─────────────────────────────────────────────────
        private List<Computer>      _allComputers  = new();
        private List<ServiceCall>   _serviceCalls  = new();
        private List<HotSwapItem>   _hotSwapItems  = new();
        private ServiceCall?        _selectedCall;
        private HotSwapItem?        _selectedHotSwapItem;
        private Computer?           _selectedHotSwapComp;
        private string              _callFilter    = "All";

        // ── Tab Controls ──────────────────────────────────────────
        private RoomTabPanel  _classroom   = null!;
        private RoomTabPanel  _serverRoom  = null!;

        // Service Calls tab
        private DataGridView _callGrid    = null!;
        private Panel        _callDetail  = null!;

        // Hot Swap tab
        private DataGridView _hsCompGrid  = null!;
        private DataGridView _hsItemGrid  = null!;

        // Activity Log tab
        private DataGridView _logGrid     = null!;

        // Status bar
        private Label _statusLbl = null!;

        // ─────────────────────────────────────────────────────────
        public MainForm(DatabaseManager db)
        {
            InitializeComponent();
            _db = db;
            BuildUI();
            RefreshAllData();
        }

        // ─────────────────────────────────────────────────────────
        // Top-level UI
        // ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            Text          = "Classroom Computer Tracker";
            Size          = new Size(1350, 820);
            MinimumSize   = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = BgBase;
            ForeColor     = FgMain;

            // ── Title bar ──────────────────────────────────────
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = BgSurface };

            var titleLbl = new Label
            {
                Text = "🖥  Classroom Computer Tracker",
                ForeColor = Accent, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 11)
            };

            var userLbl = new Label
            {
                Text = $"👤  {AppSession.Username}",
                ForeColor = FgMuted, Font = new Font("Segoe UI", 9f),
                AutoSize = true
            };

            var btnLogout = new Button
            {
                Text = "Logout", AutoSize = false, Width = 72, Height = 26,
                BackColor = BgOverlay, ForeColor = FgMuted, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f), Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (_, __) =>
            {
                AppSession.ShouldLogout = true;
                AppSession.UserId       = 0;
                AppSession.Username     = "";
                Close();
            };

            titleBar.Controls.Add(titleLbl);
            titleBar.Controls.Add(userLbl);
            titleBar.Controls.Add(btnLogout);
            titleBar.Resize += (_, __) =>
            {
                btnLogout.Location = new Point(titleBar.Width - 72  - 10, 8);
                userLbl.Location   = new Point(titleBar.Width - 72 - userLbl.Width - 20, 13);
            };

            // ── Status bar ─────────────────────────────────────
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = BgMantle };
            _statusLbl = new Label
            {
                AutoSize = false, Dock = DockStyle.Fill, ForeColor = FgMuted,
                Font = new Font("Segoe UI", 8.5f), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            statusBar.Controls.Add(_statusLbl);

            // ── Tab control ────────────────────────────────────
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                ItemSize = new Size(140, 28)
            };
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.DrawItem += TabsDrawItem;
            tabs.SelectedIndexChanged += (_, __) => OnTabChanged(tabs.SelectedIndex);

            // Tab 1 — Classroom
            var t1 = new TabPage { Text = "🏫  Classroom",   BackColor = BgBase, Padding = new Padding(0) };
            _classroom = new RoomTabPanel("Classroom", _db);
            _classroom.DataChanged += OnRoomDataChanged;
            t1.Controls.Add(_classroom);

            // Tab 2 — Server Room
            var t2 = new TabPage { Text = "🖧  Server Room", BackColor = BgBase, Padding = new Padding(0) };
            _serverRoom = new RoomTabPanel("Server Room", _db);
            _serverRoom.DataChanged += OnRoomDataChanged;
            t2.Controls.Add(_serverRoom);

            // Tab 3 — Service Calls
            var t3 = new TabPage { Text = "🛠  Service Calls", BackColor = BgBase, Padding = new Padding(0) };
            BuildServiceCallsTab(t3);

            // Tab 4 — Hot Swap
            var t4 = new TabPage { Text = "🔄  Hot Swap",    BackColor = BgBase, Padding = new Padding(0) };
            BuildHotSwapTab(t4);

            // Tab 5 — Activity Log
            var t5 = new TabPage { Text = "📋  Activity Log", BackColor = BgBase, Padding = new Padding(0) };
            BuildActivityLogTab(t5);

            tabs.TabPages.AddRange(new[] { t1, t2, t3, t4, t5 });

            Controls.Add(tabs);
            Controls.Add(statusBar);
            Controls.Add(titleBar);
        }

        // Owner-draw tabs to match the dark theme
        private void TabsDrawItem(object? s, DrawItemEventArgs e)
        {
            var tab = (TabControl)s!;
            bool sel = e.Index == tab.SelectedIndex;
            using var bgBrush  = new SolidBrush(sel ? BgSurface : BgMantle);
            using var fgBrush  = new SolidBrush(sel ? FgMain : FgMuted);
            using var fnt      = new Font("Segoe UI", 9f, sel ? FontStyle.Bold : FontStyle.Regular);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tab.TabPages[e.Index].Text, fnt, fgBrush, e.Bounds, sf);
            if (sel)
            {
                using var accentPen = new Pen(Accent, 2);
                e.Graphics.DrawLine(accentPen, e.Bounds.Left, e.Bounds.Bottom - 1,
                    e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private void OnTabChanged(int index)
        {
            // Refresh data-heavy tabs when they become active
            switch (index)
            {
                case 2: LoadServiceCalls();  break;
                case 3: LoadHotSwapData();   break;
                case 4: LoadActivityLog();   break;
            }
        }

        // ─────────────────────────────────────────────────────────
        // Cross-tab data refresh
        // ─────────────────────────────────────────────────────────
        private void OnRoomDataChanged(object? s, EventArgs e)
        {
            var sender = s as RoomTabPanel;
            if (sender != _classroom)  _classroom.RefreshData();
            if (sender != _serverRoom) _serverRoom.RefreshData();
            RefreshAllData();
        }

        private void RefreshAllData()
        {
            _allComputers = _db.GetAllComputers();
            LoadServiceCalls();
            LoadHotSwapData();
            UpdateGlobalStatus();
        }

        private void UpdateGlobalStatus()
        {
            int total   = _allComputers.Count;
            int active  = _allComputers.Count(c => c.Status == "Active");
            int broken  = _allComputers.Count(c => c.Status == "Broken");
            int retired = _allComputers.Count(c => c.Status == "Retired");
            int openCalls = _serviceCalls.Count(sc => sc.Status != "Resolved");
            _statusLbl.Text =
                $"👤 {AppSession.Username}   " +
                $"● Active: {active}   ● Broken: {broken}   ● Retired: {retired}   " +
                $"Total: {total}   🛠 Open Calls: {openCalls}";
        }

        // ─────────────────────────────────────────────────────────
        // ── SERVICE CALLS TAB ────────────────────────────────────
        // ─────────────────────────────────────────────────────────
        private void BuildServiceCallsTab(TabPage page)
        {
            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgMantle };

            Button Btn(string label, Color bg, Color? fg = null)
            {
                var b = new Button
                {
                    Text = label, AutoSize = false, Width = 120, Height = 30,
                    BackColor = bg, ForeColor = fg ?? BgBase, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }

            var btnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(8, 8, 0, 0), BackColor = BgMantle
            };

            var btnNew     = Btn("＋  New Call",   Green);
            var btnEdit    = Btn("✏  Edit",        Blue);
            var btnDelete  = Btn("✕  Delete",      Red);

            btnNew.Click    += (_, __) => AddServiceCall();
            btnEdit.Click   += (_, __) => EditServiceCall();
            btnDelete.Click += (_, __) => DeleteServiceCall();

            btnFlow.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDelete });

            // Filter
            var filterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(0, 10, 12, 0), BackColor = BgMantle
            };
            var lblFilter = new Label
            {
                Text = "Filter:", ForeColor = FgMuted, Font = new Font("Segoe UI", 9f),
                AutoSize = true, Margin = new Padding(4, 2, 6, 0)
            };
            var cmbFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f),
                Width = 130
            };
            cmbFilter.Items.AddRange(new object[] { "All", "Open", "In Progress", "Resolved" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (_, __) =>
            {
                _callFilter = cmbFilter.SelectedItem?.ToString() ?? "All";
                BindServiceCallGrid();
            };
            filterFlow.Controls.Add(lblFilter);
            filterFlow.Controls.Add(cmbFilter);

            toolbar.Controls.Add(filterFlow);
            toolbar.Controls.Add(btnFlow);

            // Grid
            _callGrid = MakeGrid();
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ServiceCallId", HeaderText = "#",          DataPropertyName = "ServiceCallId", Width = 40 });
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ComputerName",  HeaderText = "Computer",   DataPropertyName = "ComputerName",  Width = 120 });
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description",   HeaderText = "Issue",      DataPropertyName = "Description",   Width = 200 });
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",        HeaderText = "Status",     DataPropertyName = "Status",        Width = 90 });
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpenedByUser",  HeaderText = "Opened By",  DataPropertyName = "OpenedByUser",  Width = 90 });
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpenedAt",      HeaderText = "Opened",     DataPropertyName = "OpenedAtFmt",   Width = 110 });
            _callGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClosedAt",      HeaderText = "Closed",     DataPropertyName = "ClosedAtFmt",   Width = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            _callGrid.CellFormatting   += CallGridCellFormatting;
            _callGrid.SelectionChanged += CallGridSelectionChanged;
            _callGrid.DoubleClick      += (_, __) => EditServiceCall();

            // Detail card
            _callDetail = new Panel
            {
                Dock = DockStyle.Bottom, Height = 140,
                BackColor = BgSurface, Padding = new Padding(12)
            };
            _callDetail.Paint += CallDetailPaint;

            var header = MakeHeader("SERVICE CALLS");

            var body = new Panel { Dock = DockStyle.Fill };
            body.Controls.Add(_callGrid);
            body.Controls.Add(header);

            page.Controls.Add(_callDetail);
            page.Controls.Add(body);
            page.Controls.Add(toolbar);
        }

        private void LoadServiceCalls()
        {
            _serviceCalls = _db.GetAllServiceCalls();
            BindServiceCallGrid();
            UpdateGlobalStatus();
        }

        private void BindServiceCallGrid()
        {
            var filtered = _callFilter == "All"
                ? _serviceCalls
                : _serviceCalls.Where(sc => sc.Status == _callFilter).ToList();

            _callGrid.DataSource = null;
            _callGrid.DataSource = filtered.Select(sc => new
            {
                sc.ServiceCallId,
                sc.ComputerName,
                Description   = sc.Description.Length > 60
                    ? sc.Description[..60] + "…" : sc.Description,
                sc.Status,
                sc.OpenedByUser,
                OpenedAtFmt   = sc.OpenedAt.ToString("M/d/yy h:mm tt"),
                ClosedAtFmt   = sc.ClosedAt.HasValue
                    ? sc.ClosedAt.Value.ToString("M/d/yy h:mm tt") : "—"
            }).ToList();
        }

        private void CallGridSelectionChanged(object? s, EventArgs e)
        {
            if (_callGrid.SelectedRows.Count == 0) return;
            var cell = _callGrid.SelectedRows[0].Cells["ServiceCallId"];
            if (cell.Value == null) return;
            _selectedCall = _serviceCalls.FirstOrDefault(
                sc => sc.ServiceCallId == Convert.ToInt32(cell.Value));
            _callDetail.Invalidate();
        }

        private void CallGridCellFormatting(object? s, DataGridViewCellFormattingEventArgs e)
        {
            if (_callGrid.Columns[e.ColumnIndex].Name != "Status" || e.Value == null) return;
            e.CellStyle.ForeColor = e.Value.ToString() switch
            {
                "Open"        => Green,
                "In Progress" => Orange,
                "Resolved"    => Blue,
                _             => FgMain
            };
        }

        private void CallDetailPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BgSurface);

            if (_selectedCall == null)
            {
                using var f = new Font("Segoe UI", 9f, FontStyle.Italic);
                g.DrawString("Select a service call to view details", f,
                    new SolidBrush(FgMuted), new Point(12, 12));
                return;
            }

            var sc = _selectedCall;
            int y = 10;
            const int lx = 12, vx = 130;

            using var titleFont  = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var lblFont    = new Font("Segoe UI", 8f);
            using var valFont    = new Font("Segoe UI", 8.5f);
            using var titleBrush = new SolidBrush(Accent);
            using var lblBrush   = new SolidBrush(FgMuted);
            using var valBrush   = new SolidBrush(FgMain);

            Color statusColor = sc.Status switch
            {
                "Open" => Green, "In Progress" => Orange, "Resolved" => Blue, _ => FgMain
            };
            using var statusBrush = new SolidBrush(statusColor);

            g.DrawString($"🛠  #{sc.ServiceCallId} — {sc.ComputerName}", titleFont, titleBrush, lx, y); y += 22;

            void Row(string lbl, string val, Color? vc = null)
            {
                g.DrawString(lbl, lblFont, lblBrush, lx, y);
                using var vb = new SolidBrush(vc ?? FgMain);
                g.DrawString(val, valFont, vb, vx, y);
                y += 18;
            }

            Row("Status",      sc.Status, statusColor);
            Row("Opened by",   $"{sc.OpenedByUser}  on  {sc.OpenedAt:M/d/yyyy h:mm tt}");
            if (sc.ClosedAt.HasValue)
                Row("Resolved by", $"{sc.ResolvedByUser}  on  {sc.ClosedAt.Value:M/d/yyyy h:mm tt}");

            // Description (truncated if very long)
            g.DrawString("Issue", lblFont, lblBrush, lx, y);
            g.DrawString(sc.Description, valFont, valBrush,
                new RectangleF(vx, y, _callDetail.Width - vx - 12, 32));
            y += 20;

            if (sc.Status == "Resolved" && !string.IsNullOrWhiteSpace(sc.SolutionNotes))
            {
                g.DrawString("Solution", lblFont, lblBrush, lx, y);
                using var solBrush = new SolidBrush(Green);
                g.DrawString(sc.SolutionNotes, valFont, solBrush,
                    new RectangleF(vx, y, _callDetail.Width - vx - 12, 36));
            }
        }

        private void AddServiceCall()
        {
            _allComputers = _db.GetAllComputers();
            if (_allComputers.Count == 0)
            {
                MessageBox.Show("No computers in the system yet.", "No Computers",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var f = new ServiceCallForm(null, _allComputers);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            _db.AddServiceCall(f.ServiceCall, AppSession.Username);
            LoadServiceCalls();
        }

        private void EditServiceCall()
        {
            if (_selectedCall == null) { Info("Select a service call to edit."); return; }
            _allComputers = _db.GetAllComputers();
            using var f = new ServiceCallForm(_selectedCall, _allComputers);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            _db.UpdateServiceCall(f.ServiceCall, AppSession.Username);
            LoadServiceCalls();
        }

        private void DeleteServiceCall()
        {
            if (_selectedCall == null) { Info("Select a service call to delete."); return; }
            if (MessageBox.Show(
                $"Delete service call #{_selectedCall.ServiceCallId} for {_selectedCall.ComputerName}?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _db.DeleteServiceCall(_selectedCall.ServiceCallId, AppSession.Username);
            _selectedCall = null;
            LoadServiceCalls();
        }

        // ─────────────────────────────────────────────────────────
        // ── HOT SWAP TAB ──────────────────────────────────────────
        // ─────────────────────────────────────────────────────────
        private void BuildHotSwapTab(TabPage page)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
                BackColor = BgBase, SplitterWidth = 4
            };
            split.Panel1.BackColor = BgBase;
            split.Panel2.BackColor = BgBase;

            // ── Top: Hot Swap Computers ───────────────────────
            var topToolbar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgMantle };
            var topBtnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(8, 8, 0, 0), BackColor = BgMantle
            };

            Button HBtn(string t, Color bg, Color? fg = null)
            {
                var b = new Button
                {
                    Text = t, AutoSize = false, Width = 140, Height = 30,
                    BackColor = bg, ForeColor = fg ?? BgBase, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }

            var btnRemoveHS = HBtn("✕  Remove from Pool", Red);
            btnRemoveHS.Click += (_, __) => RemoveFromHotSwap();
            topBtnFlow.Controls.Add(btnRemoveHS);
            topToolbar.Controls.Add(topBtnFlow);

            _hsCompGrid = MakeGrid();
            _hsCompGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ComputerName", HeaderText = "Name",     DataPropertyName = "ComputerName", Width = 130 });
            _hsCompGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "Serial #", DataPropertyName = "SerialNumber", Width = 120 });
            _hsCompGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model",        HeaderText = "Model",    DataPropertyName = "Model",        Width = 120 });
            _hsCompGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location",     HeaderText = "Location", DataPropertyName = "Location",     Width = 100 });
            _hsCompGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",       HeaderText = "Status",   DataPropertyName = "Status",       Width = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _hsCompGrid.SelectionChanged += HsCompGridSelectionChanged;

            var compHeader = MakeHeader("🔄  HOT SWAP COMPUTERS  —  computers available for immediate replacement");
            var compBody   = new Panel { Dock = DockStyle.Fill };
            compBody.Controls.Add(_hsCompGrid);
            compBody.Controls.Add(compHeader);

            split.Panel1.Controls.Add(compBody);
            split.Panel1.Controls.Add(topToolbar);

            // ── Bottom: Hot Swap Equipment ────────────────────
            var botToolbar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgMantle };
            var botBtnFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(8, 8, 0, 0), BackColor = BgMantle
            };

            var btnAddItem    = HBtn("＋  Add Item",   Green);
            var btnEditItem   = HBtn("✏  Edit Item",   Blue);
            var btnDeleteItem = HBtn("✕  Delete Item", Red);
            btnAddItem.Width = btnEditItem.Width = btnDeleteItem.Width = 120;

            btnAddItem.Click    += (_, __) => AddHotSwapItem();
            btnEditItem.Click   += (_, __) => EditHotSwapItem();
            btnDeleteItem.Click += (_, __) => DeleteHotSwapItem();

            botBtnFlow.Controls.AddRange(new Control[] { btnAddItem, btnEditItem, btnDeleteItem });
            botToolbar.Controls.Add(botBtnFlow);

            _hsItemGrid = MakeGrid();
            _hsItemGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemType",     HeaderText = "Type",      DataPropertyName = "ItemType",     Width = 80  });
            _hsItemGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName",     HeaderText = "Name",      DataPropertyName = "ItemName",     Width = 140 });
            _hsItemGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "Serial #",  DataPropertyName = "SerialNumber", Width = 120 });
            _hsItemGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location",     HeaderText = "Location",  DataPropertyName = "Location",     Width = 100 });
            _hsItemGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Condition",    HeaderText = "Condition", DataPropertyName = "Condition",    Width = 80  });
            _hsItemGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Available",    HeaderText = "Available", DataPropertyName = "AvailBadge",   Width = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _hsItemGrid.CellFormatting   += HsItemGridCellFormatting;
            _hsItemGrid.SelectionChanged += HsItemGridSelectionChanged;
            _hsItemGrid.DoubleClick      += (_, __) => EditHotSwapItem();

            var itemHeader = MakeHeader("🔌  HOT SWAP EQUIPMENT  —  monitors, keyboards, mice and other peripherals");
            var itemBody   = new Panel { Dock = DockStyle.Fill };
            itemBody.Controls.Add(_hsItemGrid);
            itemBody.Controls.Add(itemHeader);

            split.Panel2.Controls.Add(itemBody);
            split.Panel2.Controls.Add(botToolbar);

            page.Controls.Add(split);

            // Set splitter to mid-point once the control has a real size
            bool splitInit = false;
            split.SizeChanged += (_, __) =>
            {
                if (!splitInit && split.Height > 80)
                {
                    split.SplitterDistance = split.Height / 2;
                    splitInit = true;
                }
            };
        }

        private void LoadHotSwapData()
        {
            _allComputers = _db.GetAllComputers();
            _hotSwapItems = _db.GetAllHotSwapItems();

            // Hot swap computers grid (all computers with HotSwapAvailable=true)
            _hsCompGrid.DataSource = null;
            _hsCompGrid.DataSource = _allComputers
                .Where(c => c.HotSwapAvailable)
                .Select(c => new
                {
                    c.ComputerName, c.SerialNumber, c.Model, c.Location, c.Status
                }).ToList();

            // Hot swap items grid
            _hsItemGrid.DataSource = null;
            _hsItemGrid.DataSource = _hotSwapItems.Select(i => new
            {
                i.ItemType, i.ItemName, i.SerialNumber, i.Location, i.Condition,
                AvailBadge = i.Available ? "✓ Yes" : "✗ No"
            }).ToList();
        }

        private void HsCompGridSelectionChanged(object? s, EventArgs e)
        {
            if (_hsCompGrid.SelectedRows.Count == 0) return;
            var name = _hsCompGrid.SelectedRows[0].Cells["ComputerName"].Value?.ToString() ?? "";
            _selectedHotSwapComp = _allComputers.FirstOrDefault(c => c.ComputerName == name);
        }

        private void HsItemGridSelectionChanged(object? s, EventArgs e)
        {
            if (_hsItemGrid.SelectedRows.Count == 0) return;
            int idx = _hsItemGrid.SelectedRows[0].Index;
            var avail = _hotSwapItems.Where(i => true).ToList();
            if (idx >= 0 && idx < avail.Count)
                _selectedHotSwapItem = avail[idx];
        }

        private void HsItemGridCellFormatting(object? s, DataGridViewCellFormattingEventArgs e)
        {
            if (_hsItemGrid.Columns[e.ColumnIndex].Name == "Available" && e.Value != null)
                e.CellStyle.ForeColor = e.Value.ToString()!.StartsWith("✓") ? Green : Red;
            if (_hsItemGrid.Columns[e.ColumnIndex].Name == "Condition" && e.Value != null)
                e.CellStyle.ForeColor = e.Value.ToString() switch
                {
                    "Good" => Green, "Fair" => Orange, "Poor" => Red, _ => FgMain
                };
        }

        private void RemoveFromHotSwap()
        {
            if (_selectedHotSwapComp == null) { Info("Select a computer to remove from the hot swap pool."); return; }
            _selectedHotSwapComp.HotSwapAvailable = false;
            _db.UpdateComputer(_selectedHotSwapComp, AppSession.Username);
            _classroom.RefreshData();
            _serverRoom.RefreshData();
            LoadHotSwapData();
            UpdateGlobalStatus();
        }

        private void AddHotSwapItem()
        {
            using var f = new HotSwapItemForm(null);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            _db.AddHotSwapItem(f.Item, AppSession.Username);
            LoadHotSwapData();
        }

        private void EditHotSwapItem()
        {
            if (_selectedHotSwapItem == null) { Info("Select an item to edit."); return; }
            using var f = new HotSwapItemForm(_selectedHotSwapItem);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            _db.UpdateHotSwapItem(f.Item, AppSession.Username);
            LoadHotSwapData();
        }

        private void DeleteHotSwapItem()
        {
            if (_selectedHotSwapItem == null) { Info("Select an item to delete."); return; }
            if (MessageBox.Show($"Delete \"{_selectedHotSwapItem.ItemName}\"?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _db.DeleteHotSwapItem(_selectedHotSwapItem.HotSwapItemId, AppSession.Username);
            _selectedHotSwapItem = null;
            LoadHotSwapData();
        }

        // ─────────────────────────────────────────────────────────
        // ── ACTIVITY LOG TAB ──────────────────────────────────────
        // ─────────────────────────────────────────────────────────
        private void BuildActivityLogTab(TabPage page)
        {
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgMantle };
            var btnRefresh = new Button
            {
                Text = "↺  Refresh Log", AutoSize = false, Width = 130, Height = 30,
                BackColor = BgOverlay, ForeColor = FgMain, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Location = new Point(12, 8)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (_, __) => LoadActivityLog();
            toolbar.Controls.Add(btnRefresh);

            _logGrid = MakeGrid();
            _logGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp",  HeaderText = "Timestamp",    DataPropertyName = "TimestampFmt", Width = 140 });
            _logGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Username",   HeaderText = "User",         DataPropertyName = "Username",     Width = 100 });
            _logGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action",     HeaderText = "Action",       DataPropertyName = "Action",       Width = 180 });
            _logGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntityType", HeaderText = "Type",         DataPropertyName = "EntityType",   Width = 100 });
            _logGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EntityName", HeaderText = "Entity",       DataPropertyName = "EntityName",   Width = 130 });
            _logGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Detail",     HeaderText = "Detail",       DataPropertyName = "Detail",       Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            var header = MakeHeader("📋  ACTIVITY LOG  —  all changes made in the system (most recent first)");
            var body   = new Panel { Dock = DockStyle.Fill };
            body.Controls.Add(_logGrid);
            body.Controls.Add(header);

            page.Controls.Add(body);
            page.Controls.Add(toolbar);
        }

        private void LoadActivityLog()
        {
            var entries = _db.GetChangeLog(500);
            _logGrid.DataSource = null;
            _logGrid.DataSource = entries.Select(e => new
            {
                TimestampFmt = e.Timestamp.ToString("M/d/yyyy h:mm:ss tt"),
                e.Username, e.Action, e.EntityType, e.EntityName, e.Detail
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────
        // Shared helpers
        // ─────────────────────────────────────────────────────────
        private DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, ReadOnly = true,
                BackgroundColor = BgMantle, GridColor = BgOverlay,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                Font = new Font("Segoe UI", 8.5f),
                ColumnHeadersHeight = 28, RowTemplate = { Height = 26 }
            };
            g.DefaultCellStyle.BackColor             = BgMantle;
            g.DefaultCellStyle.ForeColor             = FgMain;
            g.DefaultCellStyle.SelectionBackColor    = BgSurface;
            g.DefaultCellStyle.SelectionForeColor    = FgMain;
            g.ColumnHeadersDefaultCellStyle.BackColor = BgSurface;
            g.ColumnHeadersDefaultCellStyle.ForeColor = FgMuted;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 54);
            return g;
        }

        private Panel MakeHeader(string text) => new Panel
        {
            Dock = DockStyle.Top, Height = 30, BackColor = BgBase,
            Controls =
            {
                new Label
                {
                    Text = text, ForeColor = FgMuted, AutoSize = true,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold), Location = new Point(12, 8)
                }
            }
        };

        private static void Info(string msg) =>
            MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
