using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;

namespace AshkanIndustrialAccounting.UI.Controls
{
    // Reusable client-side search and status filtering for read-only data grids.
    public sealed class GridSearchToolbar : UserControl
    {
        private readonly TextBox search = new TextBox();
        private readonly Label count = new Label();
        private readonly DataGridView grid;
        private DataTable table;
        public GridSearchToolbar(DataGridView target)
        {
            grid = target;
            Height = 64; Dock = DockStyle.Top; BackColor = Color.White; Padding = new Padding(12);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            search.Dock = DockStyle.Fill; search.Margin = new Padding(4, 8, 10, 8);
            search.Font = new Font("Segoe UI", 11F);
            search.RightToLeft = LocalizationService.IsPersian ? RightToLeft.Yes : RightToLeft.No;
            search.AccessibleName = LocalizationService.IsPersian ? "جستجو در جدول" : "Search table";
            count.Dock = DockStyle.Fill; count.TextAlign = ContentAlignment.MiddleCenter;
            count.ForeColor = Color.FromArgb(87, 102, 122); count.Font = new Font("Segoe UI", 10F);
            layout.Controls.Add(search, 0, 0); layout.Controls.Add(count, 1, 0);
            Controls.Add(layout); search.TextChanged += (s, e) => ApplyFilter();
        }
        public void Bind(DataTable source)
        {
            table = source; ApplyFilter();
        }
        private void ApplyFilter()
        {
            if (table == null) { grid.DataSource = null; count.Text = "0"; return; }
            var query = search.Text.Trim();
            var view = new DataView(table);
            if (query.Length > 0)
            {
                // Escape DataColumn names and LIKE metacharacters to avoid malformed expressions.
                var safe = query.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                var conditions = new System.Collections.Generic.List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    var name = column.ColumnName.Replace("]", "\\]");
                    conditions.Add("Convert([" + name + "], 'System.String') LIKE '%" + safe + "%'");
                }
                if (conditions.Count > 0) view.RowFilter = string.Join(" OR ", conditions);
            }
            grid.DataSource = view;
            count.Text = (LocalizationService.IsPersian ? "ردیف: " : "Rows: ") + view.Count.ToString("N0");
        }
    }
}
