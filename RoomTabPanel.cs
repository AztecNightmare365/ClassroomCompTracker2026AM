using ClassroomComputerTracker.Data;
using ClassroomComputerTracker.Models;

namespace ClassroomComputerTracker
{
    /// <summary>
    /// Self-contained panel that hosts a full computer room view:
    /// left side = grid + detail card, right side = draggable room canvas.
    /// One instance per location (Classroom, Server Room).
    /// </summary>
    public class RoomTabPanel : UserControl
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

        // ── Config ───────────────────────────────────────────────
        private readonly string          _location;
        private readonly DatabaseManager _db;
        private const int TW = 90, TH = 64;

        // ── State ────────────────────────────────────────────────
        private List<Computer> _computers = new();
        private Computer?      _selected;
        private int  _dragId     = -1;
        private bool _dragging;
        private int  _dragOffX, _dragOffY;
        private int  _roomW = 900, _roomH = 540;
        private bool _splitterSet = false;

        // ── Controls ─────────────────────────────────────────────
        private DataGridView   _grid      = null!;
        private Panel          _room      = null!;
        private Panel          _detail    = null!;
        private Label          _statusLbl = null!;
        private NumericUpDown  _nudW      = null!;
        private NumericUpDown  _nudH      = null!;
        private SplitContainer _split     = null!;

        /// <summary>Raised after any Add / Edit / Delete so siblings can reload.</summary>
        public event EventHandler? DataChanged;

        public RoomTabPanel(string location, DatabaseManager db)
        {
            _location = location;
            _db       = db;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadComputers();
        }

        // ─────────────────────────────────────────────────────────
        // UI construction
        // ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            BackColor = BgBase;

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgMantle };

            Button Btn(string label, Color bg, Color fg = default)
            {
                if (fg == default) fg = BgBase;
                var b = new Button
                {
                    Text = label, AutoSize = false, Width = 110, Height = 30,
                    BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                    Margin = new Padding(0)
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

            var btnAdd      = Btn("＋  Add",          Green);
            var btnEdit     = Btn("✏  Edit",          Blue);
            var btnDelete   = Btn("✕  Delete",        Red);
            var btnHotSwap  = Btn("🔄  Hot Swap",     Yellow);
            btnHotSwap.ForeColor = BgBase;
            var btnRefresh  = Btn("↺  Refresh",       BgOverlay, FgMain);

            btnAdd.Click     += (_, __) => AddComputer();
            btnEdit.Click    += (_, __) => EditComputer();
            btnDelete.Click  += (_, __) => DeleteComputer();
            btnHotSwap.Click += (_, __) => ToggleHotSwap();
            btnRefresh.Click += (_, __) => LoadComputers();

            btnFlow.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnHotSwap, btnRefresh });

            // Room dimension controls (right side)
            var roomFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(0, 8, 12, 0), BackColor = BgMantle
            };
            Label RoomLbl(string t) => new Label
            {
                Text = t, ForeColor = FgMuted, Font = new Font("Segoe UI", 8.5f),
                AutoSize = true, Margin = new Padding(4, 4, 2, 0)
            };
            _nudW = Nud(900); _nudH = Nud(540);
            _nudW.Margin = _nudH.Margin = new Padding(2, 0, 2, 0);

            var btnApply = Btn("Apply", Color.FromArgb(249, 226, 175));
            btnApply.ForeColor = BgBase;
            btnApply.Width = 72;
            btnApply.Margin = new Padding(6, 0, 0, 0);
            btnApply.Click += (_, __) =>
            {
                _roomW = (int)_nudW.Value;
                _roomH = (int)_nudH.Value;
                _room.Size = new Size(_roomW, _roomH);
                _room.Invalidate();
            };

            roomFlow.Controls.Add(RoomLbl("Width"));
            roomFlow.Controls.Add(_nudW);
            roomFlow.Controls.Add(RoomLbl("×"));
            roomFlow.Controls.Add(_nudH);
            roomFlow.Controls.Add(RoomLbl("(px)"));
            roomFlow.Controls.Add(btnApply);

            toolbar.Controls.Add(roomFlow);
            toolbar.Controls.Add(btnFlow);

            // Main split: left = list + detail, right = room canvas
            _split = new SplitContainer
            {
                Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1, BackColor = BgBase
            };
            _split.Panel1.BackColor = BgMantle;
            _split.Panel2.BackColor = BgBase;

            // LEFT — header + grid + detail card
            var leftHeader = MakeHeader($"COMPUTERS — {_location.ToUpper()}");

            _grid = new DataGridView
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
            _grid.DefaultCellStyle.BackColor             = BgMantle;
            _grid.DefaultCellStyle.ForeColor             = FgMain;
            _grid.DefaultCellStyle.SelectionBackColor    = BgSurface;
            _grid.DefaultCellStyle.SelectionForeColor    = FgMain;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = BgSurface;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = FgMuted;
            _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8f, FontStyle.Bold);
            _grid.EnableHeadersVisualStyles = false;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 54);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ComputerId",   HeaderText = "#",        DataPropertyName = "ComputerId",   Width = 36  });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ComputerName", HeaderText = "Name",     DataPropertyName = "ComputerName", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model",        HeaderText = "Model",    DataPropertyName = "Model",        Width = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",       HeaderText = "Status",   DataPropertyName = "Status",       Width = 70  });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HotSwap",      HeaderText = "HotSwap",  DataPropertyName = "HotSwapBadge", Width = 66, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            _grid.CellFormatting += GridCellFormatting;
            _grid.SelectionChanged += GridSelectionChanged;
            _grid.DoubleClick      += (_, __) => EditComputer();

            _detail = new Panel
            {
                Dock = DockStyle.Bottom, Height = 165,
                BackColor = BgSurface, Padding = new Padding(12, 10, 12, 10)
            };
            _detail.Paint += DetailPaint;

            var leftStack = new Panel { Dock = DockStyle.Fill };
            leftStack.Controls.Add(_grid);
            leftStack.Controls.Add(leftHeader);
            _split.Panel1.Controls.Add(_detail);
            _split.Panel1.Controls.Add(leftStack);

            // RIGHT — header + scrollable room canvas
            var rightHeader = MakeHeader($"{_location.ToUpper()} LAYOUT  —  drag to reposition");
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BgBase };

            _room = new Panel
            {
                Width = _roomW, Height = _roomH,
                BackColor = Color.FromArgb(248, 249, 255),
                Location = new Point(16, 16)
            };
            _room.Paint     += RoomPaint;
            _room.MouseDown += RoomMouseDown;
            _room.MouseMove += RoomMouseMove;
            _room.MouseUp   += RoomMouseUp;

            scroll.Controls.Add(_room);
            _split.Panel2.Controls.Add(scroll);
            _split.Panel2.Controls.Add(rightHeader);

            // Status bar
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = BgMantle };
            _statusLbl = new Label
            {
                AutoSize = false, Dock = DockStyle.Fill, ForeColor = FgMuted,
                Font = new Font("Segoe UI", 8.5f), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            statusBar.Controls.Add(_statusLbl);

            Controls.Add(_split);
            Controls.Add(statusBar);
            Controls.Add(toolbar);

            // Defer splitter distance until after first layout pass gives us a real Width
            _split.SizeChanged += (_, __) =>
            {
                if (!_splitterSet && _split.Width > 500)
                {
                    _split.SplitterDistance = 380;
                    _splitterSet = true;
                }
            };
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

        private NumericUpDown Nud(int val) => new NumericUpDown
        {
            Minimum = 200, Maximum = 4000, Value = val, Width = 70,
            BackColor = BgSurface, ForeColor = FgMain, BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8.5f), TextAlign = HorizontalAlignment.Center
        };

        // ─────────────────────────────────────────────────────────
        // Data
        // ─────────────────────────────────────────────────────────
        public void RefreshData() => LoadComputers();

        private void LoadComputers()
        {
            try
            {
                _computers = _db.GetComputersByLocation(_location);
                RebindGrid();
                _room.Invalidate();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading computers: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RebindGrid()
        {
            _grid.DataSource = null;
            _grid.DataSource = _computers.Select(c => new
            {
                c.ComputerId,
                c.ComputerName,
                c.Model,
                c.Status,
                HotSwapBadge = c.HotSwapAvailable ? "🔄 Yes" : ""
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────
        // CRUD
        // ─────────────────────────────────────────────────────────
        private void AddComputer()
        {
            using var f = new ComputerEditForm(null, _location);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            _db.AddComputer(f.Computer, AppSession.Username);
            LoadComputers();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EditComputer()
        {
            if (_selected == null) { Info("Select a computer to edit."); return; }
            using var f = new ComputerEditForm(_selected);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            f.Computer.RoomPositionX = _selected.RoomPositionX;
            f.Computer.RoomPositionY = _selected.RoomPositionY;
            _db.UpdateComputer(f.Computer, AppSession.Username);
            LoadComputers();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DeleteComputer()
        {
            if (_selected == null) { Info("Select a computer to delete."); return; }
            if (MessageBox.Show($"Delete \"{_selected.ComputerName}\"?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _db.DeleteComputer(_selected.ComputerId, AppSession.Username);
            _selected = null;
            LoadComputers();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleHotSwap()
        {
            if (_selected == null) { Info("Select a computer to toggle hot swap."); return; }
            _selected.HotSwapAvailable = !_selected.HotSwapAvailable;
            _db.UpdateComputer(_selected, AppSession.Username);
            string msg = _selected.HotSwapAvailable
                ? $"{_selected.ComputerName} marked as available for hot swap."
                : $"{_selected.ComputerName} removed from hot swap pool.";
            LoadComputers();
            DataChanged?.Invoke(this, EventArgs.Empty);
            // Re-select
            if (_computers.FirstOrDefault(c => c.ComputerId == _selected.ComputerId) is { } updated)
                _selected = updated;
            MessageBox.Show(msg, "Hot Swap", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ─────────────────────────────────────────────────────────
        // Grid events
        // ─────────────────────────────────────────────────────────
        private void GridSelectionChanged(object? s, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0 || _computers == null) return;
            var cell = _grid.SelectedRows[0].Cells["ComputerId"];
            if (cell.Value == null) return;
            _selected = _computers.FirstOrDefault(c => c.ComputerId == Convert.ToInt32(cell.Value));
            _room.Invalidate();
            _detail.Invalidate();
            UpdateStatus();
        }

        private void GridCellFormatting(object? s, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name != "Status" || e.Value == null) return;
            e.CellStyle.ForeColor = e.Value.ToString() switch
            {
                "Active"   => Green,
                "Inactive" => Orange,
                "Broken"   => Yellow,
                "Retired"  => Red,
                _          => FgMain
            };
        }

        // ─────────────────────────────────────────────────────────
        // Room drawing
        // ─────────────────────────────────────────────────────────
        private void RoomPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(248, 249, 255));

            // Grid lines
            using var gridPen = new Pen(Color.FromArgb(30, 100, 100, 180));
            for (int x = 0; x < _roomW; x += 50) g.DrawLine(gridPen, x, 0, x, _roomH);
            for (int y = 0; y < _roomH; y += 50) g.DrawLine(gridPen, 0, y, _roomW, y);

            // Room watermark
            using var wFont = new Font("Segoe UI", 10f, FontStyle.Italic);
            g.DrawString(_location, wFont, new SolidBrush(Color.FromArgb(60, 100, 100, 150)), 8, 4);

            if (_computers == null) return;

            foreach (var pc in _computers)
            {
                var r     = new Rectangle(pc.RoomPositionX, pc.RoomPositionY, TW, TH);
                bool isSel = _selected?.ComputerId == pc.ComputerId;

                Color fill = pc.Status switch
                {
                    "Inactive" => Color.FromArgb(253, 232, 216),
                    "Retired"  => Color.FromArgb(253, 212, 212),
                    "Broken"   => Color.FromArgb(255, 245, 200), // amber for "needs repair"
                    _          => Color.FromArgb(212, 245, 212)
                };
                Color border = pc.Status switch
                {
                    "Inactive" => Color.FromArgb(250, 179, 135),
                    "Retired"  => Color.FromArgb(243, 139, 168),
                    "Broken"   => Color.FromArgb(240, 190,  50),
                    _          => Color.FromArgb(166, 227, 161)
                };

                using var fillBrush = new SolidBrush(fill);
                using var borderPen = new Pen(isSel ? Color.FromArgb(137, 180, 250) : border,
                                              isSel ? 2.5f : 1.5f);

                DrawRoundRect(g, fillBrush, r, 6);
                DrawRoundRectBorder(g, borderPen, r, 6);

                if (isSel)
                {
                    using var glowPen = new Pen(Color.FromArgb(80, 137, 180, 250), 4f);
                    DrawRoundRectBorder(g, glowPen, Rectangle.Inflate(r, 2, 2), 7);
                }

                // Icon: ⚠ for broken, 🖥 otherwise
                string icon = pc.Status == "Broken" ? "⚠" : "🖥";
                using var iconFont  = new Font("Segoe UI", 14f);
                var iconRect = new RectangleF(r.X, r.Y + 4, TW, 22);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(icon, iconFont, Brushes.Black, iconRect, sf);

                // Computer name
                using var nameFont  = new Font("Segoe UI", 8f, FontStyle.Bold);
                using var nameBrush = new SolidBrush(Color.FromArgb(30, 30, 46));
                g.DrawString(pc.ComputerName, nameFont, nameBrush,
                    new RectangleF(r.X, r.Y + 30, TW, 16), sf);

                // Sub-label: broken reason (truncated) or model
                string subText = pc.Status == "Broken" && !string.IsNullOrWhiteSpace(pc.BrokenReason)
                    ? (pc.BrokenReason.Length > 12 ? pc.BrokenReason[..12] + "…" : pc.BrokenReason)
                    : (!string.IsNullOrWhiteSpace(pc.Model)
                        ? (pc.Model.Length > 10 ? pc.Model[..10] : pc.Model)
                        : "");
                if (!string.IsNullOrEmpty(subText))
                {
                    using var subFont  = new Font("Segoe UI", 7f);
                    using var subBrush = new SolidBrush(
                        pc.Status == "Broken" ? Color.FromArgb(180, 120, 0) : Color.FromArgb(80, 30, 30, 46));
                    g.DrawString(subText, subFont, subBrush,
                        new RectangleF(r.X, r.Y + 46, TW, 14), sf);
                }

                // Hot swap badge (small 🔄 in top-right corner)
                if (pc.HotSwapAvailable)
                {
                    using var badgeFont = new Font("Segoe UI", 8f);
                    g.DrawString("🔄", badgeFont, Brushes.DarkGreen,
                        new PointF(r.Right - 18, r.Y + 2));
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // Room mouse — drag
        // ─────────────────────────────────────────────────────────
        private void RoomMouseDown(object? s, MouseEventArgs e)
        {
            foreach (var pc in _computers)
            {
                var r = new Rectangle(pc.RoomPositionX, pc.RoomPositionY, TW, TH);
                if (!r.Contains(e.Location)) continue;
                _dragId   = pc.ComputerId;
                _dragging = true;
                _dragOffX = e.X - pc.RoomPositionX;
                _dragOffY = e.Y - pc.RoomPositionY;
                _selected = pc;
                SyncGridSelection(pc.ComputerId);
                _room.Invalidate();
                _detail.Invalidate();
                UpdateStatus();
                return;
            }
        }

        private void RoomMouseMove(object? s, MouseEventArgs e)
        {
            if (!_dragging || _dragId < 0) return;
            var pc = _computers.FirstOrDefault(c => c.ComputerId == _dragId);
            if (pc == null) return;
            pc.RoomPositionX = Math.Clamp(e.X - _dragOffX, 0, _roomW - TW);
            pc.RoomPositionY = Math.Clamp(e.Y - _dragOffY, 0, _roomH - TH);
            _room.Invalidate();
        }

        private void RoomMouseUp(object? s, MouseEventArgs e)
        {
            if (_dragging && _dragId >= 0)
            {
                var pc = _computers.FirstOrDefault(c => c.ComputerId == _dragId);
                if (pc != null) _db.UpdateComputerPosition(pc); // position-only, not logged
            }
            _dragging = false;
            _dragId   = -1;
        }

        private void SyncGridSelection(int computerId)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Cells["ComputerId"].Value != null &&
                    Convert.ToInt32(row.Cells["ComputerId"].Value) == computerId)
                {
                    _grid.ClearSelection();
                    row.Selected = true;
                    if (row.Index >= 0) _grid.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // Detail card
        // ─────────────────────────────────────────────────────────
        private void DetailPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BgSurface);

            if (_selected == null)
            {
                using var f = new Font("Segoe UI", 9f, FontStyle.Italic);
                g.DrawString("Select a computer to view details", f,
                    new SolidBrush(FgMuted), new Point(12, 12));
                return;
            }

            var pc = _selected;
            int y = 10;
            const int lx = 12, vx = 120;
            int lw = _detail.Width - vx - 12;

            using var titleFont  = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var lblFont    = new Font("Segoe UI", 8f);
            using var valFont    = new Font("Segoe UI", 8.5f);
            using var titleBrush = new SolidBrush(Accent);
            using var lblBrush   = new SolidBrush(FgMuted);
            using var valBrush   = new SolidBrush(FgMain);

            string titleIcon = pc.Status == "Broken" ? "⚠" : "🖥";
            g.DrawString($"{titleIcon}  {pc.ComputerName}", titleFont, titleBrush, lx, y); y += 22;

            void Row(string label, string val, Color? valColor = null)
            {
                g.DrawString(label, lblFont, lblBrush, lx, y);
                using var vb = new SolidBrush(valColor ?? FgMain);
                g.DrawString(val, valFont, vb, vx, y);
                y += 18;
            }

            Row("Model",       string.IsNullOrWhiteSpace(pc.Model)         ? "—" : pc.Model);
            Row("Serial #",    string.IsNullOrWhiteSpace(pc.SerialNumber)   ? "—" : pc.SerialNumber);
            Row("Status",      pc.Status, pc.Status switch
            {
                "Active"   => Green,  "Inactive" => Orange,
                "Broken"   => Yellow, "Retired"  => Red, _ => FgMain
            });

            // Broken reason — shown prominently in amber when broken
            if (pc.Status == "Broken" && !string.IsNullOrWhiteSpace(pc.BrokenReason))
                Row("Broken Reason", pc.BrokenReason, Color.FromArgb(240, 190, 50));

            Row("Hot Swap",    pc.HotSwapAvailable ? "🔄 Available" : "—");
            Row("Modified by", string.IsNullOrWhiteSpace(pc.LastModifiedBy) ? "—" : pc.LastModifiedBy
                + "  on  " + pc.LastUpdated.ToString("M/d/yyyy h:mm tt"));

            if (!string.IsNullOrWhiteSpace(pc.ConnectedItems))
                Row("Connected", pc.ConnectedItems);

            if (!string.IsNullOrWhiteSpace(pc.Notes))
            {
                g.DrawString("Notes", lblFont, lblBrush, lx, y);
                g.DrawString(pc.Notes, valFont, valBrush,
                    new RectangleF(vx, y, lw, _detail.Height - y - 8));
            }
        }

        // ─────────────────────────────────────────────────────────
        // Status bar
        // ─────────────────────────────────────────────────────────
        private void UpdateStatus()
        {
            int total    = _computers.Count;
            int active   = _computers.Count(c => c.Status == "Active");
            int inactive = _computers.Count(c => c.Status == "Inactive");
            int broken   = _computers.Count(c => c.Status == "Broken");
            int retired  = _computers.Count(c => c.Status == "Retired");
            string sel   = _selected != null
                ? $"   ●  Selected: {_selected.ComputerName}  —  {_selected.Model}"
                : "";
            _statusLbl.Text =
                $"● Active: {active}   ● Inactive: {inactive}   ● Broken: {broken}" +
                $"   ● Retired: {retired}   Total: {total}{sel}";
        }

        // ─────────────────────────────────────────────────────────
        // Drawing helpers
        // ─────────────────────────────────────────────────────────
        private static void DrawRoundRect(Graphics g, Brush b, Rectangle r, int rad)
        {
            using var path = RoundedPath(r, rad);
            g.FillPath(b, path);
        }
        private static void DrawRoundRectBorder(Graphics g, Pen p, Rectangle r, int rad)
        {
            using var path = RoundedPath(r, rad);
            g.DrawPath(p, path);
        }
        private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X,          r.Y,          rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad * 2, r.Y,          rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2,   0, 90);
            p.AddArc(r.X,          r.Bottom - rad * 2, rad * 2, rad * 2,  90, 90);
            p.CloseFigure();
            return p;
        }

        private static void Info(string msg) =>
            MessageBox.Show(msg, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
