using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Exports the displayed report, with spreadsheet formula-injection protection.</summary>
    public static class ReportExportService
    {
        public static void SaveCsv(DataGridView grid, string reportName)
        {
            if (grid.DataSource == null || grid.Rows.Count == 0)
            {
                MessageBox.Show(LocalizationService.IsPersian ?
                    "ابتدا گزارش را بارگذاری کنید." : "Load a report before exporting.");
                return;
            }
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV (Excel compatible)|*.csv";
                dialog.FileName = reportName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (dialog.ShowDialog(grid.FindForm()) != DialogResult.OK) return;
                var builder = new StringBuilder();
                bool first = true;
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (!column.Visible) continue;
                    if (!first) builder.Append(',');
                    AppendCell(builder, column.HeaderText);
                    first = false;
                }
                builder.Append("\r\n");
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow || !row.Visible) continue;
                    first = true;
                    foreach (DataGridViewColumn column in grid.Columns)
                    {
                        if (!column.Visible) continue;
                        if (!first) builder.Append(',');
                        object value = row.Cells[column.Index].Value;
                        string formatted = value == null || value == DBNull.Value ? "" :
                            value is DateTime ? ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) :
                            value is IFormattable ? ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture) :
                            Convert.ToString(value);
                        AppendCell(builder, formatted);
                        first = false;
                    }
                    builder.Append("\r\n");
                }
                // UTF-8 BOM allows Excel to correctly read Persian characters.
                File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(true));
                MessageBox.Show(LocalizationService.IsPersian ?
                    "فایل گزارش ذخیره شد." : "Report exported successfully.");
            }
        }

        /// <summary>Exports a report as a genuine multi-worksheet Excel 2003 XML workbook.
        /// Uses the framework XML writer only; no external spreadsheet dependency is required.</summary>
        public static void SaveMultiSheetExcel(DataTable report, string reportName)
        {
            if (report == null || report.Columns.Count == 0 || report.Rows.Count == 0)
            {
                MessageBox.Show(LocalizationService.IsPersian ? "ابتدا گزارش را بارگذاری کنید." : "Load a report before exporting.");
                return;
            }
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Excel XML Workbook (*.xml)|*.xml";
                dialog.FileName = reportName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                var settings = new System.Xml.XmlWriterSettings { Encoding = new UTF8Encoding(true), Indent = true };
                using (var writer = System.Xml.XmlWriter.Create(dialog.FileName, settings))
                {
                    const string ss = "urn:schemas-microsoft-com:office:spreadsheet";
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Workbook", ss);
                    writer.WriteAttributeString("xmlns", "o", null, "urn:schemas-microsoft-com:office:office");
                    writer.WriteAttributeString("xmlns", "x", null, "urn:schemas-microsoft-com:office:excel");
                    writer.WriteAttributeString("xmlns", "ss", null, ss);
                    WriteWorksheet(writer, ss, "All metrics", report);
                    if (report.Columns.Contains("Category"))
                    {
                        var categories = new List<string>();
                        foreach (DataRow row in report.Rows)
                        {
                            string category = Convert.ToString(row["Category"]);
                            if (!categories.Contains(category)) categories.Add(category);
                        }
                        foreach (string category in categories)
                        {
                            var view = new DataView(report) { RowFilter = "Category = '" + category.Replace("'", "''") + "'" };
                            WriteWorksheet(writer, ss, SafeSheetName(category), view.ToTable());
                        }
                    }
                    writer.WriteEndElement(); writer.WriteEndDocument();
                }
                MessageBox.Show(LocalizationService.IsPersian ? "گزارش چندبرگی اکسل ذخیره شد. فایل XML را با Excel باز کنید." : "Multi-sheet Excel XML workbook saved. Open the XML file with Excel.");
            }
        }

        private static void WriteWorksheet(System.Xml.XmlWriter writer, string ns, string name, DataTable table)
        {
            writer.WriteStartElement("Worksheet", ns);
            writer.WriteAttributeString("ss", "Name", "urn:schemas-microsoft-com:office:spreadsheet", SafeSheetName(name));
            writer.WriteStartElement("Table", ns);
            writer.WriteStartElement("Row", ns);
            foreach (DataColumn column in table.Columns) WriteCell(writer, ns, column.ColumnName);
            writer.WriteEndElement();
            foreach (DataRow row in table.Rows)
            {
                writer.WriteStartElement("Row", ns);
                foreach (DataColumn column in table.Columns) WriteCell(writer, ns, row[column]);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); writer.WriteEndElement();
        }

        private static void WriteCell(System.Xml.XmlWriter writer, string ns, object value)
        {
            writer.WriteStartElement("Cell", ns); writer.WriteStartElement("Data", ns);
            string type = value is byte || value is short || value is int || value is long || value is float || value is double || value is decimal ? "Number" : "String";
            writer.WriteAttributeString("ss", "Type", "urn:schemas-microsoft-com:office:spreadsheet", type);
            string text = value == null || value == DBNull.Value ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
            if (type == "String" && text.Length > 0 && "=+-@".IndexOf(text.TrimStart()[0]) >= 0) text = "'" + text;
            writer.WriteString(text); writer.WriteEndElement(); writer.WriteEndElement();
        }

        private static string SafeSheetName(string value)
        {
            string name = (value ?? "Report").Replace("[", "(").Replace("]", ")").Replace(":", "-").Replace("\\", "-").Replace("/", "-").Replace("?", "").Replace("*", "");
            if (name.Length > 31) name = name.Substring(0, 31);
            return String.IsNullOrWhiteSpace(name) ? "Report" : name;
        }

        private static void AppendCell(StringBuilder builder, string value)
        {
            value = value ?? "";
            // Spreadsheet programs may interpret leading =,+,-,@ as executable formulas.
            // Preserve genuine signed numeric values, but escape other formula-like strings.
            string trimmed = value.TrimStart(' ', '\t', '\r', '\n');
            if (trimmed.Length > 0 && "=+@-".IndexOf(trimmed[0]) >= 0 &&
                !decimal.TryParse(trimmed, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out _))
                value = "'" + value;
            builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        }
    }
}
