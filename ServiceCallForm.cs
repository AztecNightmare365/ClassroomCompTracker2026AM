using ClassroomComputerTracker.Models;

namespace ClassroomComputerTracker
{
    public class ServiceCallForm : Form
    {
        public ServiceCall ServiceCall { get; private set; }

        static readonly Color BgBase    = Color.FromArgb(30,  30,  46);
        static readonly Color BgSurface = Color.FromArgb(49,  50,  68);
        static readonly Color BgOverlay = Color.FromArgb(69,  71,  90);
        static readonly Color Accent    = Color.FromArgb(203, 166, 247);
        static readonly Color FgMain    = Color.FromArgb(205, 214, 244);
        static readonly Color FgMuted   = Color.FromArgb(166, 173, 200);

        private readonly List<Computer> _computers;
        private ComboBox _cmbComputer  = null!;
        private TextBox  _txtDesc      = null!;
        private ComboBox _cmbStatus    = null!;
        private TextBox  _txtSolution  = null!;
        private Label    _lblSolHeader = null!;

        public ServiceCallForm(ServiceCall? call, List<Computer> computers)
        {
            _computers = computers;
            ServiceCall = call != null
                ? new ServiceCall
                {
                    ServiceCallId  = call.ServiceCallId,
                    ComputerId     = call.ComputerId,
                    ComputerName   = call.ComputerName,
                    Description    = call.Description,
                    Status         = call.Status,
                    SolutionNotes  = call.SolutionNotes,
                    OpenedAt       = call.OpenedAt,
                    ClosedAt       = call.ClosedAt,
                    OpenedByUser   = call.OpenedByUser,
                    ResolvedByUser = call.ResolvedByUser
                }
                : new ServiceCall
                {
                    OpenedByUser = AppSession.Username,
                    OpenedAt     = DateTime.Now
                };

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            bool isNew = ServiceCall.ServiceCallId == 0;
            Text            = isNew ? "New Service Call" : $"Edit Service Call #{ServiceCall.ServiceCallId}";
            Size            = new Size(540, 500);
            MinimumSize     = new Size(500, 480);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = BgBase;
            ForeColor       = FgMain;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
                Padding = new Padding(18, 16, 18, 8), BackColor = BgBase
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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

            _cmbComputer = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f)
            };
            foreach (var c in _computers)
                _cmbComputer.Items.Add($"{c.ComputerName}  [{c.Location}]  SN:{c.SerialNumber}");

            _cmbStatus = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f)
            };
            _cmbStatus.Items.AddRange(new object[] { "Open", "In Progress", "Resolved" });
            _cmbStatus.SelectedIndexChanged += (_, __) => UpdateSolutionVisibility();

            _txtDesc     = Txt(true);
            _txtSolution = Txt(true);
            _lblSolHeader = Lbl("Solution Notes");

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));

            int row = 0;
            layout.Controls.Add(Lbl("Computer *"),    0, row); layout.Controls.Add(_cmbComputer,  1, row++);
            layout.Controls.Add(Lbl("Description *"), 0, row); layout.Controls.Add(_txtDesc,       1, row++);
            layout.Controls.Add(Lbl("Status"),        0, row); layout.Controls.Add(_cmbStatus,     1, row++);
            layout.Controls.Add(_lblSolHeader,        0, row); layout.Controls.Add(_txtSolution,   1, row++);

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

            void PositionBtns()
            {
                btnSave.Location   = new Point(btnBar.Width - 90  - 12, 10);
                btnCancel.Location = new Point(btnBar.Width - 178 - 12, 10);
            }
            PositionBtns();
            btnBar.Resize += (_, __) => PositionBtns();
            btnBar.Controls.AddRange(new Control[] { btnSave, btnCancel });

            Controls.Add(layout);
            Controls.Add(btnBar);
        }

        private void LoadData()
        {
            for (int i = 0; i < _computers.Count; i++)
            {
                if (_computers[i].ComputerId == ServiceCall.ComputerId)
                {
                    _cmbComputer.SelectedIndex = i;
                    break;
                }
            }
            if (_cmbComputer.SelectedIndex < 0 && _cmbComputer.Items.Count > 0)
                _cmbComputer.SelectedIndex = 0;

            _txtDesc.Text          = ServiceCall.Description;
            _cmbStatus.SelectedItem = ServiceCall.Status;
            if (_cmbStatus.SelectedIndex < 0) _cmbStatus.SelectedIndex = 0;
            _txtSolution.Text      = ServiceCall.SolutionNotes;
            UpdateSolutionVisibility();
        }

        private void UpdateSolutionVisibility()
        {
            bool resolved = _cmbStatus.SelectedItem?.ToString() == "Resolved";
            _txtSolution.Enabled     = resolved;
            _lblSolHeader.ForeColor  = resolved ? FgMuted : Color.FromArgb(100, 130, 140, 160);
        }

        private void Save()
        {
            if (_cmbComputer.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a computer.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtDesc.Text))
            {
                MessageBox.Show("Description is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            var comp = _computers[_cmbComputer.SelectedIndex];
            ServiceCall.ComputerId    = comp.ComputerId;
            ServiceCall.ComputerName  = comp.ComputerName;
            ServiceCall.Description   = _txtDesc.Text.Trim();
            ServiceCall.Status        = _cmbStatus.SelectedItem?.ToString() ?? "Open";
            ServiceCall.SolutionNotes = _txtSolution.Text.Trim();

            if (ServiceCall.Status == "Resolved" && !ServiceCall.ClosedAt.HasValue)
            {
                ServiceCall.ClosedAt       = DateTime.Now;
                ServiceCall.ResolvedByUser = AppSession.Username;
            }
        }
    }
}
