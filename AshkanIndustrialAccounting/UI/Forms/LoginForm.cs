using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;
namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class LoginForm : Form
    {
        private readonly PlaceholderTextBox _user=new PlaceholderTextBox(), _pass=new PlaceholderTextBox();
        private readonly Label _error=new Label();
        public LoginForm()
        {
            Text="Ashkan Industrial Accounting — Sign in"; StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(480,560); BackColor=Color.FromArgb(245,248,252); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
            var card=new Panel{BackColor=Color.White,Size=new Size(360,390),Location=new Point(52,70),Padding=new Padding(32)};
            var logo=new Label{Text="AIA",Font=new Font("Segoe UI Semibold",24),ForeColor=Color.FromArgb(25,99,180),Dock=DockStyle.Top,Height=52,TextAlign=ContentAlignment.MiddleCenter};
            var title=new Label{Text="Industrial Accounting Suite",Font=new Font("Segoe UI Semibold",16),Dock=DockStyle.Top,Height=54,TextAlign=ContentAlignment.MiddleCenter};
            _user.Placeholder="Username";  _user.Font=new Font("Segoe UI",11); _user.Dock=DockStyle.Top; _user.Height=38;
            _pass.Placeholder="Password"; _pass.UseSystemPasswordChar=true; _pass.Font=new Font("Segoe UI",11); _pass.Dock=DockStyle.Top; _pass.Height=38;
            var gap1=new Panel{Height=18,Dock=DockStyle.Top}; var gap2=new Panel{Height=12,Dock=DockStyle.Top};
            var btn=new Button{Text=SecurityService.RequiresSetup ? "Create administrator" : "Sign in",Dock=DockStyle.Top,Height=44,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(25,99,180),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",11)};
            btn.FlatAppearance.BorderSize=0; btn.Click+=(s,e)=>Login();
            _error.Dock=DockStyle.Top; _error.Height=40; _error.ForeColor=Color.Firebrick; _error.TextAlign=ContentAlignment.MiddleCenter;
            var hint=new Label{Text=SecurityService.RequiresSetup ? "First run: create a strong administrator password" : "Sign in with your administrator account",Dock=DockStyle.Bottom,Height=34,ForeColor=Color.DimGray,TextAlign=ContentAlignment.MiddleCenter};
            card.Controls.Add(hint); card.Controls.Add(_error); card.Controls.Add(btn); card.Controls.Add(gap2); card.Controls.Add(_pass); card.Controls.Add(gap1); card.Controls.Add(_user); card.Controls.Add(title); card.Controls.Add(logo);
            Controls.Add(card); AcceptButton=btn;
            card.Anchor=AnchorStyles.None;
            Resize += (s,e) => { card.Left=(ClientSize.Width-card.Width)/2; card.Top=(ClientSize.Height-card.Height)/2; };
            card.Left=(ClientSize.Width-card.Width)/2; card.Top=(ClientSize.Height-card.Height)/2;
        }
        private bool TryAuthenticate(string username, string password)
        {
            // Local bootstrap remains available for existing installations.
            if (SecurityService.Authenticate(username, password))
            {
                AuthorizationService.StartSession(username, AuthorizationService.Administrator);
                return true;
            }
            string role;
            if (SqlAuthenticationService.TryLogin(username, password, out role))
            {
                AuthorizationService.StartSession(username, role, true);
                return true;
            }
            return false;
        }
        private void Login()
        {
            try
            {
                if (SecurityService.RequiresSetup)
                {
                    SecurityService.CreateAdministrator(_user.Text, _pass.Text);
                    AuthorizationService.StartSession(_user.Text, AuthorizationService.Administrator);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else if (TryAuthenticate(_user.Text, _pass.Text))
                {
                    AuditService.TryWrite("LoginSucceeded", "AppUsers");
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else { AuditService.TryWrite("LoginFailed", "AppUsers", "Invalid local administrator credentials"); _error.Text = "Invalid username or password."; }
            }
            catch (System.Data.SqlClient.SqlException) { _error.Text = "SQL Server connection or authentication error."; }
            catch (InvalidOperationException) { _error.Text = "Database configuration error."; }
            catch (ArgumentException ex) { _error.Text = ex.Message; }
            catch (System.IO.IOException) { _error.Text = "Unable to save credentials."; }
            catch (UnauthorizedAccessException) { _error.Text = "Access denied saving credentials."; }
        }
    }
}
