using ClassroomComputerTracker.Data;

namespace ClassroomComputerTracker
{
    public class LoginForm : Form
    {
        static readonly Color BgBase    = Color.FromArgb(30,  30,  46);
        static readonly Color BgSurface = Color.FromArgb(49,  50,  68);
        static readonly Color BgOverlay = Color.FromArgb(69,  71,  90);
        static readonly Color Accent    = Color.FromArgb(203, 166, 247);
        static readonly Color FgMain    = Color.FromArgb(205, 214, 244);
        static readonly Color FgMuted   = Color.FromArgb(166, 173, 200);
        static readonly Color Green     = Color.FromArgb(166, 227, 161);
        static readonly Color Red       = Color.FromArgb(243, 139, 168);

        private readonly DatabaseManager _db;
        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private Label   _lblError    = null!;

        public LoginForm(DatabaseManager db)
        {
            _db = db;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = "Login — Classroom Computer Tracker";
            Size            = new Size(420, 360);
            MinimumSize     = new Size(400, 340);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = BgBase;
            ForeColor       = FgMain;

            var titleLbl = new Label
            {
                Text = "🖥  Classroom Computer Tracker",
                ForeColor = Accent, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true, Location = new Point(28, 24)
            };
            var subLbl = new Label
            {
                Text = "Sign in or create an account to continue",
                ForeColor = FgMuted, Font = new Font("Segoe UI", 9f),
                AutoSize = true, Location = new Point(30, 58)
            };

            var lblUser = new Label
            {
                Text = "Username", ForeColor = FgMuted, Font = new Font("Segoe UI", 9f),
                AutoSize = true, Location = new Point(30, 98)
            };
            _txtUsername = new TextBox
            {
                Location = new Point(30, 117), Width = 340, Height = 28,
                BackColor = BgSurface, ForeColor = FgMain,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f)
            };

            var lblPass = new Label
            {
                Text = "Password", ForeColor = FgMuted, Font = new Font("Segoe UI", 9f),
                AutoSize = true, Location = new Point(30, 158)
            };
            _txtPassword = new TextBox
            {
                Location = new Point(30, 177), Width = 340, Height = 28,
                BackColor = BgSurface, ForeColor = FgMain,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f),
                PasswordChar = '●'
            };
            _txtPassword.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };

            _lblError = new Label
            {
                Text = "", ForeColor = Red, Font = new Font("Segoe UI", 8.5f),
                AutoSize = false, Width = 340, Height = 20, Location = new Point(30, 214)
            };

            var btnLogin = new Button
            {
                Text = "Login", Location = new Point(30, 242),
                Width = 158, Height = 36,
                BackColor = Accent, ForeColor = BgBase, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (_, __) => TryLogin();

            var btnRegister = new Button
            {
                Text = "Create Account", Location = new Point(200, 242),
                Width = 170, Height = 36,
                BackColor = BgOverlay, ForeColor = FgMain, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += (_, __) => TryRegister();

            Controls.AddRange(new Control[]
            {
                titleLbl, subLbl, lblUser, _txtUsername,
                lblPass, _txtPassword, _lblError, btnLogin, btnRegister
            });

            AcceptButton = btnLogin;
        }

        private void TryLogin()
        {
            _lblError.Text = "";
            if (string.IsNullOrWhiteSpace(_txtUsername.Text) ||
                string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                _lblError.Text = "Please enter a username and password.";
                return;
            }
            var user = _db.ValidateUser(_txtUsername.Text.Trim(), _txtPassword.Text);
            if (user == null)
            {
                _lblError.Text = "Invalid username or password.";
                return;
            }
            AppSession.UserId   = user.UserId;
            AppSession.Username = user.Username;
            DialogResult = DialogResult.OK;
        }

        private void TryRegister()
        {
            _lblError.Text = "";
            var username = _txtUsername.Text.Trim();
            var password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _lblError.Text = "Please enter a username and password.";
                return;
            }
            if (password.Length < 4)
            {
                _lblError.Text = "Password must be at least 4 characters.";
                return;
            }
            if (!_db.CreateUser(username, password))
            {
                _lblError.Text = "Username already taken.";
                return;
            }
            var user = _db.ValidateUser(username, password)!;
            AppSession.UserId   = user.UserId;
            AppSession.Username = user.Username;
            DialogResult = DialogResult.OK;
        }
    }
}
