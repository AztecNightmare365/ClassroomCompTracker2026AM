using ClassroomComputerTracker.Models;

namespace ClassroomComputerTracker
{
    public class ComputerEditForm : Form
    {
        public Computer Computer { get; private set; }

        static readonly Color BgBase    = Color.FromArgb(30,  30,  46);
        static readonly Color BgSurface = Color.FromArgb(49,  50,  68);
        static readonly Color BgOverlay = Color.FromArgb(69,  71,  90);
        static readonly Color Accent    = Color.FromArgb(203, 166, 247);
        static readonly Color FgMain    = Color.FromArgb(205, 214, 244);
        static readonly Color FgMuted   = Color.FromArgb(166, 173, 200);
        static readonly Color Orange    = Color.FromArgb(250, 179, 135);

        private TextBox  _txtName          = null!;
        private TextBox  _txtModel         = null!;
        private TextBox  _txtSerial        = null!;
        private ComboBox _cmbStatus        = null!;
        private TextBox  _txtBrokenReason  = null!;
        private Label    _lblBrokenReason  = null!;
        private ComboBox _cmbLocation      = null!;
        private CheckBox _chkHotSwap       = null!;
        private TextBox  _txtConnected     = null!;
        private TextBox  _txtNotes         = null!;
        private TableLayoutPanel _layout   = null!;

        public ComputerEditForm(Computer? computer, string defaultLocation = "Classroom")
        {
            Computer = computer != null
                ? new Computer
                {
                    ComputerId       = computer.ComputerId,
                    ComputerName     = computer.ComputerName,
                    Model            = computer.Model,
                    SerialNumber     = computer.SerialNumber,
                    ConnectedItems   = computer.ConnectedItems,
                    Notes            = computer.Notes,
                    Status           = computer.Status,
                    Location         = computer.Location,
                    BrokenReason     = computer.BrokenReason,
                    HotSwapAvailable = computer.HotSwapAvailable,
                    RoomPositionX    = computer.RoomPositionX,
                    RoomPositionY    = computer.RoomPositionY,
                    LastUpdated      = computer.LastUpdated,
                    LastModifiedBy   = computer.LastModifiedBy
                }
                : new Computer { Location = defaultLocation };

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            Text            = Computer.ComputerId == 0 ? "Add Computer" : $"Edit — {Computer.ComputerName}";
            Size            = new Size(500, 580);
            MinimumSize     = new Size(460, 560);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = BgBase;
            ForeColor       = FgMain;

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 10,
                Padding = new Padding(18, 16, 18, 8), BackColor = BgBase
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label Lbl(string t) => new Label
            {
                Text = t, AutoSize = true, ForeColor = FgMuted,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 0, 0)
            };
            TextBox Txt(bool multi = false) => new TextBox
            {
                Dock = DockStyle.Fill, Multiline = multi,
                BackColor = BgSurface, ForeColor = FgMain,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f)
            };

            _txtName    = Txt();
            _txtModel   = Txt();
            _txtSerial  = Txt();

            _cmbStatus = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f)
            };
            _cmbStatus.Items.AddRange(new object[] { "Active", "Inactive", "Broken", "Retired" });
            _cmbStatus.SelectedIndexChanged += (_, __) => UpdateBrokenVisibility();

            _lblBrokenReason = new Label
            {
                Text = "⚠ Broken Reason *", AutoSize = true, ForeColor = Orange,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 0, 0),
                Visible = false
            };
            _txtBrokenReason = new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true,
                BackColor = BgSurface, ForeColor = FgMain,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f),
                Visible = false
            };

            _cmbLocation = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f)
            };
            _cmbLocation.Items.AddRange(new object[] { "Classroom", "Server Room" });

            _chkHotSwap = new CheckBox
            {
                Text = "Available for Hot Swap", ForeColor = FgMain,
                Font = new Font("Segoe UI", 9f), AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 0, 0)
            };

            _txtConnected = Txt(true);
            _txtNotes     = Txt(true);

            // Row heights (must match the 9-row grid order below exactly)
            for (int i = 0; i < 4; i++) _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // rows 0-3: Name,Model,Serial,Status
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));  // row 4: BrokenReason (hidden initially, expanded when Broken)
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // row 5: Location
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // row 6: HotSwap checkbox
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78)); // row 7: Connected Items
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78)); // row 8: Notes

            int row = 0;
            _layout.Controls.Add(Lbl("Computer Name *"), 0, row); _layout.Controls.Add(_txtName,         1, row++);
            _layout.Controls.Add(Lbl("Model"),           0, row); _layout.Controls.Add(_txtModel,         1, row++);
            _layout.Controls.Add(Lbl("Serial Number"),   0, row); _layout.Controls.Add(_txtSerial,        1, row++);
            _layout.Controls.Add(Lbl("Status"),          0, row); _layout.Controls.Add(_cmbStatus,        1, row++);
            _layout.Controls.Add(_lblBrokenReason,       0, row); _layout.Controls.Add(_txtBrokenReason,  1, row++);
            _layout.Controls.Add(Lbl("Location"),        0, row); _layout.Controls.Add(_cmbLocation,      1, row++);
            _layout.Controls.Add(new Label(),            0, row); _layout.Controls.Add(_chkHotSwap,       1, row++);
            _layout.Controls.Add(Lbl("Connected Items"), 0, row); _layout.Controls.Add(_txtConnected,     1, row++);
            _layout.Controls.Add(Lbl("Notes"),           0, row); _layout.Controls.Add(_txtNotes,         1, row++);

            var btnBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50, BackColor = BgSurface,
                Padding = new Padding(12, 10, 12, 10)
            };
            var btnSave = new Button
            {
                Text = "Save", Width = 90, Height = 30,
                BackColor = Accent, ForeColor = BgBase, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Right | AnchorStyles.Top, DialogResult = DialogResult.OK
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (_, __) => Save();

            var btnCancel = new Button
            {
                Text = "Cancel", Width = 80, Height = 30,
                BackColor = BgOverlay, ForeColor = FgMain, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Right | AnchorStyles.Top, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            void PosBtns()
            {
                btnSave.Location   = new Point(btnBar.Width - 90  - 12, 10);
                btnCancel.Location = new Point(btnBar.Width - 178 - 12, 10);
            }
            PosBtns();
            btnBar.Resize += (_, __) => PosBtns();
            btnBar.Controls.AddRange(new Control[] { btnSave, btnCancel });

            Controls.Add(_layout);
            Controls.Add(btnBar);
        }

        private void LoadData()
        {
            _txtName.Text           = Computer.ComputerName;
            _txtModel.Text          = Computer.Model;
            _txtSerial.Text         = Computer.SerialNumber;
            _txtConnected.Text      = Computer.ConnectedItems;
            _txtNotes.Text          = Computer.Notes;
            _txtBrokenReason.Text   = Computer.BrokenReason;
            _chkHotSwap.Checked     = Computer.HotSwapAvailable;

            _cmbStatus.SelectedItem   = Computer.Status;
            if (_cmbStatus.SelectedIndex < 0) _cmbStatus.SelectedIndex = 0;

            _cmbLocation.SelectedItem = Computer.Location;
            if (_cmbLocation.SelectedIndex < 0) _cmbLocation.SelectedIndex = 0;

            UpdateBrokenVisibility();
        }

        private void UpdateBrokenVisibility()
        {
            bool isBroken = _cmbStatus.SelectedItem?.ToString() == "Broken";
            _lblBrokenReason.Visible  = isBroken;
            _txtBrokenReason.Visible  = isBroken;

            // Shift broken-reason row height so layout doesn't waste space when hidden
            _layout.RowStyles[4] = isBroken
                ? new RowStyle(SizeType.Absolute, 60)
                : new RowStyle(SizeType.Absolute, 0);
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Computer Name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            string status = _cmbStatus.SelectedItem?.ToString() ?? "Active";
            if (status == "Broken" && string.IsNullOrWhiteSpace(_txtBrokenReason.Text))
            {
                MessageBox.Show("Please enter a reason why this computer is broken.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            Computer.ComputerName     = _txtName.Text.Trim();
            Computer.Model            = _txtModel.Text.Trim();
            Computer.SerialNumber     = _txtSerial.Text.Trim();
            Computer.ConnectedItems   = _txtConnected.Text.Trim();
            Computer.Notes            = _txtNotes.Text.Trim();
            Computer.Status           = status;
            Computer.BrokenReason     = status == "Broken" ? _txtBrokenReason.Text.Trim() : "";
            Computer.Location         = _cmbLocation.SelectedItem?.ToString() ?? "Classroom";
            Computer.HotSwapAvailable = _chkHotSwap.Checked;
        }
    }
}
