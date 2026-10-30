using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;

namespace AshkanIndustrialAccounting.Services
{
    /// <summary>Product margin report; does not mix currencies or infer missing costs.</summary>
    public static class ProductProfitabilityService
    {
        public static DataTable Read(DateTime from, DateTime to, out int excludedInvoices)
        {
            if (to.Date < from.Date) throw new ArgumentException("Invalid date range");
            // One row per invoice/product. Discount is allocated across products in proportion
            // to their pre-header-discount line totals, with the last row taking rounding remainder.
            const string sql = @"SELECT i.Id AS InvoiceId, i.CurrencyCode, i.Subtotal,
 i.Discount AS HeaderDiscount, i.Tax, i.Total,
 l.ProductId, p.SKU, p.Name AS ProductName,
 SUM(l.Qty) AS Quantity, SUM(l.LineTotal) AS LineRevenue,
 MAX(s.Quantity) AS CostQuantity, MAX(s.TotalCost) AS Cost
 FROM dbo.SalesInvoices i
 JOIN dbo.SalesInvoiceLines l ON l.InvoiceId=i.Id
 JOIN dbo.Products p ON p.Id=l.ProductId
 LEFT JOIN dbo.SalesCostSnapshots s ON s.InvoiceId=i.Id AND s.ProductId=l.ProductId
 WHERE i.Status=N'Posted' AND i.InvoiceDate>=@from AND i.InvoiceDate<@until
 GROUP BY i.Id,i.CurrencyCode,i.Subtotal,i.Discount,i.Tax,i.Total,
 l.ProductId,p.SKU,p.Name
 ORDER BY i.Id,l.ProductId";
            var raw=new DataTable();
            using(var cn=Db.Open())
            using(var cmd=new SqlCommand(sql,cn))
            using(var adapter=new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=from.Date;
                cmd.Parameters.Add("@until",SqlDbType.DateTime2).Value=to.Date.AddDays(1);
                adapter.Fill(raw);
            }
            var result=new DataTable();
            result.Columns.Add("SKU",typeof(string));
            result.Columns.Add("Product",typeof(string));
            result.Columns.Add("Currency",typeof(string));
            result.Columns.Add("Quantity",typeof(decimal));
            result.Columns.Add("NetRevenue",typeof(decimal));
            result.Columns.Add("COGS",typeof(decimal));
            result.Columns.Add("GrossProfit",typeof(decimal));
            result.Columns.Add("MarginPercent",typeof(decimal));
            var aggregates=new Dictionary<string,decimal[]>();
            var names=new Dictionary<string,string[]>();
            excludedInvoices=0;
            int cursor=0;
            while(cursor<raw.Rows.Count)
            {
                int id=Convert.ToInt32(raw.Rows[cursor]["InvoiceId"]);
                int end=cursor;
                while(end<raw.Rows.Count && Convert.ToInt32(raw.Rows[end]["InvoiceId"])==id) end++;
                DataRow header=raw.Rows[cursor];
                decimal subtotal=Convert.ToDecimal(header["Subtotal"]);
                decimal discount=Convert.ToDecimal(header["HeaderDiscount"]);
                decimal tax=Convert.ToDecimal(header["Tax"]);
                decimal total=Convert.ToDecimal(header["Total"]);
                decimal sumLines=0m;
                bool valid=subtotal>0m && discount>=0m && discount<=subtotal && tax>=0m
                    && Math.Abs(total-(subtotal-discount+tax))<=0.01m;
                for(int j=cursor;j<end;j++)
                {
                    var r=raw.Rows[j];
                    decimal qty=Convert.ToDecimal(r["Quantity"]);
                    decimal line=Convert.ToDecimal(r["LineRevenue"]);
                    if(r.IsNull("Cost") || r.IsNull("CostQuantity") ||
                       qty<=0m || line<0m ||
                       Convert.ToDecimal(r["CostQuantity"])!=qty) valid=false;
                    sumLines+=line;
                }
                if(Math.Abs(sumLines-subtotal)>0.01m) valid=false;
                if(!valid) { excludedInvoices++;cursor=end;continue; }
                decimal allocated=0m;
                for(int j=cursor;j<end;j++)
                {
                    DataRow r=raw.Rows[j];
                    decimal line=Convert.ToDecimal(r["LineRevenue"]);
                    decimal part=j==end-1 ? discount-allocated :
                        Math.Round(discount*line/subtotal,2,MidpointRounding.AwayFromZero);
                    allocated+=part;
                    decimal revenue=line-part;
                    decimal cost=Convert.ToDecimal(r["Cost"]);
                    string currency=Convert.ToString(r["CurrencyCode"]);
                    string sku=Convert.ToString(r["SKU"]);
                    string key=currency+"|"+Convert.ToInt32(r["ProductId"]);
                    if(!aggregates.ContainsKey(key))
                    {
                        aggregates.Add(key,new decimal[3]);
                        names.Add(key,new []{sku,Convert.ToString(r["ProductName"]),currency});
                    }
                    aggregates[key][0]+=Convert.ToDecimal(r["Quantity"]);
                    aggregates[key][1]+=revenue;
                    aggregates[key][2]+=cost;
                }
                cursor=end;
            }
            foreach(var item in aggregates)
            {
                var a=item.Value;
                decimal profit=a[1]-a[2];
                decimal margin=a[1]==0m?0m:Math.Round(profit*100m/a[1],2);
                var n=names[item.Key];
                result.Rows.Add(n[0],n[1],n[2],a[0],a[1],a[2],profit,margin);
            }
            var view=result.DefaultView;
            view.Sort="GrossProfit DESC";
            return view.ToTable();
        }
    }
}
