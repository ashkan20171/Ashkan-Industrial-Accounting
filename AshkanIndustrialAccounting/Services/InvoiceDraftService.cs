using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AshkanIndustrialAccounting.Infrastructure.Data;
namespace AshkanIndustrialAccounting.Services
{
 public sealed class InvoiceItem { public int ProductId {get;set;} public decimal Quantity {get;set;} public decimal UnitPrice {get;set;} }
 public static class InvoiceDraftService
 {
  public static int Create(string number,int partnerId,IList<InvoiceItem> items)
  {
   AuthorizationService.RequireActiveSession("sales");
   if(String.IsNullOrWhiteSpace(number)||number.Length>40) throw new ArgumentException("Invalid invoice number");
   if(partnerId<=0||items==null||items.Count==0) throw new ArgumentException("Customer and items are required");
   decimal subtotal=0;
   foreach(var item in items) { if(item.ProductId<=0||item.Quantity<=0||item.UnitPrice<0) throw new ArgumentException("Invalid line"); subtotal+=Math.Round(item.Quantity*item.UnitPrice,2,MidpointRounding.AwayFromZero); }
   using(var cn=Db.Open()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
   {
    try {
     using(var check=new SqlCommand("SELECT COUNT(*) FROM dbo.BusinessPartners WHERE Id=@id AND IsActive=1 AND PartnerType IN ('Customer','Both')",cn,tx)) {check.Parameters.Add("@id",SqlDbType.Int).Value=partnerId;if(Convert.ToInt32(check.ExecuteScalar())!=1)throw new InvalidOperationException("Customer not available");}
     int id;
     using(var cmd=new SqlCommand("INSERT dbo.SalesInvoices(InvoiceNo,InvoiceDate,PartnerId,CurrencyCode,Subtotal,Discount,Tax,Total,Status) OUTPUT INSERTED.Id VALUES(@no,SYSUTCDATETIME(),@partner,'IRR',@total,0,0,@total,'Draft')",cn,tx)) {
      cmd.Parameters.Add("@no",SqlDbType.NVarChar,40).Value=number.Trim();cmd.Parameters.Add("@partner",SqlDbType.Int).Value=partnerId;Money(cmd,"@total",subtotal);id=Convert.ToInt32(cmd.ExecuteScalar());}
     foreach(var item in items) {
      using(var cmd=new SqlCommand("INSERT dbo.SalesInvoiceLines(InvoiceId,ProductId,Qty,UnitPrice,Discount,LineTotal) SELECT @invoice,Id,@qty,@price,0,@line FROM dbo.Products WHERE Id=@product AND IsActive=1",cn,tx)) {
       cmd.Parameters.Add("@invoice",SqlDbType.Int).Value=id;cmd.Parameters.Add("@product",SqlDbType.Int).Value=item.ProductId;var qty=cmd.Parameters.Add("@qty",SqlDbType.Decimal);qty.Precision=18;qty.Scale=3;qty.Value=item.Quantity;Money(cmd,"@price",item.UnitPrice);Money(cmd,"@line",Math.Round(item.Quantity*item.UnitPrice,2,MidpointRounding.AwayFromZero));if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Product unavailable");}
     }
     tx.Commit();return id;
    } catch {tx.Rollback();throw;}
   }
  }
  private static void Money(SqlCommand c,string n,decimal v) {var p=c.Parameters.Add(n,SqlDbType.Decimal);p.Precision=18;p.Scale=2;p.Value=v;}
 }
}
