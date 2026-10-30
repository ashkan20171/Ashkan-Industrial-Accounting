using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class ProductsForm : Form
    {
        private readonly DataGridView _grid = UiFactory.Grid();
        private readonly GridSearchToolbar _search;
        private readonly Button _save = new Button();
        private readonly Button _new = new Button();
        private readonly Button _movement = new Button();
        private readonly TextBox _sku = new TextBox(), _name = new TextBox(), _unit = new TextBox();
        private readonly NumericUpDown _purchase = Money(), _sale = Money(), _reorder = Quantity();
        private readonly CheckBox _active = new CheckBox();
        private readonly Label _warning = new Label();
        private int? _selectedId;
        private bool Fa { get { return LocalizationService.IsPersian; } }

        private static NumericUpDown Money() { return new NumericUpDown { Maximum = 999999999999m, DecimalPlaces = 2, ThousandsSeparator = true, Dock = DockStyle.Fill }; }
        private static NumericUpDown Quantity() { return new NumericUpDown { Maximum = 999999999m, DecimalPlaces = 3, ThousandsSeparator = true, Dock = DockStyle.Fill }; }
        public ProductsForm()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            _search = new GridSearchToolbar(_grid);
            var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White };
            header.Controls.Add(new Label { Text = Fa ? "مدیریت کالا و موجودی" : "Products & Inventory", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(22,43,70), TextAlign = Fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft });
            var editor = new TableLayoutPanel { Dock = DockStyle.Top, Height = 176, Padding = new Padding(12), ColumnCount = 4, RowCount = 4, BackColor = Color.White, RightToLeft = Fa ? RightToLeft.Yes : RightToLeft.No };
            for (int i = 0; i < 4; i++) editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            Add(editor, 0, 0, Fa ? "کد کالا" : "SKU", _sku); Add(editor, 1, 0, Fa ? "نام کالا" : "Name", _name);
            Add(editor, 2, 0, Fa ? "واحد" : "Unit", _unit); Add(editor, 3, 0, Fa ? "قیمت خرید" : "Purchase price", _purchase);
            Add(editor, 0, 2, Fa ? "قیمت فروش" : "Sale price", _sale); Add(editor, 1, 2, Fa ? "نقطه سفارش" : "Reorder point", _reorder);
            _active.Text = Fa ? "فعال" : "Active"; _active.Checked = true; _active.Dock = DockStyle.Fill; editor.Controls.Add(_active, 2, 3);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            _new.Text = Fa ? "کالای جدید" : "New"; _save.Text = Fa ? "ذخیره کالا" : "Save";
            _new.AutoSize = _save.AutoSize = true; actions.Controls.Add(_new); actions.Controls.Add(_save); _movement.Text = Fa ? "گردش انبار" : "Stock movements"; _movement.AutoSize = true; actions.Controls.Add(_movement); editor.Controls.Add(actions, 3, 3);
            _warning.Dock = DockStyle.Top; _warning.Height = 30; _warning.ForeColor = Color.FromArgb(160, 72, 30); _warning.BackColor = Color.FromArgb(255, 248, 229);
            _grid.SelectionChanged += (s, e) => PopulateSelection(); _new.Click += (s, e) => ClearEditor(); _movement.Click += (s, e) => { if (!_selectedId.HasValue) { MessageBox.Show(Fa ? "ابتدا کالا را انتخاب کنید." : "Select a product first."); return; } using (var dialog = new InventoryMovementsForm(_selectedId.Value, _name.Text)) dialog.ShowDialog(this); LoadData(); }; _save.Click += (s, e) => SaveProduct();
            Controls.Add(_grid); Controls.Add(_search); Controls.Add(_warning); Controls.Add(editor); Controls.Add(header);
            Shown += (s, e) => LoadData();
        }
        private static void Add(TableLayoutPanel p, int col, int row, string label, Control input)
        {
            p.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, AutoSize = true }, col, row);
            input.Dock = DockStyle.Fill; p.Controls.Add(input, col, row + 1);
        }
        private void LoadData()
        {
            try
            {
                using (var cn = Db.Open()) using (var da = new SqlDataAdapter(@"SELECT Id, SKU, Name, Unit, StockQty, ReorderPoint, PurchasePrice, SalePrice, IsActive, CASE WHEN StockQty <= ReorderPoint THEN 'LOW' ELSE 'OK' END AS StockStatus FROM dbo.Products ORDER BY Name", cn))
                { var dt = new DataTable(); da.Fill(dt); _search.Bind(dt); if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false; }
                int low = 0; foreach (DataGridViewRow r in _grid.Rows) if (!r.IsNewRow && Convert.ToString(r.Cells["StockStatus"].Value) == "LOW") low++;
                _warning.Text = Fa ? "کالاهای نیازمند سفارش (در نتایج فعلی): " + low : "Low-stock products (visible results): " + low;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Fa ? "خطای دیتابیس" : "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void PopulateSelection()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.IsNewRow || !_grid.Columns.Contains("Id")) return;
            var r = _grid.CurrentRow;
            if (r.Cells["Id"].Value == DBNull.Value) return;
            _selectedId = Convert.ToInt32(r.Cells["Id"].Value);
            _sku.Text = Convert.ToString(r.Cells["SKU"].Value); _name.Text = Convert.ToString(r.Cells["Name"].Value); _unit.Text = Convert.ToString(r.Cells["Unit"].Value);
            _purchase.Value = Convert.ToDecimal(r.Cells["PurchasePrice"].Value); _sale.Value = Convert.ToDecimal(r.Cells["SalePrice"].Value);
            _reorder.Value = Convert.ToDecimal(r.Cells["ReorderPoint"].Value); _active.Checked = Convert.ToBoolean(r.Cells["IsActive"].Value);
        }
        private void ClearEditor() { _grid.ClearSelection(); _selectedId = null; _sku.Clear(); _name.Clear(); _unit.Text = Fa ? "عدد" : "pcs"; _purchase.Value = _sale.Value = _reorder.Value = 0; _active.Checked = true; _sku.Focus(); }
        private void SaveProduct()
        {
            if (string.IsNullOrWhiteSpace(_sku.Text) || string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_unit.Text))
            { MessageBox.Show(Fa ? "کد، نام و واحد کالا الزامی هستند." : "SKU, name and unit are required."); return; }
            if (_sku.Text.Trim().Length > 50 || _name.Text.Trim().Length > 180 || _unit.Text.Trim().Length > 30)
            { MessageBox.Show(Fa ? "طول اطلاعات کالا بیش از حد مجاز است." : "Product field exceeds maximum length."); return; }
            try
            {
                using (var cn = Db.Open()) using (var cmd = new SqlCommand(_selectedId.HasValue ? @"UPDATE dbo.Products SET SKU=@sku,Name=@name,Unit=@unit,PurchasePrice=@purchase,SalePrice=@sale,ReorderPoint=@reorder,IsActive=@active WHERE Id=@id" : @"INSERT dbo.Products(SKU,Name,Unit,PurchasePrice,SalePrice,ReorderPoint,IsActive) VALUES(@sku,@name,@unit,@purchase,@sale,@reorder,@active)", cn))
                {
                    cmd.Parameters.Add("@sku", SqlDbType.NVarChar, 50).Value = _sku.Text.Trim(); cmd.Parameters.Add("@name", SqlDbType.NVarChar, 180).Value = _name.Text.Trim(); cmd.Parameters.Add("@unit", SqlDbType.NVarChar, 30).Value = _unit.Text.Trim();
                    DecimalParam(cmd, "@purchase", _purchase.Value, 2); DecimalParam(cmd, "@sale", _sale.Value, 2); DecimalParam(cmd, "@reorder", _reorder.Value, 3);
                    cmd.Parameters.Add("@active", SqlDbType.Bit).Value = _active.Checked;
                    if (_selectedId.HasValue) cmd.Parameters.Add("@id", SqlDbType.Int).Value = _selectedId.Value;
                    if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Product was not saved.");
                }
                LoadData();
                MessageBox.Show(Fa ? "کالا ذخیره شد." : "Product saved.");
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627) { MessageBox.Show(Fa ? "کد کالا تکراری است." : "SKU already exists."); }
            catch (Exception ex) { MessageBox.Show(ex.Message, Fa ? "خطای ذخیره" : "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private static void DecimalParam(SqlCommand cmd, string name, decimal value, byte scale)
        { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 18; p.Scale = scale; p.Value = value; }
    }
}
