using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace AshkanIndustrialAccounting.Services
{
    public sealed class BankStatementCsvRow
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
    /// <summary>Strict UTF-8 CSV interchange. Columns: Date,Reference,Amount,Description.</summary>
    public static class BankStatementCsv
    {
        public static List<BankStatementCsvRow> Read(string csv)
        {
            if(csv==null)throw new ArgumentNullException("csv");
            if(csv.Length>5*1024*1024)throw new FormatException("CSV file exceeds 5 MB.");
            var records=Parse(csv.TrimStart('\ufeff'));
            if(records.Count==0)throw new FormatException("CSV is empty.");
            var header=records[0];
            if(header.Count!=4 || header[0]!="Date" || header[1]!="Reference" || header[2]!="Amount" || header[3]!="Description")
                throw new FormatException("Expected columns: Date,Reference,Amount,Description.");
            var result=new List<BankStatementCsvRow>();
            for(int i=1;i<records.Count;i++)
            {
                var fields=records[i];
                if(fields.Count==1 && String.IsNullOrWhiteSpace(fields[0]))continue;
                if(fields.Count!=4)throw new FormatException("Wrong number of columns at row "+(i+1));
                DateTime date;decimal amount;
                if(!DateTime.TryParseExact(fields[0],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date))
                    throw new FormatException("Invalid ISO date at row "+(i+1));
                if(!decimal.TryParse(fields[2],NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out amount)
                    || amount==0 || amount!=Decimal.Round(amount,2) || Math.Abs(amount)>9999999999999999m)
                    throw new FormatException("Invalid amount at row "+(i+1));
                if(String.IsNullOrWhiteSpace(fields[1]) || fields[1].Length>100 || fields[3].Length>500)
                    throw new FormatException("Invalid reference or description at row "+(i+1));
                result.Add(new BankStatementCsvRow { Date=date,Reference=fields[1],Amount=amount,Description=fields[3] });
                if(result.Count>10000)throw new FormatException("CSV exceeds 10,000 rows.");
            }
            return result;
        }
        /// <summary>Map arbitrary source column positions to Date, Reference, Amount, Description.</summary>
        public static List<BankStatementCsvRow> ReadMapped(string csv, int[] columns, bool hasHeader)
        {
            if (csv == null) throw new ArgumentNullException("csv");
            if (csv.Length > 5 * 1024 * 1024) throw new FormatException("CSV exceeds 5 MB.");
            ValidateColumns(columns);
            var records = Parse(csv.TrimStart('\ufeff'));
            var normalized = new StringBuilder("Date,Reference,Amount,Description\r\n");
            for (int i = hasHeader ? 1 : 0; i < records.Count; i++)
            {
                var fields = records[i];
                if (fields.Count == 1 && String.IsNullOrWhiteSpace(fields[0])) continue;
                for (int j = 0; j < 4; j++)
                {
                    if (columns[j] >= fields.Count) throw new FormatException("Missing mapped column at row " + (i + 1));
                    if (j > 0) normalized.Append(',');
                    normalized.Append(Escape(fields[columns[j]]));
                }
                normalized.Append("\r\n");
            }
            return Read(normalized.ToString());
        }
        /// <summary>Best-effort row parsing: preserves invalid values for correction in the preview.</summary>
        public static DataTable ReadPreview(string csv, int[] columns, bool hasHeader)
        {
            if (csv == null) throw new ArgumentNullException("csv");
            if (csv.Length > 5 * 1024 * 1024) throw new FormatException("CSV exceeds 5 MB.");
            ValidateColumns(columns);
            var sourceLines = new List<int>();
            var records = Parse(csv.TrimStart('\ufeff'), sourceLines);
            var result = new DataTable();
            foreach (string name in new[] { "Date", "Reference", "Amount", "Description", "Status" })
                result.Columns.Add(name, typeof(string));
            result.Columns.Add("SourceRow", typeof(int));
            for (int i = hasHeader ? 1 : 0; i < records.Count; i++)
            {
                var fields = records[i];
                if (fields.Count == 1 && String.IsNullOrWhiteSpace(fields[0])) continue;
                var values = new string[5];
                for (int j = 0; j < 4; j++)
                    values[j] = columns[j] < fields.Count ? fields[columns[j]] : "";
                values[4] = "Needs validation";
                var data = result.NewRow();
                for (int j=0; j<5; j++) data[j]=values[j];
                data["SourceRow"] = sourceLines[i];
                result.Rows.Add(data);
                if (result.Rows.Count > 10000) throw new FormatException("CSV exceeds 10,000 rows.");
            }
            return result;
        }
        public static void ValidateColumns(int[] columns)
        {
            if (columns == null || columns.Length != 4) throw new ArgumentException("Four column indexes required.");
            var unique = new HashSet<int>();
            foreach (int column in columns)
                if (column < 0 || column > 25 || !unique.Add(column)) throw new ArgumentException("Column indexes must be distinct and between 0 and 25.");
        }
        private static List<List<string>> Parse(string csv)
        {
            return Parse(csv, null);
        }
        private static List<List<string>> Parse(string csv, List<int> sourceLines)
        {
            var result=new List<List<string>>();var record=new List<string>();var cell=new StringBuilder();
            bool quoted=false;bool closed=false;
            int physicalLine=1, recordStart=1;
            for(int i=0;i<csv.Length;i++)
            {
                char ch=csv[i];
                if(quoted)
                {
                    if(ch=='"' && i+1<csv.Length && csv[i+1]=='"'){cell.Append('"');i++;}
                    else if(ch=='"'){quoted=false;closed=true;}
                    else { cell.Append(ch); if(ch=='\n' || (ch=='\r' && (i+1>=csv.Length || csv[i+1]!='\n'))) physicalLine++; }
                }
                else if(ch=='"' && cell.Length==0 && !closed)quoted=true;
                else if(ch==','){record.Add(cell.ToString());cell.Clear();closed=false;}
                else if(ch=='\r' || ch=='\n')
                {
                    if(ch=='\r' && i+1<csv.Length && csv[i+1]=='\n')i++;
                    record.Add(cell.ToString());result.Add(record);if(sourceLines!=null)sourceLines.Add(recordStart);
                    physicalLine++;recordStart=physicalLine;record=new List<string>();cell.Clear();closed=false;
                }
                else { if(closed || ch=='"')throw new FormatException("Malformed CSV quoting.");cell.Append(ch); }
            }
            if(quoted)throw new FormatException("Unclosed CSV quote.");
            if(record.Count>0 || cell.Length>0 || closed){record.Add(cell.ToString());result.Add(record);if(sourceLines!=null)sourceLines.Add(recordStart);}
            return result;
        }
        private static string Escape(object value)
        {
            string text=Convert.ToString(value,CultureInfo.InvariantCulture)??"";
            // Avoid spreadsheet formula injection when CSV is opened in Excel.
            if(text.Length>0 && (text[0]=='=' || text[0]=='+' || text[0]=='-' || text[0]=='@' || text[0]=='\t'))text="'"+text;
            return "\""+text.Replace("\"","\"\"")+"\"";
        }
        public static string Export(DataTable table)
        {
            if(table==null)throw new ArgumentNullException("table");
            var sb=new StringBuilder("Date,Reference,Amount,Description,Status\r\n");
            foreach(DataRow row in table.Rows)
            {
                sb.Append(Escape(Convert.ToDateTime(row["StatementDate"]).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Escape(row["ExternalReference"])).Append(',');
                sb.Append(Convert.ToDecimal(row["Amount"]).ToString("0.00",CultureInfo.InvariantCulture)).Append(',');
                sb.Append(Escape(row["Description"]==DBNull.Value?"":row["Description"])).Append(',');
                sb.Append(Escape(row["Status"])).Append("\r\n");
            }
            return sb.ToString();
        }
    }
}
