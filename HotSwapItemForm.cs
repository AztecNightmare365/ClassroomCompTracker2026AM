using ClassroomComputerTracker.Models;

namespace ClassroomComputerTracker
{
    public class HotSwapItemForm : Form
    {
        public HotSwapItem Item { get; private set; }

        static readonly Color BgBase    = Color.FromArgb(30,  30,  46);
        static readonly Color BgSurface = Color.FromArgb(49,  50,  68);
        static readonly Color BgOverlay = Color.FromArgb(69,  71,  90);
        static readonly Color Accent    = Color.FromArgb(203, 166, 247);
        static readonly Color FgMain    = Color.FromArgb(205, 214, 244);
        static readonly Color FgMuted   = Color.FromArgb(166, 173, 200);

        private TextBox  _txtName      = null!;
        private ComboBox _cmbType      = null!;
        private TextBox  _txtSerial    = null!;
        private TextBox  _txtLocation  = null!;
        private ComboBox _cmbCondition = null!;
        private CheckBox _chkAvailable = null!;
        private TextBox  _txtNotes     = null!;

        public HotSwapItemForm(HotSwapItem? item)
        {
            Item = item != null
                ? new HotSwapItem
                {
                    HotSwapItemId  = item.HotSwapItemId,
                    ItemName       = item.ItemName,
                    ItemType       = item.ItemType,
                    SerialNumber   = item.SerialNumber,
                    Location       = item.Location,
                    Condition      = item.Condition,
                    Available      = item.Available,
                    Notes          = item.Notes,
                    LastModifiedBy = item.LastModifiedBy
                }
                : new HotSwapItem();

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            bool isNew = Item.HotSwapItemId == 0;
            Text            = isNew ? "Add Hot Swap Item" : $"Edit — {Item.ItemName}";
            Size            = new Size(480, 430);
            MinimumSize     = new Size(440, 410);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = BgBase;
            ForeColor       = FgMain;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7,
                Padding = new Padding(18, 16, 18, 8), BackColor = BgBase
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Row heights for 7 rows
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Available checkbox
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); // Notes (multiline)

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

            _txtName     = Txt();
            _txtSerial   = Txt();
            _txtLocation = Txt();
            _txtNotes    = Txt(true);

            _cmbType = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f)
            };
            _cmbType.Items.AddRange(new object[] { "Monitor", "Keyboard", "Mouse", "Other" });

            _cmbCondition = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgSurface, ForeColor = FgMain, Font = new Font("Segoe UI", 9f)
            };
            _cmbCondition.Items.AddRange(new object[] { "Good", "Fair", "Poor" });

            _chkAvailable = new CheckBox
            {
                Text = "Available for hot swap", ForeColor = FgMain,
                Font = new Font("Segoe UI", 9f), AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 0, 0)
            };

            int row = 0;
            layout.Controls.Add(Lbl("Item Name *"), 0, row); layout.Controls.Add(_txtName,      1, row++);
            layout.Controls.Add(Lbl("Type"),        0, row); layout.Controls.Add(_cmbType,       1, row++);
            layout.Controls.Add(Lbl("Serial #"),    0, row); layout.Controls.Add(_txtSerial,     1, row++);
            layout.Controls.Add(Lbl("Location"),    0, row); layout.Controls.Add(_txtLocation,   1, row++);
            layout.Controls.Add(Lbl("Condition"),   0, row); layout.Controls.Add(_cmbCondition,  1, row++);
            layout.Controls.Add(new Label(),        0, row); layout.Controls.Add(_chkAvailable,  1, row++);
            layout.Controls.Add(Lbl("Notes"),       0, row); layout.Controls.Add(_txtNotes,       1, row++);

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

            Controls.Add(layout);
            Controls.Add(btnBar);
        }

        private void LoadData()
        {
            _txtName.Text     = Item.ItemName;
            _txtSerial.Text   = Item.SerialNumber;
            _txtLocation.Text = Item.Location;
            _txtNotes.Text    = Item.Notes;

            _cmbType.SelectedItem = Item.ItemType;
            if (_cmbType.SelectedIndex < 0) _cmbType.SelectedIndex = 0;

            _cmbCondition.SelectedItem = Item.Condition;
            if (_cmbCondition.SelectedIndex < 0) _cmbCondition.SelectedIndex = 0;

            _chkAvailable.Checked = Item.Available;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Item Name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            Item.ItemName      = _txtName.Text.Trim();
            Item.ItemType      = _cmbType.SelectedItem?.ToString() ?? "Monitor";
            Item.SerialNumber  = _txtSerial.Text.Trim();
            Item.Location      = _txtLocation.Text.Trim();
            Item.Condition     = _cmbCondition.SelectedItem?.ToString() ?? "Good";
            Item.Available     = _chkAvailable.Checked;
            Item.Notes         = _txtNotes.Text.Trim();
        }
    }
}
