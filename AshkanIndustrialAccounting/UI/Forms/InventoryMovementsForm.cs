using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class InventoryMovementsForm : Form
    {
        private readonly int _productId;
        private readonly ComboBox _type = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        private readonly NumericUpDown _quantity = new NumericUpDown { DecimalPlaces = 3, Maximum = 999999999, Width = 130 };
        private readonly NumericUpDown _unitCost = new NumericUpDown { DecimalPlaces = 2, Maximum = 999999999, Width = 130, ThousandsSeparator = true };
        private readonly TextBox _reference = new TextBox { Width = 180, MaxLength = 80 };
        private readonly TextBox _notes = new TextBox { Width = 250, MaxLength = 500 };
        private readonly DataGridView _history = UiFactory.Grid();
        private Guid _operation = Guid.NewGuid();
        private bool Fa { get { return LocalizationService.IsPersian; } }
        public InventoryMovementsForm(int productId, string productName)
        {
            _productId = productId;
            Text = (Fa ? "گردش انبار - " : "Inventory movements - ") + productName;
            Size = new Size(950, 610); StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(242, 246, 251);
            RightToLeft = Fa ? RightToLeft.Yes : RightToLeft.No;
            _type.Items.AddRange(new object[] { "IN", "OUT", "ADJUST" }); _type.SelectedIndex = 0;
            var inputs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 155, Padding = new Padding(12), AutoScroll = true };
            inputs.Controls.Add(new Label { Text = Fa ? "نوع گردش" : "Movement", AutoSize = true }); inputs.Controls.Add(_type);
            inputs.Controls.Add(new Label { Text = Fa ? "مقدار / موجودی نهایی" : "Quantity / target stock", AutoSize = true }); inputs.Controls.Add(_quantity);
            inputs.Controls.Add(new Label { Text = Fa ? "بهای واحد ورودی" : "Inbound unit cost", AutoSize = true }); inputs.Controls.Add(_unitCost);
            _type.SelectedIndexChanged += (s, e) => UpdateCostState();
            UpdateCostState();
            inputs.Controls.Add(new Label { Text = Fa ? "شماره مرجع" : "Reference", AutoSize = true }); inputs.Controls.Add(_reference);
            inputs.Controls.Add(new Label { Text = Fa ? "توضیحات" : "Notes", AutoSize = true }); inputs.Controls.Add(_notes);
            var save = new Button { Text = Fa ? "ثبت گردش" : "Post movement", AutoSize = true, BackColor = Color.FromArgb(35, 99, 175), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            save.Click += (s, e) => SaveMovement(); inputs.Controls.Add(save);
            _history.ReadOnly = true; _history.Dock = DockStyle.Fill;
            Controls.Add(_history); Controls.Add(inputs);
            Shown += (s, e) => RefreshHistory();
        }
        private void UpdateCostState()
        {
            // ADJUST can be either positive or negative: cost is required by the service only when increasing stock.
            _unitCost.Enabled = (string)_type.SelectedItem != "OUT";
        }
        private void RefreshHistory()
        {
            try { _history.DataSource = InventoryService.History(_productId); }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void SaveMovement()
        {
            try
            {
                InventoryService.RecordValued(_productId, (string)_type.SelectedItem, _quantity.Value, _reference.Text.Trim(), _notes.Text.Trim(), AuthorizationService.CurrentUserName, _operation, _unitCost.Enabled ? (decimal?)_unitCost.Value : null);
                _operation = Guid.NewGuid();
                RefreshHistory();
                MessageBox.Show(Fa ? "گردش با موفقیت ثبت شد." : "Movement posted successfully.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
