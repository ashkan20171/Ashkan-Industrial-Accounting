using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Repositories;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class ChartOfAccountsForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _status = new Label();

        public ChartOfAccountsForm()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.White, Padding = new Padding(14, 12, 14, 12) };
            var refresh = new Button {
                Text = LocalizationService.IsPersian ? "↻  بارگذاری مجدد" : "↻  Refresh",
                Dock = LocalizationService.IsPersian ? DockStyle.Right : DockStyle.Left,
                Width = 150, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(25, 99, 180), ForeColor = Color.White
            };
            refresh.FlatAppearance.BorderSize = 0; refresh.Click += (s, e) => LoadData();
            _status.Dock = DockStyle.Fill; _status.TextAlign = LocalizationService.IsPersian ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            _status.ForeColor = Color.FromArgb(91, 103, 118);
            toolbar.Controls.Add(_status); toolbar.Controls.Add(refresh);

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
            _grid.Dock = DockStyle.Fill; _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false; _grid.RowHeadersVisible = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false; _grid.EnableHeadersVisualStyles = false; _grid.ColumnHeadersHeight = 42; _grid.RowTemplate.Height = 38;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 241, 248);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(22, 43, 70);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 251);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 43, 70);
            card.Controls.Add(_grid);
            Controls.Add(card); Controls.Add(toolbar);
            Shown += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                var data = new AccountRepository().GetAll();
                _grid.DataSource = data;
                _status.Text = LocalizationService.IsPersian ? "تعداد حساب‌ها: " + data.Count : "Accounts: " + data.Count;
            }
            catch (Exception ex)
            {
                _status.Text = LocalizationService.IsPersian ? "عدم دسترسی به دیتابیس" : "Database unavailable";
                MessageBox.Show(ex.Message, LocalizationService.IsPersian ? "خطای دیتابیس" : "Database",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}