using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>
    /// Print/Save as PDF through an installed Windows PDF printer.
    /// Prints visible rows/columns; paginates long tables and wraps cells.
    /// </summary>
    public static class ReportPrintService
    {
        public static void Print(DataGridView grid, string title)
        {
            if (grid.DataSource == null || grid.Rows.Count == 0)
            {
                MessageBox.Show(LocalizationService.IsPersian ?
                    "ابتدا گزارش را بارگذاری کنید." : "Load the report first.");
                return;
            }
            var columns=new List<DataGridViewColumn>();
            foreach(DataGridViewColumn col in grid.Columns)
                if(col.Visible) columns.Add(col);
            if(columns.Count==0) return;
            int rowIndex=0;
            using(var document=new PrintDocument())
            {
                document.DocumentName=title;
                document.DefaultPageSettings.Landscape=true;
                document.DefaultPageSettings.Margins=new Margins(35,35,45,45);
                document.PrintPage+=(sender,args)=>
                {
                    Graphics g=args.Graphics;
                    Rectangle bounds=args.MarginBounds;
                    using(var heading=new Font("Segoe UI",13,FontStyle.Bold))
                    using(var textFont=new Font("Segoe UI",8))
                    using(var brush=new SolidBrush(Color.FromArgb(35,49,65)))
                    using(var headerBrush=new SolidBrush(Color.FromArgb(222,233,245)))
                    using(var pen=new Pen(Color.FromArgb(215,223,232)))
                    {
                        int y=bounds.Top;
                        g.DrawString(title,heading,brush,bounds.Left,y);
                        y+=35;
                        int colWidth=Math.Max(45,bounds.Width/columns.Count);
                        int rowHeight=30;
                        for(int i=0;i<columns.Count;i++)
                        {
                            var rect=new Rectangle(bounds.Left+i*colWidth,y,colWidth,rowHeight);
                            g.FillRectangle(headerBrush,rect);
                            g.DrawRectangle(pen,rect);
                            g.DrawString(columns[i].HeaderText,textFont,brush,
                                new RectangleF(rect.X+3,rect.Y+2,rect.Width-6,rect.Height-4));
                        }
                        y+=rowHeight;
                        while(rowIndex<grid.Rows.Count)
                        {
                            DataGridViewRow row=grid.Rows[rowIndex];
                            if(row.IsNewRow || !row.Visible){rowIndex++;continue;}
                            if(y+rowHeight>bounds.Bottom)break;
                            for(int i=0;i<columns.Count;i++)
                            {
                                var rect=new Rectangle(bounds.Left+i*colWidth,y,colWidth,rowHeight);
                                g.DrawRectangle(pen,rect);
                                object value=row.Cells[columns[i].Index].FormattedValue;
                                string display=value==null?"":Convert.ToString(value);
                                g.DrawString(display,textFont,brush,
                                    new RectangleF(rect.X+3,rect.Y+2,rect.Width-6,rect.Height-4));
                            }
                            y+=rowHeight;
                            rowIndex++;
                        }
                    }
                    args.HasMorePages=rowIndex<grid.Rows.Count;
                };
                using(var preview=new PrintPreviewDialog())
                {
                    preview.Document=document;
                    preview.Width=1100;preview.Height=750;
                    preview.ShowDialog(grid.FindForm());
                }
            }
        }
    }
}
