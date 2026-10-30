using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Reads the first worksheet of an unencrypted XLSX (Open XML) with strict resource limits.</summary>
    public static class BankStatementXlsx
    {
        private const int MaxBytes = 8 * 1024 * 1024;
        private const int MaxRows = 10001;
        private const string Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static XmlDocument Load(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name);
            if (entry == null) throw new FormatException("Missing XLSX part: " + name);
            if (entry.Length > MaxBytes) throw new FormatException("XLSX part is too large.");
            var doc = new XmlDocument { XmlResolver = null };
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxBytes }))
                doc.Load(reader);
            return doc;
        }
        private static string Child(XmlNode node, string localName)
        {
            foreach (XmlNode child in node.ChildNodes)
                if (child.LocalName == localName && child.NamespaceURI == Ns) return child.InnerText;
            return "";
        }
        private static string Column(string address)
        {
            int i=0; while (i<address.Length && char.IsLetter(address[i])) i++;
            return address.Substring(0,i).ToUpperInvariant();
        }
        private static string Escape(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }
        public static List<BankStatementCsvRow> Read(string path)
        {
            return Read(path, new[] { 0, 1, 2, 3 }, true);
        }
        public static List<BankStatementCsvRow> Read(string path, int[] columns, bool hasHeader)
        {
            return BankStatementCsv.ReadMapped(ReadCsvContent(path, columns, hasHeader, null), new[] { 0, 1, 2, 3 }, hasHeader);
        }
        public static DataTable ReadPreview(string path, int[] columns, bool hasHeader)
        {
            var sourceRows = new List<int>();
            var preview = BankStatementCsv.ReadPreview(ReadCsvContent(path, columns, hasHeader, sourceRows), new[] { 0, 1, 2, 3 }, hasHeader);
            int offset = hasHeader ? 1 : 0;
            for (int i = 0; i < preview.Rows.Count; i++)
                preview.Rows[i]["SourceRow"] = sourceRows[i + offset];
            return preview;
        }
        private static string ReadCsvContent(string path, int[] columns, bool hasHeader, List<int> sourceRows)
        {
            BankStatementCsv.ValidateColumns(columns);
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Select an XLSX file.");
            var info = new FileInfo(path);
            if (info.Length > MaxBytes) throw new FormatException("XLSX exceeds 8 MB.");
            using (var stream = File.OpenRead(path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                if (archive.Entries.Count > 250) throw new FormatException("Too many XLSX parts.");
                long expanded=0;
                foreach (var entry in archive.Entries)
                {
                    expanded += entry.Length;
                    if (expanded > 24L * 1024 * 1024) throw new FormatException("XLSX decompressed content too large.");
                }
                var strings = new List<string>();
                if (archive.GetEntry("xl/sharedStrings.xml") != null)
                {
                    var shared = Load(archive, "xl/sharedStrings.xml");
                    var mgr = new XmlNamespaceManager(shared.NameTable); mgr.AddNamespace("x",Ns);
                    foreach (XmlNode si in shared.SelectNodes("/x:sst/x:si",mgr))
                    {
                        strings.Add(si.InnerText);
                        if (strings.Count > 100000) throw new FormatException("Too many shared strings.");
                    }
                }
                // Resolve the first sheet through workbook relationships rather than assuming sheet1.xml.
                var workbook=Load(archive,"xl/workbook.xml");
                var wmgr=new XmlNamespaceManager(workbook.NameTable);wmgr.AddNamespace("x",Ns);
                var sheet=workbook.SelectSingleNode("/x:workbook/x:sheets/x:sheet",wmgr);
                if(sheet==null) throw new FormatException("Workbook has no worksheet.");
                var relId=sheet.Attributes["id","http://schemas.openxmlformats.org/officeDocument/2006/relationships"];
                if(relId==null) throw new FormatException("Missing worksheet relationship.");
                var rels=Load(archive,"xl/_rels/workbook.xml.rels");
                string target=null;
                foreach(XmlNode rel in rels.DocumentElement.ChildNodes)
                    if(rel.Attributes!=null && rel.Attributes["Id"]!=null && rel.Attributes["Id"].Value==relId.Value)
                    {
                        if(rel.Attributes["TargetMode"]!=null) throw new FormatException("External worksheets are not supported.");
                        target=rel.Attributes["Target"].Value;break;
                    }
                if(String.IsNullOrEmpty(target))throw new FormatException("Worksheet target missing.");
                string part=target.StartsWith("/",StringComparison.Ordinal) ? target.TrimStart('/') : "xl/"+target;
                part=part.Replace('\\','/');
                if(part.Contains("..") || !part.StartsWith("xl/worksheets/",StringComparison.Ordinal))
                    throw new FormatException("Unsupported worksheet path.");
                var doc=Load(archive,part);
                var mgr2=new XmlNamespaceManager(doc.NameTable);mgr2.AddNamespace("x",Ns);
                var csv=new StringBuilder(); int count=0; int lastPhysicalRow=0;
                foreach(XmlNode row in doc.SelectNodes("/x:worksheet/x:sheetData/x:row",mgr2))
                {
                    int physicalRow;
                    var rowAttribute = row.Attributes == null ? null : row.Attributes["r"];
                    if (rowAttribute != null)
                    {
                        if (!Int32.TryParse(rowAttribute.Value, NumberStyles.None, CultureInfo.InvariantCulture, out physicalRow)
                            || physicalRow <= lastPhysicalRow || physicalRow > 1048576)
                            throw new FormatException("Invalid or out-of-order XLSX worksheet row number.");
                    }
                    else physicalRow = lastPhysicalRow + 1;
                    lastPhysicalRow = physicalRow;
                    var values=new string[4];
                    foreach(XmlNode cell in row.ChildNodes)
                    {
                        if(cell.LocalName!="c")continue;
                        var address=cell.Attributes["r"]==null?"":cell.Attributes["r"].Value;
                        string columnName = Column(address);
                        int physical = 0;
                        foreach (char letter in columnName) physical = physical * 26 + (letter - 'A' + 1);
                        physical--;
                        int col = Array.IndexOf(columns, physical);
                        if(col<0)continue;
                        var type=cell.Attributes["t"]==null?"":cell.Attributes["t"].Value;
                        if(type=="e" || type=="f") throw new FormatException("Formula errors are not supported.");
                        string value=type=="inlineStr" ? Child(cell,"is") : Child(cell,"v");
                        if(type=="s")
                        {
                            int index;
                            if(!Int32.TryParse(value,out index)||index<0||index>=strings.Count)throw new FormatException("Invalid shared string index.");
                            value=strings[index];
                        }
                        if(col==0 && (!hasHeader || count>0) && type!="s" && type!="inlineStr")
                        {
                            double serial;
                            if(Double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out serial) && serial>=1 && serial<2958466)
                                value=DateTime.FromOADate(serial).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
                        }
                        values[col]=value;
                    }
                    if((!hasHeader || count>0) && String.IsNullOrWhiteSpace(String.Concat(values)))continue;
                    if(sourceRows!=null)sourceRows.Add(physicalRow);
                    csv.Append(Escape(values[0])).Append(',').Append(Escape(values[1])).Append(',')
                        .Append(Escape(values[2])).Append(',').Append(Escape(values[3])).Append("\r\n");
                    count++;
                    if(count>MaxRows)throw new FormatException("XLSX exceeds 10,000 data rows.");
                }
                return csv.ToString();
            }
        }
    }
}
