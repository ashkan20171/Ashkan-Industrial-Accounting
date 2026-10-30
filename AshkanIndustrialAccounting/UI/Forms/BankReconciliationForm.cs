using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using AshkanIndustrialAccounting.Infrastructure.Data;
using AshkanIndustrialAccounting.Services;
using AshkanIndustrialAccounting.UI.Controls;

namespace AshkanIndustrialAccounting.UI.Forms
{
    public sealed class BankReconciliationForm : Form
    {
        private readonly ComboBox _accounts = new ComboBox { Width=220, DropDownStyle=ComboBoxStyle.DropDownList, DisplayMember="Name", ValueMember="Id" };
        private readonly DateTimePicker _from = new DateTimePicker { Width=115, Format=DateTimePickerFormat.Short, Value=DateTime.Today.AddMonths(-1) };
        private readonly DateTimePicker _until = new DateTimePicker { Width=115, Format=DateTimePickerFormat.Short };
        private readonly DataGridView _grid = UiFactory.Grid();
        private readonly Label _summary = new Label { Dock=DockStyle.Bottom, Height=35, TextAlign=ContentAlignment.MiddleLeft };
        public BankReconciliationForm()
        {
            Text=LocalizationService.IsPersian?"مغایرت گیری بانکی":"Bank reconciliation";
            Width=1050; Height=650; StartPosition=FormStartPosition.CenterParent;
            BackColor=Color.FromArgb(242,246,251);
            var bar=new FlowLayoutPanel { Dock=DockStyle.Top, Height=56, Padding=new Padding(8), AutoScroll=true };
            var refresh=UiFactory.Primary(LocalizationService.IsPersian?"نمایش":"Refresh");
            var add=UiFactory.Primary(LocalizationService.IsPersian?"ثبت ردیف صورتحساب":"Add statement line");
            var match=UiFactory.Primary(LocalizationService.IsPersian?"تطبیق انتخاب شده":"Match selected");
            bar.Controls.Add(_accounts);bar.Controls.Add(_from);bar.Controls.Add(_until);
            var import=UiFactory.Primary(LocalizationService.IsPersian?"ورود CSV / Excel":"Import CSV / Excel");
            var export=UiFactory.Primary(LocalizationService.IsPersian?"خروجی CSV":"Export CSV");
            var suggest=UiFactory.Primary(LocalizationService.IsPersian?"پیشنهاد تطبیق":"Suggest matches");
            bar.Controls.Add(refresh);bar.Controls.Add(add);bar.Controls.Add(match);
            var unmatch=UiFactory.Primary(LocalizationService.IsPersian?"لغو تطبیق":"Undo match");
            var dashboard=UiFactory.Primary(LocalizationService.IsPersian?"داشبورد مغایرت":"Reconciliation dashboard");
            var history=UiFactory.Primary(LocalizationService.IsPersian?"تاریخچه ورود":"Import history");
            var failures=UiFactory.Primary(LocalizationService.IsPersian?"ورودهای ناموفق":"Failed imports");
            var analytics=UiFactory.Primary(LocalizationService.IsPersian?"تحلیل ورود بانکی":"Import analytics");
            bar.Controls.Add(import);bar.Controls.Add(export);bar.Controls.Add(suggest);bar.Controls.Add(unmatch);bar.Controls.Add(dashboard);bar.Controls.Add(history);bar.Controls.Add(failures);bar.Controls.Add(analytics);
            var compare=UiFactory.Primary(LocalizationService.IsPersian?"مقایسه حساب‌ها":"Compare accounts");
            var alerts=UiFactory.Primary(LocalizationService.IsPersian?"هشدارهای بانکی":"Bank alerts");
            bar.Controls.Add(compare);bar.Controls.Add(alerts);
            Controls.Add(_grid);Controls.Add(_summary);Controls.Add(bar);
            refresh.Click+=(s,e)=>Run(LoadLines);
            add.Click+=(s,e)=>Run(AddLine);
            match.Click+=(s,e)=>Run(MatchSelected);
            import.Click+=(s,e)=>Run(ImportCsv);
            export.Click+=(s,e)=>Run(ExportCsv);
            suggest.Click+=(s,e)=>Run(SuggestSelected);
            unmatch.Click+=(s,e)=>Run(UnmatchSelected);
            dashboard.Click+=(s,e)=>Run(ShowDashboard);
            history.Click+=(s,e)=>Run(ShowImportHistory);
            failures.Click+=(s,e)=>Run(ShowImportFailures);
            analytics.Click+=(s,e)=>Run(ShowImportAnalytics);
            compare.Click+=(s,e)=>Run(ShowAccountComparison);
            alerts.Click+=(s,e)=>Run(ShowBankAlerts);
            Shown+=(s,e)=>Run(()=>{LoadAccounts();LoadLines();});
        }
        private int SelectedAccount { get { if(_accounts.SelectedValue==null) throw new InvalidOperationException("Select an account."); return Convert.ToInt32(_accounts.SelectedValue); } }
        private void Run(Action action) { try { AuthorizationService.RequireActiveSession("cashbank"); action(); } catch(Exception ex) { MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning); } }
        private void LoadAccounts()
        {
            using(var cn=Db.Open())using(var da=new SqlDataAdapter("SELECT Id,Code+' - '+Name AS Name FROM dbo.CashBankAccounts WHERE IsActive=1 AND CurrencyCode='IRR' ORDER BY Name",cn))
            { var dt=new DataTable();da.Fill(dt);_accounts.DataSource=dt; }
        }
        private void LoadLines()
        {
            if(_accounts.SelectedValue==null)return;
            var table=BankReconciliationService.GetStatement(SelectedAccount,_from.Value,_until.Value);
            _grid.DataSource=table;
            decimal open=0;int count=0;
            foreach(DataRow row in table.Rows) if(Convert.ToString(row["Status"])=="Open") { open+=Convert.ToDecimal(row["Amount"]);count++; }
            _summary.Text=(LocalizationService.IsPersian?"تعداد مغایرت باز: ":"Open lines: ")+count+
                (LocalizationService.IsPersian?" | خالص مبالغ باز: ":" | Net unmatched amount: ")+open.ToString("N2")+" IRR"+
                (LocalizationService.IsPersian?" | تطبیق شده: ":" | Matched: ")+(table.Rows.Count-count);
        }
        private void ShowAccountComparison()
        {
            bool fa=LocalizationService.IsPersian;
            using (var dialog=new Form { Text=fa?"مقایسه ورود بانکی حساب‌ها":"Bank import account comparison",
                Width=1100,Height=600,StartPosition=FormStartPosition.CenterParent,
                BackColor=Color.FromArgb(242,246,251) })
            {
                var tools=new FlowLayoutPanel { Dock=DockStyle.Top,Height=55,Padding=new Padding(8) };
                var from=new DateTimePicker { Width=135,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddMonths(-1) };
                var until=new DateTimePicker { Width=135,Format=DateTimePickerFormat.Short,Value=DateTime.Today };
                var refresh=UiFactory.Primary(fa?"به‌روزرسانی":"Refresh");
                var export=UiFactory.Primary(fa?"خروجی CSV":"Export CSV");
                var filter=new ComboBox { Width=190,DropDownStyle=ComboBoxStyle.DropDownList };
                filter.Items.AddRange(fa?new object[]{"همه حساب‌ها","دارای خطا","نیازمند بررسی"}:new object[]{"All accounts","Accounts with failures","Needs review"});
                filter.SelectedIndex=0;
                tools.Controls.Add(from);tools.Controls.Add(until);tools.Controls.Add(filter);tools.Controls.Add(refresh);tools.Controls.Add(export);
                var grid=UiFactory.Grid();grid.Dock=DockStyle.Fill;grid.ReadOnly=true;
                grid.RightToLeft=fa?RightToLeft.Yes:RightToLeft.No;
                var summary=new Label { Dock=DockStyle.Bottom,Height=52,Padding=new Padding(10),
                    BackColor=Color.White,ForeColor=Color.FromArgb(32,77,128) };
                DataTable data=null;
                DataView visible=null;
                Action updateSummary=() =>
                {
                    if(visible==null)return;
                    long success=0,failed=0,rows=0; string worst=""; decimal worstRate=-1m;
                    foreach(DataRowView item in visible)
                    {
                        success+=Convert.ToInt64(item["SuccessfulImports"]);
                        failed+=Convert.ToInt64(item["FailedImports"]);
                        rows+=Convert.ToInt64(item["ImportedRows"]);
                        decimal rate=Convert.ToDecimal(item["FailureRate"]);
                        if(rate>worstRate){worstRate=rate;worst=Convert.ToString(item["Name"]);}
                    }
                    summary.Text=(fa?"حساب‌های نمایش‌داده‌شده: ":"Visible accounts: ")+visible.Count+
                        (fa?" | موفق: ":" | Successful: ")+success+
                        (fa?" | ناموفق: ":" | Failed: ")+failed+
                        (fa?" | ردیف‌ها: ":" | Rows: ")+rows+
                        (worstRate>=0?(fa?" | بیشترین نرخ خطا: ":" | Highest failure rate: ")+worst+" ("+worstRate.ToString("N1")+"%)":"");
                };
                Action applyFilter=() =>
                {
                    if(visible==null)return;
                    visible.RowFilter=filter.SelectedIndex==1?"FailedImports > 0":(filter.SelectedIndex==2?"ReviewStatus = 'Review'":"");
                    updateSummary();
                };
                Action load=() =>
                {
                    if(from.Value.Date>until.Value.Date || (until.Value.Date-from.Value.Date).TotalDays>366)
                        throw new ArgumentException(fa?"بازه تاریخ نامعتبر است (حداکثر ۳۶۶ روز).":"Invalid date range (max 366 days).");
                    data=BankReconciliationService.GetAccountImportComparison(from.Value.Date,until.Value.Date.AddDays(1));
                    if(!data.Columns.Contains("FailureRate"))data.Columns.Add("FailureRate",typeof(decimal));
                    if(!data.Columns.Contains("ReviewStatus"))data.Columns.Add("ReviewStatus",typeof(string));
                    foreach(DataRow row in data.Rows)
                    {
                        decimal ok=Convert.ToDecimal(row["SuccessfulImports"]),bad=Convert.ToDecimal(row["FailedImports"]);
                        decimal rate=(ok+bad)==0?0m:(bad/(ok+bad))*100m;
                        row["FailureRate"]=Decimal.Round(rate,2);
                        row["ReviewStatus"]=(bad>0 && rate>=10m)?"Review":"Normal";
                    }
                    visible=new DataView(data);
                    visible.Sort="FailureRate DESC, FailedImports DESC, Name ASC";
                    grid.DataSource=visible;
                    applyFilter();
                };
                refresh.Click+=(sender,args)=>{try{load();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}};
                filter.SelectedIndexChanged+=(sender,args)=>applyFilter();
                dashboard.Click+=(sender,args)=>
                {
                    try
                    {
                        ShowBankAlertDashboard(from.Value.Date,until.Value.Date.AddDays(1),dialog);
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                export.Click+=(sender,args)=>
                {
                    if(visible==null)return;
                    var exportData=visible.ToTable();
                    using(var save=new SaveFileDialog { Filter="CSV (*.csv)|*.csv",FileName="bank-account-comparison.csv" })
                    if(save.ShowDialog(dialog)==DialogResult.OK)
                    {
                        var sb=new StringBuilder();
                        foreach(DataColumn col in exportData.Columns)
                        { if(sb.Length>0)sb.Append(','); sb.Append(CsvSafeCell(col.ColumnName)); }
                        sb.AppendLine();
                        foreach(DataRow row in exportData.Rows)
                        {
                            for(int i=0;i<exportData.Columns.Count;i++)
                            { if(i>0)sb.Append(','); sb.Append(CsvSafeCell(Convert.ToString(row[i]))); }
                            sb.AppendLine();
                        }
                        File.WriteAllText(save.FileName,sb.ToString(),new UTF8Encoding(true));
                    }
                };
                dialog.Controls.Add(grid);dialog.Controls.Add(summary);dialog.Controls.Add(tools);
                load();dialog.ShowDialog(this);
            }
        }

        private void ShowBankAlerts()
        {
            bool fa=LocalizationService.IsPersian;
            using(var dialog=new Form { Text=fa?"مرکز مدیریت هشدارهای بانکی":"Bank alert management center",
                Width=1180,Height=650,StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(242,246,251) })
            {
                var tools=new FlowLayoutPanel { Dock=DockStyle.Top,Height=62,Padding=new Padding(8),AutoScroll=true };
                var from=new DateTimePicker { Width=125,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddMonths(-1) };
                var until=new DateTimePicker { Width=125,Format=DateTimePickerFormat.Short,Value=DateTime.Today };
                var severity=new ComboBox { Width=135,DropDownStyle=ComboBoxStyle.DropDownList };
                severity.Items.AddRange(fa?new object[]{"همه شدت‌ها","بحرانی","هشدار","اطلاع‌رسانی"}:new object[]{"All severities","Critical","Warning","Information"});severity.SelectedIndex=0;
                var status=new ComboBox { Width=145,DropDownStyle=ComboBoxStyle.DropDownList };
                status.Items.AddRange(fa?new object[]{"همه وضعیت‌ها","جدید","در حال بررسی","رفع‌شده"}:new object[]{"All statuses","New","In progress","Resolved"});status.SelectedIndex=0;
                var refresh=UiFactory.Primary(fa?"به‌روزرسانی":"Refresh");
                var update=UiFactory.Primary(fa?"تغییر وضعیت":"Update status");
                var assign=UiFactory.Primary(fa?"تعیین مسئول":"Assign owner");
                var schedule=UiFactory.Primary(fa?"مهلت و اولویت":"Due date / priority");
                var note=UiFactory.Primary(fa?"ثبت یادداشت":"Add note");
                var history=UiFactory.Primary(fa?"تاریخچه":"View history");
                var export=UiFactory.Primary(fa?"خروجی CSV":"Export CSV");
                var dashboard=UiFactory.Primary(fa?"داشبورد مدیریتی":"Management dashboard");
                tools.Controls.Add(from);tools.Controls.Add(until);tools.Controls.Add(severity);tools.Controls.Add(status);tools.Controls.Add(refresh);tools.Controls.Add(update);tools.Controls.Add(assign);tools.Controls.Add(schedule);tools.Controls.Add(note);tools.Controls.Add(history);tools.Controls.Add(export);tools.Controls.Add(dashboard);
                var grid=UiFactory.Grid();grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
                grid.RightToLeft=fa?RightToLeft.Yes:RightToLeft.No;
                var summary=new Label { Dock=DockStyle.Bottom,Height=45,Padding=new Padding(10),BackColor=Color.White,ForeColor=Color.FromArgb(32,77,128) };
                DataTable alerts=null;
                Action apply=() =>
                {
                    if(alerts==null)return;
                    var view=new DataView(alerts);
                    string statusFilter=status.SelectedIndex==1?"StatusCode = 'New'":status.SelectedIndex==2?"StatusCode = 'InProgress'":status.SelectedIndex==3?"StatusCode = 'Resolved'":"";
                    string severityFilter=severity.SelectedIndex==1?"SeverityCode = 'Critical'":severity.SelectedIndex==2?"SeverityCode = 'Warning'":severity.SelectedIndex==3?"SeverityCode = 'Information'":"";
                    view.RowFilter=String.IsNullOrEmpty(statusFilter)?severityFilter:String.IsNullOrEmpty(severityFilter)?statusFilter:"("+statusFilter+") AND ("+severityFilter+")";
                    grid.DataSource=view;
                    int critical=0,warning=0,open=0;
                    foreach(DataRowView row in view){string sev=Convert.ToString(row["SeverityCode"]);if(sev=="Critical")critical++;if(sev=="Warning")warning++;if(Convert.ToString(row["StatusCode"])!="Resolved")open++;}
                    summary.Text=(fa?"هشدارها: ":"Alerts: ")+view.Count+(fa?" | بحرانی: ":" | Critical: ")+critical+
                        (fa?" | هشدار: ":" | Warning: ")+warning+(fa?" | باز: ":" | Open: ")+open;
                };
                Action load=() =>
                {
                    if(from.Value.Date>until.Value.Date||(until.Value.Date-from.Value.Date).TotalDays>366)
                        throw new ArgumentException(fa?"بازه تاریخ نامعتبر است (حداکثر ۳۶۶ روز).":"Invalid date range (max 366 days).");
                    var source=BankReconciliationService.GetAccountImportComparison(from.Value.Date,until.Value.Date.AddDays(1));
                    foreach(DataRow row in source.Rows)
                    {
                        long ok=Convert.ToInt64(row["SuccessfulImports"]),bad=Convert.ToInt64(row["FailedImports"]);
                        if(bad<=0)continue;
                        decimal rate=Decimal.Round((decimal)bad/(ok+bad)*100m,2);
                        string code=rate>=25m||bad>=5?"Critical":rate>=10m||bad>=2?"Warning":"Information";
                        string recommendation=code=="Critical"?(fa?"بررسی فوری علت خطا و توقف ورودهای تکراری":"Investigate immediately; pause repeated imports"):
                            code=="Warning"?(fa?"بررسی علت خطا و اعتبارسنجی فایل ورودی":"Review failure causes and validate the source file"):
                            (fa?"پایش تلاش بعدی و ثبت علت خطا":"Monitor next attempt and document the cause");
                        BankReconciliationService.SaveBankAlert(Convert.ToInt32(row["AccountId"]),from.Value.Date,until.Value.Date.AddDays(1),code,ok,bad,rate,recommendation);
                    }
                    alerts=BankReconciliationService.GetBankAlerts(from.Value.Date,until.Value.Date.AddDays(1));
                    // Recommended action is persisted in Recommendation; critical failures still advise to validate the source file.
                    apply();
                };
                refresh.Click+=(sender,args)=>{try{load();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
                status.SelectedIndexChanged+=(sender,args)=>apply();
                severity.SelectedIndexChanged+=(sender,args)=>apply();
                Func<int> selectedAlert=() => { if(grid.CurrentRow==null) throw new InvalidOperationException(fa?"ابتدا یک هشدار انتخاب کنید.":"Select an alert first."); return Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value); };
                assign.Click+=(sender,args)=>
                {
                    try
                    {
                        int id=selectedAlert(); var users=BankReconciliationService.GetBankAlertAssignees();
                        using(var pick=new Form { Text=fa?"تعیین مسئول رسیدگی":"Assign alert owner",Width=390,Height=150,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog })
                        {
                            var combo=new ComboBox { Left=12,Top=12,Width=345,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="DisplayName",ValueMember="UserId" };
                            var clear=new DataTable(); clear.Columns.Add("UserId",typeof(int));clear.Columns.Add("DisplayName",typeof(string));clear.Rows.Add(DBNull.Value,fa?"بدون مسئول":"Unassigned");
                            foreach(DataRow r in users.Rows) clear.Rows.Add(r["UserId"],r["DisplayName"]); combo.DataSource=clear;
                            var okButton=UiFactory.Primary(fa?"ذخیره":"Save");okButton.Left=12;okButton.Top=55;okButton.Click+=(a,b)=>{pick.DialogResult=DialogResult.OK;pick.Close();};pick.Controls.Add(combo);pick.Controls.Add(okButton);
                            if(pick.ShowDialog(dialog)==DialogResult.OK) { object value=combo.SelectedValue; BankReconciliationService.AssignBankAlert(id,value==DBNull.Value? (int?)null:Convert.ToInt32(value));load(); }
                        }
                    } catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                schedule.Click+=(sender,args)=>
                {
                    try
                    {
                        int id=selectedAlert(); using(var edit=new Form { Text=fa?"مهلت و اولویت هشدار":"Alert due date and priority",Width=390,Height=190,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog })
                        {
                            var due=new DateTimePicker { Left=12,Top=12,Width=220,Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd HH:mm",ShowCheckBox=true,Checked=false };
                            var priority=new ComboBox { Left=12,Top=48,Width=220,DropDownStyle=ComboBoxStyle.DropDownList }; priority.Items.AddRange(new object[]{"Low","Normal","High","Urgent"});priority.SelectedItem="Normal";
                            var saveSchedule=UiFactory.Primary(fa?"ذخیره":"Save");saveSchedule.Left=12;saveSchedule.Top=90;saveSchedule.Click+=(a,b)=>{edit.DialogResult=DialogResult.OK;edit.Close();};edit.Controls.Add(due);edit.Controls.Add(priority);edit.Controls.Add(saveSchedule);
                            if(edit.ShowDialog(dialog)==DialogResult.OK){BankReconciliationService.SetBankAlertSchedule(id,due.Checked?due.Value.ToUniversalTime():(DateTime?)null,Convert.ToString(priority.SelectedItem));load();}
                        }
                    } catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                note.Click+=(sender,args)=>
                {
                    try
                    {
                        int id=selectedAlert(); using(var edit=new Form { Text=fa?"یادداشت رسیدگی":"Investigation note",Width=500,Height=260,StartPosition=FormStartPosition.CenterParent })
                        {
                            var input=new TextBox { Left=12,Top=12,Width=455,Height=150,Multiline=true,MaxLength=1000,ScrollBars=ScrollBars.Vertical };
                            var saveNote=UiFactory.Primary(fa?"ثبت یادداشت":"Save note");saveNote.Left=12;saveNote.Top=175;saveNote.Click+=(a,b)=>{edit.DialogResult=DialogResult.OK;edit.Close();};edit.Controls.Add(input);edit.Controls.Add(saveNote);
                            if(edit.ShowDialog(dialog)==DialogResult.OK){BankReconciliationService.AddBankAlertNote(id,input.Text);MessageBox.Show(fa?"یادداشت ثبت شد.":"Note saved.",dialog.Text);}
                        }
                    } catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                history.Click+=(sender,args)=>
                {
                    try
                    {
                        int id=selectedAlert();var changes=BankReconciliationService.GetBankAlertHistory(id);var notes=BankReconciliationService.GetBankAlertNotes(id);
                        using(var viewHistory=new Form { Text=fa?"تاریخچه و یادداشت‌های هشدار":"Alert history and notes",Width=850,Height=520,StartPosition=FormStartPosition.CenterParent })
                        {
                            var tabs=new TabControl { Dock=DockStyle.Fill };var changesPage=new TabPage(fa?"تغییرات":"Changes");var notesPage=new TabPage(fa?"یادداشت‌ها":"Notes");
                            var changesGrid=UiFactory.Grid();changesGrid.Dock=DockStyle.Fill;changesGrid.ReadOnly=true;changesGrid.DataSource=changes;var notesGrid=UiFactory.Grid();notesGrid.Dock=DockStyle.Fill;notesGrid.ReadOnly=true;notesGrid.DataSource=notes;changesPage.Controls.Add(changesGrid);notesPage.Controls.Add(notesGrid);tabs.TabPages.Add(changesPage);tabs.TabPages.Add(notesPage);viewHistory.Controls.Add(tabs);viewHistory.ShowDialog(dialog);
                        }
                    } catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                update.Click+=(sender,args)=>
                {
                    if(grid.CurrentRow==null)return;
                    try
                    {
                        int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
                        string current=Convert.ToString(grid.CurrentRow.Cells["StatusCode"].Value);
                        string next=current=="New"?"InProgress":current=="InProgress"?"Resolved":"New";
                        BankReconciliationService.UpdateBankAlertStatus(id,next);load();
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                dashboard.Click+=(sender,args)=>
                {
                    try
                    {
                        ShowBankAlertDashboard(from.Value.Date,until.Value.Date.AddDays(1),dialog);
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                export.Click+=(sender,args)=>
                {
                    var view=grid.DataSource as DataView;if(view==null)return;
                    using(var save=new SaveFileDialog { Filter="CSV (*.csv)|*.csv",FileName="bank-alert-register.csv" })
                    if(save.ShowDialog(dialog)==DialogResult.OK)
                    {
                        var output=view.ToTable();
                        var sb=new StringBuilder();
                        for(int c=0;c<output.Columns.Count;c++){if(c>0)sb.Append(',');sb.Append(CsvSafeCell(output.Columns[c].ColumnName));}sb.AppendLine();
                        foreach(DataRow row in output.Rows){for(int c=0;c<output.Columns.Count;c++){if(c>0)sb.Append(',');sb.Append(CsvSafeCell(Convert.ToString(row[c])));}sb.AppendLine();}
                        File.WriteAllText(save.FileName,sb.ToString(),new UTF8Encoding(true));
                    }
                };
                dialog.Controls.Add(grid);dialog.Controls.Add(summary);dialog.Controls.Add(tools);
                load();dialog.ShowDialog(this);
            }
        }

        private static string CsvSafeCell(string value)
        {
            value=value??"";
            var trimmed=value.TrimStart();
            if(trimmed.Length>0 && "=+-@".IndexOf(trimmed[0])>=0)value="'"+value;
            return "\""+value.Replace("\"","\"\"")+"\"";
        }

        private void ShowImportAnalytics()
        {
            bool fa=LocalizationService.IsPersian;
            using (var dialog=new Form { Text=fa?"تحلیل ورودهای بانکی":"Bank import analytics",
                Width=1000,Height=620,StartPosition=FormStartPosition.CenterParent,
                BackColor=Color.FromArgb(242,246,251) })
            {
                var tools=new FlowLayoutPanel { Dock=DockStyle.Top,Height=55,Padding=new Padding(8),AutoScroll=true };
                var from=new DateTimePicker { Width=130,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddMonths(-1) };
                var until=new DateTimePicker { Width=130,Format=DateTimePickerFormat.Short,Value=DateTime.Today };
                var refresh=UiFactory.Primary(fa?"به‌روزرسانی":"Refresh");
                var export=UiFactory.Primary(fa?"خروجی CSV":"Export CSV");
                var print=UiFactory.Primary(fa?"پیش‌نمایش چاپ":"Print preview");
                var showOnlyIssues=new CheckBox { Text=fa?"فقط روزهای دارای خطا":"Days with failures",AutoSize=true,Padding=new Padding(8,6,0,0) };
                tools.Controls.Add(from);tools.Controls.Add(until);tools.Controls.Add(refresh);tools.Controls.Add(export);tools.Controls.Add(print);tools.Controls.Add(showOnlyIssues);
                var grid=UiFactory.Grid();grid.Dock=DockStyle.Fill;grid.ReadOnly=true;
                grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
                grid.RightToLeft=fa?RightToLeft.Yes:RightToLeft.No;
                var summary=new Label { Dock=DockStyle.Bottom,Height=48,Padding=new Padding(10),
                    BackColor=Color.White,ForeColor=Color.FromArgb(32,77,128),
                    Font=new Font("Segoe UI",10,FontStyle.Bold) };
                var chart=new Panel { Dock=DockStyle.Top,Height=195,BackColor=Color.White,
                    Padding=new Padding(10) };
                DataTable data=null;
                DataTable previousData=null;
                chart.Paint+=(sender,args)=>PaintImportAnalyticsChart(args.Graphics,chart.ClientRectangle,
                    data==null?null:(showOnlyIssues.Checked?new DataView(data) { RowFilter="FailedImports > 0" }.ToTable():data),fa);
                var kpiBar=new TableLayoutPanel { Dock=DockStyle.Top,Height=100,ColumnCount=4,
                    BackColor=Color.FromArgb(242,246,251),Padding=new Padding(6) };
                for(int i=0;i<4;i++) kpiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25f));
                var kpiLabels=new Label[4];
                for(int i=0;i<4;i++)
                {
                    var tile=new Panel { Dock=DockStyle.Fill,Margin=new Padding(5),BackColor=Color.White,
                        Padding=new Padding(9) };
                    kpiLabels[i]=new Label { Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,
                        Font=new Font("Segoe UI",10f,FontStyle.Bold),ForeColor=Color.FromArgb(35,76,119) };
                    tile.Controls.Add(kpiLabels[i]);kpiBar.Controls.Add(tile,i,0);
                }
                Action refreshPresentation=()=>
                {
                    if(data==null)return;
                    var view=new DataView(data);
                    if(showOnlyIssues.Checked)view.RowFilter="FailedImports > 0";
                    DataTable visible=view.ToTable();
                    grid.DataSource=view;
                    chart.Invalidate();
                    var current=AggregateImportAnalytics(visible);
                    var priorView=new DataView(previousData??data);
                    if(showOnlyIssues.Checked)priorView.RowFilter="FailedImports > 0";
                    var prior=AggregateImportAnalytics(priorView.ToTable());
                    decimal rate=current.Successes+current.Failures==0?0m:100m*current.Successes/(current.Successes+current.Failures);
                    decimal priorRate=prior.Successes+prior.Failures==0?0m:100m*prior.Successes/(prior.Successes+prior.Failures);
                    string vs=fa?"نسبت به دوره قبل: ":"vs previous period: ";
                    kpiLabels[0].Text=(fa?"ورودهای موفق":"Successful imports")+Environment.NewLine+current.Successes.ToString("N0")+Environment.NewLine+vs+FormatPeriodChange(current.Successes,prior.Successes,fa);
                    kpiLabels[1].Text=(fa?"ورودهای ناموفق":"Failed imports")+Environment.NewLine+current.Failures.ToString("N0")+Environment.NewLine+vs+FormatPeriodChange(current.Failures,prior.Failures,fa);
                    kpiLabels[1].ForeColor=current.Failures>0?Color.FromArgb(184,65,74):Color.FromArgb(35,76,119);
                    kpiLabels[2].Text=(fa?"نرخ موفقیت":"Success rate")+Environment.NewLine+rate.ToString("N1")+"%"+Environment.NewLine+vs+FormatPercentagePointChange(rate,priorRate)+ (fa?" واحد درصد":" pp");
                    kpiLabels[3].Text=(fa?"ردیف‌های ثبت‌شده":"Imported rows")+Environment.NewLine+current.Rows.ToString("N0")+Environment.NewLine+vs+FormatPeriodChange(current.Rows,prior.Rows,fa);
                    summary.Text=(fa?"موفق: ":"Successful: ")+current.Successes+"  |  "+
                        (fa?"ناموفق: ":"Failed: ")+current.Failures+"  |  "+
                        (fa?"نرخ موفقیت: ":"Success rate: ")+rate.ToString("N1")+"%  |  "+
                        (fa?"ردیف‌ها: ":"Rows: ")+current.Rows+"  |  "+
                        (fa?"ورودی: ":"Incoming: ")+current.Incoming.ToString("N2")+" ("+FormatPeriodChange(current.Incoming,prior.Incoming,fa)+")  |  "+
                        (fa?"خروجی: ":"Outgoing: ")+current.Outgoing.ToString("N2")+" ("+FormatPeriodChange(current.Outgoing,prior.Outgoing,fa)+") IRR"+
                        (showOnlyIssues.Checked?(fa?"  |  فقط روزهای دارای خطا":"  |  Failures only"):"")+ (fa?"  |  مقایسه با بازه قبلی هم‌طول":"  |  Compared with preceding equal-length period");
                };
                Action load=()=>
                {
                    if(from.Value.Date>until.Value.Date)
                        throw new ArgumentException(fa?"بازه تاریخ نامعتبر است.":"Invalid date range.");
                    if((until.Value.Date-from.Value.Date).TotalDays>366)
                        throw new ArgumentException(fa?"بازه گزارش نباید بیشتر از ۳۶۶ روز باشد.":"Report range cannot exceed 366 days.");
                    // Date pickers represent UTC calendar days; upper boundary is exclusive.
                    int periodDays=(until.Value.Date-from.Value.Date).Days+1;
                    data=BankReconciliationService.GetImportAnalytics(SelectedAccount,from.Value.Date,until.Value.Date.AddDays(1));
                    DateTime previousFrom=from.Value.Date.AddDays(-periodDays);
                    previousData=BankReconciliationService.GetImportAnalytics(SelectedAccount,previousFrom,from.Value.Date);
                    refreshPresentation();
                };
                refresh.Click+=(sender,args)=>{try{load();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}};
                showOnlyIssues.CheckedChanged+=(sender,args)=>refreshPresentation();
                dashboard.Click+=(sender,args)=>
                {
                    try
                    {
                        ShowBankAlertDashboard(from.Value.Date,until.Value.Date.AddDays(1),dialog);
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                export.Click+=(sender,args)=>
                {
                    if(data==null)return;
                    var exportView=new DataView(data);
                    if(showOnlyIssues.Checked)exportView.RowFilter="FailedImports > 0";
                    using(var save=new SaveFileDialog { Filter="CSV (*.csv)|*.csv",FileName="bank-import-analytics.csv" })
                    if(save.ShowDialog(dialog)==DialogResult.OK) ExportAnalyticsCsv(exportView.ToTable(),save.FileName);
                };
                print.Click+=(sender,args)=>ReportPrintService.Print(grid,dialog.Text+
                    (showOnlyIssues.Checked?(fa?" - فقط روزهای دارای خطا":" - Failures only"):""));
                dialog.Controls.Add(grid);dialog.Controls.Add(chart);dialog.Controls.Add(kpiBar);dialog.Controls.Add(summary);dialog.Controls.Add(tools);
                load();dialog.ShowDialog(this);
            }
        }

        private void ShowBankAlertDashboard(DateTime fromDate,DateTime untilDateExclusive,IWin32Window owner)
        {
            bool fa=LocalizationService.IsPersian;
            using(var view=new Form { Text=fa?"داشبورد مدیریتی هشدارهای بانکی":"Bank alerts management dashboard",Width=1180,Height=760,MinimumSize=new Size(900,620),StartPosition=FormStartPosition.CenterParent })
            {
                var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=82,Padding=new Padding(6),WrapContents=true,AutoScroll=true };
                var category=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=175 };
                category.Items.AddRange(new object[]{fa?"همه شاخص‌ها":"All metrics","MonthlyTrend","AssigneeWorkload","AverageResolutionMinutes","Status","Severity","Priority","SLA","Assignment","Resolution","Rate","ExecutivePriority"});category.SelectedIndex=0;
                var account=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=205,DisplayMember="Name",ValueMember="Id" };
                var accountTable=BankReconciliationService.GetBankAlertAccounts();
                var allAccounts=accountTable.NewRow();allAccounts["Id"]=DBNull.Value;allAccounts["Name"]=fa?"همه حساب‌ها":"All accounts";accountTable.Rows.InsertAt(allAccounts,0);account.DataSource=accountTable;account.SelectedIndex=0;
                var assignee=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=190,DisplayMember="DisplayName",ValueMember="UserId" };
                var assigneeTable=BankReconciliationService.GetBankAlertAssignees();
                var allAssignees=assigneeTable.NewRow();allAssignees["UserId"]=DBNull.Value;allAssignees["DisplayName"]=fa?"همه مسئولان":"All assignees";assigneeTable.Rows.InsertAt(allAssignees,0);assignee.DataSource=assigneeTable;assignee.SelectedIndex=0;
                var export=new Button { Text=fa?"خروجی CSV":"Export CSV",AutoSize=true };
                var excel=new Button { Text=fa?"گزارش چندبرگی Excel":"Multi-sheet Excel",AutoSize=true };
                var print=new Button { Text=fa?"چاپ گزارش":"Print report",AutoSize=true };
                toolbar.Controls.Add(new Label { Text=fa?"گروه:":"Metric:",AutoSize=true,Padding=new Padding(3,8,0,0) });toolbar.Controls.Add(category);
                toolbar.Controls.Add(new Label { Text=fa?"حساب بانکی:":"Bank account:",AutoSize=true,Padding=new Padding(3,8,0,0) });toolbar.Controls.Add(account);
                toolbar.Controls.Add(new Label { Text=fa?"مسئول:":"Assignee:",AutoSize=true,Padding=new Padding(3,8,0,0) });toolbar.Controls.Add(assignee);
                toolbar.Controls.Add(export);toolbar.Controls.Add(excel);toolbar.Controls.Add(print);
                var grid=UiFactory.Grid();grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;
                var chart=new Panel { Dock=DockStyle.Top,Height=250,BackColor=Color.White,Padding=new Padding(8) };
                var totals=new Label { Dock=DockStyle.Top,Height=34,Padding=new Padding(8) };
                var filtered=new DataView();
                Action refresh=() =>
                {
                    int? accountId=account.SelectedValue==null||account.SelectedValue==DBNull.Value?(int?)null:Convert.ToInt32(account.SelectedValue);
                    int? assigneeId=assignee.SelectedValue==null||assignee.SelectedValue==DBNull.Value?(int?)null:Convert.ToInt32(assignee.SelectedValue);
                    // Compatibility overload remains available: BankReconciliationService.GetBankAlertDashboard(fromDate,untilDateExclusive).
                    var report=BankReconciliationService.GetBankAlertDashboard(fromDate,untilDateExclusive,accountId,assigneeId);
                    filtered=new DataView(report);
                    string selected=Convert.ToString(category.SelectedItem);
                    filtered.RowFilter=category.SelectedIndex<=0?"":"Category = '"+selected.Replace("'","''")+"'";
                    grid.DataSource=filtered;
                    totals.Text=(fa?"تعداد شاخص‌ها: ":"Report metrics: ")+filtered.Count;
                    chart.Invalidate();
                };
                category.SelectedIndexChanged+=(s,e)=>refresh();account.SelectedIndexChanged+=(s,e)=>refresh();assignee.SelectedIndexChanged+=(s,e)=>refresh();
                chart.Paint+=(s,e)=>PaintBankAlertDashboardChart(e.Graphics,chart.ClientRectangle,filtered.ToTable(),fa);
                // The grid is bound to the filtered DataView, so the shared CSV exporter exports only visible rows.
                export.Click+=(s,e)=>ReportExportService.SaveCsv(grid,"BankAlertManagementDashboard");
                excel.Click+=(s,e)=>ReportExportService.SaveMultiSheetExcel(filtered.ToTable(),"BankAlertManagementDashboard");
                print.Click+=(s,e)=>ReportPrintService.Print(grid,view.Text+" - "+fromDate.ToString("yyyy-MM-dd")+" / "+untilDateExclusive.AddDays(-1).ToString("yyyy-MM-dd"));
                view.Controls.Add(grid);view.Controls.Add(chart);view.Controls.Add(totals);view.Controls.Add(toolbar);
                refresh();view.ShowDialog(owner);
            }
        }

        private static void PaintBankAlertDashboardChart(Graphics graphics,Rectangle bounds,DataTable data,bool persian)
        {
            graphics.Clear(Color.White);
            using(var title=new Font("Segoe UI",10,FontStyle.Bold)) using(var body=new Font("Segoe UI",8))
            using(var ink=new SolidBrush(Color.FromArgb(39,66,94))) using(var bar=new SolidBrush(Color.FromArgb(45,132,174)))
            using(var axis=new Pen(Color.FromArgb(218,226,234)))
            {
                graphics.DrawString(persian?"نمای مقایسه‌ای شاخص‌های انتخاب‌شده":"Selected metric comparison",title,ink,12,8);
                if(data==null||data.Rows.Count==0){graphics.DrawString(persian?"داده‌ای برای نمایش وجود ندارد":"No data to display",body,ink,14,48);return;}
                var rows=new List<DataRow>();foreach(DataRow row in data.Rows)rows.Add(row);
                rows.Sort((a,b)=>Convert.ToInt64(b["AlertCount"]).CompareTo(Convert.ToInt64(a["AlertCount"])));
                int maxRows=Math.Min(rows.Count,10);long max=1;for(int i=0;i<maxRows;i++)max=Math.Max(max,Convert.ToInt64(rows[i]["AlertCount"]));
                int left=Math.Min(220,Math.Max(120,bounds.Width/3)),right=bounds.Width-24,top=38,rowHeight=Math.Max(18,(bounds.Height-48)/Math.Max(1,maxRows));
                for(int i=0;i<maxRows;i++)
                {
                    string label=Convert.ToString(rows[i]["Item"]);long value=Convert.ToInt64(rows[i]["AlertCount"]);int y=top+i*rowHeight;
                    graphics.DrawString(label.Length>28?label.Substring(0,25)+"...":label,body,ink,8,y+2);
                    int width=(int)((right-left-55)*(value/(double)max));
                    graphics.FillRectangle(bar,left,y+1,Math.Max(1,width),Math.Max(8,rowHeight-5));
                    graphics.DrawString(value.ToString("N0"),body,ink,left+width+6,y+2);
                }
                graphics.DrawLine(axis,left,bounds.Height-8,right,bounds.Height-8);
            }
        }

        private sealed class ImportAnalyticsTotals
        {
            public long Successes,Failures,Rows;
            public decimal Incoming,Outgoing;
        }

        private static ImportAnalyticsTotals AggregateImportAnalytics(DataTable table)
        {
            var totals=new ImportAnalyticsTotals();
            if(table==null)return totals;
            foreach(DataRow row in table.Rows)
            {
                totals.Successes+=Convert.ToInt64(row["SuccessfulImports"]);
                totals.Failures+=Convert.ToInt64(row["FailedImports"]);
                totals.Rows+=Convert.ToInt64(row["ImportedRows"]);
                totals.Incoming+=Convert.ToDecimal(row["TotalIncoming"]);
                totals.Outgoing+=Convert.ToDecimal(row["TotalOutgoing"]);
            }
            return totals;
        }

        private static string FormatPeriodChange(decimal current,decimal previous,bool persian)
        {
            if(previous==0m)return current==0m?"0%":(persian?"جدید":"New");
            decimal change=(current-previous)/Math.Abs(previous)*100m;
            return (change>0?"+":"")+change.ToString("N1")+"%";
        }

        private static string FormatPercentagePointChange(decimal currentRate,decimal previousRate)
        {
            decimal change=currentRate-previousRate;
            return (change>0?"+":"")+change.ToString("N1");
        }

        // Rendered using GDI+ so the legacy .NET Framework 4.8 project needs no charting package.
        private static void PaintImportAnalyticsChart(Graphics g,Rectangle bounds,DataTable data,bool persian)
        {
            g.Clear(Color.White);
            using(var titleBrush=new SolidBrush(Color.FromArgb(35,66,98)))
            using(var successBrush=new SolidBrush(Color.FromArgb(36,153,111)))
            using(var failureBrush=new SolidBrush(Color.FromArgb(216,86,94)))
            using(var axisPen=new Pen(Color.FromArgb(214,224,235)))
            using(var font=new Font("Segoe UI",9f))
            {
                g.DrawString(persian?"روند ورودهای بانکی (۱۴ روز اخیر در بازه انتخابی)":"Bank import trend (last 14 days in range)",
                    font,titleBrush,12,8);
                if(data==null || data.Rows.Count==0)
                {
                    g.DrawString(persian?"داده‌ای برای نمایش وجود ندارد":"No data to display",font,titleBrush,12,70);
                    return;
                }
                // SQL returns newest days first; chart displays oldest to newest.
                // Restrict to the latest 14 available days, independent of DataView filters.
                var days=new List<DataRow>();
                foreach(DataRow row in data.Rows)days.Add(row);
                days.Sort((a,b)=>Convert.ToDateTime(b["DayUtc"]).CompareTo(Convert.ToDateTime(a["DayUtc"])));
                int n=Math.Min(14,days.Count);
                long max=1;
                for(int i=0;i<n;i++)
                {
                    DataRow row=days[i];
                    long count=Convert.ToInt64(row["SuccessfulImports"])+Convert.ToInt64(row["FailedImports"]);
                    if(count>max)max=count;
                }
                int left=48,top=38,right=Math.Max(left+20,bounds.Width-18),bottom=bounds.Height-38;
                int height=Math.Max(1,bottom-top);
                g.DrawLine(axisPen,left,bottom,right,bottom);
                float slot=(float)(right-left)/n;
                for(int i=0;i<n;i++)
                {
                    DataRow row=days[n-1-i];
                    long success=Convert.ToInt64(row["SuccessfulImports"]);
                    long failed=Convert.ToInt64(row["FailedImports"]);
                    float x=left+i*slot+slot*0.18f;
                    float w=Math.Max(2,slot*0.64f);
                    float hSuccess=(float)(success/(double)max*height);
                    float hFailure=(float)(failed/(double)max*height);
                    g.FillRectangle(successBrush,x,bottom-hSuccess,w,hSuccess);
                    g.FillRectangle(failureBrush,x,bottom-hSuccess-hFailure,w,hFailure);
                    if(n<=7 || i%2==0)
                    {
                        string day=Convert.ToDateTime(row["DayUtc"]).ToString("MM/dd");
                        g.DrawString(day,font,titleBrush,x,bottom+4);
                    }
                }
                g.FillRectangle(successBrush,12,bounds.Height-17,9,9);
                g.DrawString(persian?"موفق":"Success",font,titleBrush,24,bounds.Height-22);
                g.FillRectangle(failureBrush,120,bounds.Height-17,9,9);
                g.DrawString(persian?"ناموفق":"Failed",font,titleBrush,132,bounds.Height-22);
            }
        }

        private static void ExportAnalyticsCsv(DataTable data,string path)
        {
            var sb=new StringBuilder();
            var headers=new List<string>();
            foreach(DataColumn column in data.Columns)headers.Add(column.ColumnName);
            sb.AppendLine(String.Join(",",headers.ToArray()));
            foreach(DataRow row in data.Rows)
            {
                var cells=new List<string>();
                foreach(DataColumn column in data.Columns)
                {
                    string value=Convert.ToString(row[column],System.Globalization.CultureInfo.InvariantCulture)??"";
                    string trimmed=value.TrimStart();
                    if(trimmed.StartsWith("=")||trimmed.StartsWith("+")||trimmed.StartsWith("-")||trimmed.StartsWith("@"))value="'"+value;
                    cells.Add("\""+value.Replace("\"","\"\"")+"\"");
                }
                sb.AppendLine(String.Join(",",cells.ToArray()));
            }
            File.WriteAllText(path,sb.ToString(),new UTF8Encoding(true));
        }

        private void ShowImportFailures()
        {
            var data=BankReconciliationService.GetImportFailures(SelectedAccount);
            using (var dialog=new Form { Text=LocalizationService.IsPersian?"گزارش ورودهای ناموفق":"Failed bank imports",
                Width=800,Height=460,StartPosition=FormStartPosition.CenterParent })
            {
                var grid=UiFactory.Grid(); grid.Dock=DockStyle.Fill;
                grid.ReadOnly=true; grid.DataSource=data;
                grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
                grid.RightToLeft=LocalizationService.IsPersian?RightToLeft.Yes:RightToLeft.No;
                dialog.Controls.Add(grid);
                dialog.ShowDialog(this);
            }
        }

        private void ShowImportHistory()
        {
            DataTable data = null;
            int pageIndex = 0;
            int totalRows = 0;
            const int pageSize = 100;
            using (var dialog = new Form { Text=LocalizationService.IsPersian?"تاریخچه ورود بانکی":"Bank import history",
                Width=1050, Height=620, StartPosition=FormStartPosition.CenterParent,
                BackColor=Color.FromArgb(242,246,251) })
            {
                bool fa = LocalizationService.IsPersian;
                var grid=UiFactory.Grid(); grid.Dock=DockStyle.Fill; grid.ReadOnly=true;
                grid.AllowUserToAddRows=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
                var view = new DataView();
                grid.DataSource=view;
                var tools = new FlowLayoutPanel { Dock=DockStyle.Top, Height=85, Padding=new Padding(8), AutoScroll=true };
                var from = new DateTimePicker { Width=125, Format=DateTimePickerFormat.Short, Value=DateTime.Today.AddMonths(-1) };
                var until = new DateTimePicker { Width=125, Format=DateTimePickerFormat.Short, Value=DateTime.Today };
                var user = new TextBox { Width=155 };
                var userLabel = new Label { Text=fa?"کاربر:":"User:", AutoSize=true, TextAlign=ContentAlignment.MiddleCenter };
                var apply = UiFactory.Primary(fa?"اعمال فیلتر":"Apply filters");
                var clear = UiFactory.Primary(fa?"پاک کردن فیلتر":"Clear filters");
                var details = UiFactory.Primary(fa?"جزئیات عملیات":"Import details");
                var export = UiFactory.Primary(fa?"خروجی Excel/CSV":"Excel-compatible CSV");
                var preview = UiFactory.Primary(fa?"پیش‌نمایش چاپ":"Print preview");
                tools.Controls.Add(from); tools.Controls.Add(until); tools.Controls.Add(userLabel);
                tools.Controls.Add(user); tools.Controls.Add(apply); tools.Controls.Add(clear);
                tools.Controls.Add(details); tools.Controls.Add(export); tools.Controls.Add(preview);
                var status = new Label { Dock=DockStyle.Bottom, Height=30, TextAlign=ContentAlignment.MiddleLeft };
                var previous = UiFactory.Primary(fa?"صفحه قبل":"Previous");
                var next = UiFactory.Primary(fa?"صفحه بعد":"Next");
                tools.Controls.Add(previous); tools.Controls.Add(next);
                Action loadPage=()=>
                {
                    if(from.Value.Date>until.Value.Date ||
                        (until.Value.Date-from.Value.Date).TotalDays>366)
                        throw new ArgumentException(fa?"بازه تاریخ نامعتبر است.":"Invalid date range.");
                    DateTime fromUtc=from.Value.Date;
                    DateTime untilUtc=until.Value.Date.AddDays(1);
                    totalRows=BankReconciliationService.GetImportHistoryCount(
                        SelectedAccount,fromUtc,untilUtc,user.Text);
                    int totalPages=Math.Max(1,(int)Math.Ceiling(totalRows/(double)pageSize));
                    if(pageIndex>=totalPages) pageIndex=totalPages-1;
                    data=BankReconciliationService.GetImportHistoryPage(SelectedAccount,
                        fromUtc,untilUtc,user.Text,pageIndex,pageSize);
                    view.Table=data;
                    status.Text=(fa?"صفحه ":"Page ")+(pageIndex+1)+" / "+totalPages+
                        (fa?" | کل رکوردها: ":" | Total rows: ")+totalRows+
                        (fa?" | نمایش این صفحه: ":" | Rows on page: ")+data.Rows.Count;
                    previous.Enabled=pageIndex>0;
                    next.Enabled=pageIndex+1<totalPages;
                };
                apply.Click+=(sender,args)=>{try{pageIndex=0;loadPage();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}};
                clear.Click+=(sender,args)=>{try{user.Clear();from.Value=DateTime.Today.AddMonths(-1);until.Value=DateTime.Today;pageIndex=0;loadPage();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}};
                previous.Click+=(sender,args)=>{if(pageIndex>0){try{pageIndex--;loadPage();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}}};
                next.Click+=(sender,args)=>{if(pageIndex+1<(int)Math.Ceiling(totalRows/(double)pageSize)){try{pageIndex++;loadPage();}catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}}};
                details.Click+=(sender,args)=>
                {
                    if(grid.CurrentRow==null || grid.CurrentRow.DataBoundItem==null)return;
                    var selected=grid.CurrentRow.DataBoundItem as DataRowView;
                    if(selected==null)return;
                    Guid importId=(Guid)selected["ImportId"];
                    var lines=BankReconciliationService.GetImportDetails(SelectedAccount,importId);
                    using(var detailDialog=new Form { Text=(fa?"جزئیات ورود: ":"Import details: ")+importId,
                        Width=920,Height=490,StartPosition=FormStartPosition.CenterParent })
                    {
                        var detailGrid=UiFactory.Grid();detailGrid.Dock=DockStyle.Fill;
                        detailGrid.ReadOnly=true;detailGrid.AllowUserToAddRows=false;detailGrid.DataSource=lines;
                        detailDialog.Controls.Add(detailGrid);detailDialog.ShowDialog(dialog);
                    }
                };
                Func<DataTable> getFilteredExportData = () =>
                {
                    if(from.Value.Date>until.Value.Date ||
                        (until.Value.Date-from.Value.Date).TotalDays>366)
                        throw new ArgumentException(fa?"بازه تاریخ نامعتبر است.":"Invalid date range.");
                    return BankReconciliationService.GetImportHistoryForExport(SelectedAccount,
                        from.Value.Date,until.Value.Date.AddDays(1),user.Text);
                };
                dashboard.Click+=(sender,args)=>
                {
                    try
                    {
                        ShowBankAlertDashboard(from.Value.Date,until.Value.Date.AddDays(1),dialog);
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                };
                export.Click+=(sender,args)=>
                {
                    try
                    {
                        using(var save=new SaveFileDialog { Filter="CSV (*.csv)|*.csv", FileName="bank-import-history.csv" })
                        if(save.ShowDialog(dialog)==DialogResult.OK)
                        {
                            DataTable exportData=getFilteredExportData();
                            var sb=new StringBuilder("ImportId,ImportedAt,ImportedBy,RowCount,TotalIncoming,TotalOutgoing\r\n");
                            foreach(DataRow row in exportData.Rows)
                            {
                                string[] cells = new string[] {
                                    CsvCell(Convert.ToString(row["ImportId"],System.Globalization.CultureInfo.InvariantCulture),false),
                                    CsvCell(Convert.ToDateTime(row["ImportedAt"]).ToString("yyyy-MM-dd HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture),false),
                                    CsvCell(Convert.ToString(row["ImportedBy"],System.Globalization.CultureInfo.InvariantCulture),true),
                                    CsvCell(Convert.ToString(row["RowCount"],System.Globalization.CultureInfo.InvariantCulture),false),
                                    CsvCell(Convert.ToString(row["TotalIncoming"],System.Globalization.CultureInfo.InvariantCulture),false),
                                    CsvCell(Convert.ToString(row["TotalOutgoing"],System.Globalization.CultureInfo.InvariantCulture),false)
                                };
                                sb.AppendLine(String.Join(",",cells));
                            }
                            File.WriteAllText(save.FileName,sb.ToString(),new UTF8Encoding(true));
                            MessageBox.Show((fa?"تعداد رکوردهای خروجی: ":"Exported rows: ")+exportData.Rows.Count,dialog.Text);
                        }
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}
                };
                preview.Click+=(sender,args)=>
                {
                    try
                    {
                        DataTable printData=getFilteredExportData();
                        using(var document=new PrintDocument())
                        {
                            document.DocumentName=fa?"گزارش تاریخچه ورود بانکی":"Bank import history report";
                            document.DefaultPageSettings.Landscape=true;
                            int rowIndex=0, pageNumber=0;
                            document.PrintPage+=(printSender,printArgs)=>
                            {
                                Graphics g=printArgs.Graphics;
                                Rectangle bounds=printArgs.MarginBounds;
                                using(var titleFont=new Font("Segoe UI",12,FontStyle.Bold))
                                using(var headerFont=new Font("Segoe UI",8,FontStyle.Bold))
                                using(var bodyFont=new Font("Segoe UI",7,FontStyle.Regular))
                                using(var linePen=new Pen(Color.LightGray))
                                {
                                    int y=bounds.Top;
                                    string heading=fa?"گزارش تاریخچه ورود بانکی":"Bank import history report";
                                    g.DrawString(heading,titleFont,Brushes.Black,bounds.Left,y); y+=30;
                                    g.DrawString((fa?"بازه: ":"Period: ")+from.Value.ToShortDateString()+" - "+until.Value.ToShortDateString()+
                                        (String.IsNullOrWhiteSpace(user.Text)?"":" | "+(fa?"کاربر: ":"User: ")+user.Text),bodyFont,Brushes.Black,bounds.Left,y); y+=22;
                                    string[] headers={"Import ID","Imported at","Imported by","Rows","Incoming","Outgoing"};
                                    int[] widths={190,125,120,65,105,105};
                                    int x=bounds.Left;
                                    for(int i=0;i<headers.Length;i++){g.DrawString(headers[i],headerFont,Brushes.Black,x,y);x+=widths[i];}
                                    y+=18;g.DrawLine(linePen,bounds.Left,y,bounds.Right,y);y+=5;
                                    while(rowIndex<printData.Rows.Count && y+22<bounds.Bottom-20)
                                    {
                                        DataRow r=printData.Rows[rowIndex++]; x=bounds.Left;
                                        string[] values={Convert.ToString(r["ImportId"]),Convert.ToDateTime(r["ImportedAt"]).ToString("yyyy-MM-dd HH:mm"),
                                            Convert.ToString(r["ImportedBy"]),Convert.ToString(r["RowCount"]),
                                            Convert.ToDecimal(r["TotalIncoming"]).ToString("N2"),Convert.ToDecimal(r["TotalOutgoing"]).ToString("N2")};
                                        for(int i=0;i<values.Length;i++){g.DrawString(values[i],bodyFont,Brushes.Black,new RectangleF(x,y,widths[i]-4,18));x+=widths[i];}
                                        y+=20;
                                    }
                                    pageNumber++;
                                    g.DrawString((fa?"صفحه ":"Page ")+pageNumber+" | "+printData.Rows.Count,
                                        bodyFont,Brushes.DimGray,bounds.Left,bounds.Bottom-15);
                                    printArgs.HasMorePages=rowIndex<printData.Rows.Count;
                                }
                            };
                            using(var dialogPreview=new PrintPreviewDialog { Document=document,Width=1000,Height=700 })
                                dialogPreview.ShowDialog(dialog);
                        }
                    }
                    catch(Exception ex){MessageBox.Show(ex.Message,dialog.Text);}
                };
                dialog.Controls.Add(grid);dialog.Controls.Add(status);dialog.Controls.Add(tools);
                loadPage();dialog.ShowDialog(this);
            }
        }

        private static string CsvCell(string value,bool protectFormula)
        {
            value=value??String.Empty;
            if(protectFormula)
            {
                int index=0;
                while(index<value.Length && (Char.IsWhiteSpace(value[index]) || Char.IsControl(value[index]))) index++;
                if(index<value.Length && "=+-@".IndexOf(value[index])>=0) value="'"+value;
            }
            return "\""+value.Replace("\"","\"\"")+"\"";
        }

        private void ShowDashboard()
        {
            var table=BankReconciliationService.GetStatement(SelectedAccount,_from.Value,_until.Value);
            int openCount=0,matchedCount=0;
            decimal openIncoming=0m,openOutgoing=0m,matchedIncoming=0m,matchedOutgoing=0m;
            foreach(DataRow row in table.Rows)
            {
                decimal amount=Convert.ToDecimal(row["Amount"]);
                if(Convert.ToString(row["Status"])=="Matched")
                {
                    matchedCount++;
                    if(amount>0)matchedIncoming+=amount;else matchedOutgoing+=-amount;
                }
                else
                {
                    openCount++;
                    if(amount>0)openIncoming+=amount;else openOutgoing+=-amount;
                }
            }
            using(var dialog=new Form { Text=LocalizationService.IsPersian?"داشبورد مغایرت بانکی":"Bank reconciliation dashboard",
                Width=650,Height=460,StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(242,246,251) })
            {
                var panel=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=4 };
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
                AddMetric(panel,0,0,LocalizationService.IsPersian?"ردیف‌های باز":"Open lines",openCount.ToString("N0"),Color.FromArgb(178,72,43));
                AddMetric(panel,1,0,LocalizationService.IsPersian?"ردیف‌های تطبیق شده":"Matched lines",matchedCount.ToString("N0"),Color.FromArgb(26,119,93));
                AddMetric(panel,0,1,LocalizationService.IsPersian?"ورودی‌های باز":"Open incoming",openIncoming.ToString("N2")+" IRR",Color.FromArgb(178,72,43));
                AddMetric(panel,1,1,LocalizationService.IsPersian?"خروجی‌های باز":"Open outgoing",openOutgoing.ToString("N2")+" IRR",Color.FromArgb(178,72,43));
                AddMetric(panel,0,2,LocalizationService.IsPersian?"ورودی‌های تطبیق شده":"Matched incoming",matchedIncoming.ToString("N2")+" IRR",Color.FromArgb(26,119,93));
                AddMetric(panel,1,2,LocalizationService.IsPersian?"خروجی‌های تطبیق شده":"Matched outgoing",matchedOutgoing.ToString("N2")+" IRR",Color.FromArgb(26,119,93));
                int total=openCount+matchedCount;
                AddMetric(panel,0,3,LocalizationService.IsPersian?"درصد تطبیق":"Match rate",(total==0?0m:100m*matchedCount/total).ToString("N1")+"%",Color.FromArgb(32,77,128));
                AddMetric(panel,1,3,LocalizationService.IsPersian?"خالص مبالغ باز":"Net open",(openIncoming-openOutgoing).ToString("N2")+" IRR",Color.FromArgb(32,77,128));
                dialog.Controls.Add(panel);dialog.ShowDialog(this);
            }
        }
        private static void AddMetric(TableLayoutPanel panel,int column,int row,string title,string value,Color accent)
        {
            var card=new Panel { Dock=DockStyle.Fill,Margin=new Padding(6),Padding=new Padding(12),BackColor=Color.White };
            var label=new Label { Dock=DockStyle.Top,Height=30,Text=title,ForeColor=Color.FromArgb(85,96,110) };
            var number=new Label { Dock=DockStyle.Fill,Text=value,Font=new Font("Segoe UI",14,FontStyle.Bold),ForeColor=accent,
                TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true };
            card.Controls.Add(number);card.Controls.Add(label);panel.Controls.Add(card,column,row);
        }
        private void AddLine()
        {
            using(var dialog=new Form{Text=LocalizationService.IsPersian?"ردیف صورتحساب":"Statement entry",Width=480,Height=370,StartPosition=FormStartPosition.CenterParent})
            {
                var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true,Padding=new Padding(12)};
                var date=new DateTimePicker{Format=DateTimePickerFormat.Short,Dock=DockStyle.Top};
                var reference=new TextBox{Dock=DockStyle.Top,MaxLength=100};
                var amount=new NumericUpDown{Dock=DockStyle.Top,Minimum=-1000000000000m,Maximum=1000000000000m,DecimalPlaces=2,Value=1m};
                var note=new TextBox{Dock=DockStyle.Top,MaxLength=500};
                var save=new Button{Text=LocalizationService.IsPersian?"ثبت":"Save",DialogResult=DialogResult.OK,Dock=DockStyle.Top};
                panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"تاریخ":"Date"});panel.Controls.Add(date);
                panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"شناسه بانکی یکتا":"Unique bank reference"});panel.Controls.Add(reference);
                panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"مبلغ علامت دار (+ دریافت، - برداشت)":"Signed amount (+ incoming, - outgoing)"});panel.Controls.Add(amount);
                panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"شرح":"Description"});panel.Controls.Add(note);panel.Controls.Add(save);
                dialog.Controls.Add(panel);dialog.AcceptButton=save;
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                BankReconciliationService.AddStatementLine(SelectedAccount,date.Value,reference.Text,amount.Value,note.Text);LoadLines();
            }
        }
        private void ImportCsv()
        {
            using(var dialog=new OpenFileDialog { Filter="Bank statements (*.csv;*.xlsx)|*.csv;*.xlsx|CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx",CheckFileExists=true })
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                int[] mappedColumns;
                bool hasHeader;
                if (!PromptColumnMapping(out mappedColumns, out hasHeader)) return;
                var table=String.Equals(Path.GetExtension(dialog.FileName),".xlsx",StringComparison.OrdinalIgnoreCase)
                    ? BankStatementXlsx.ReadPreview(dialog.FileName,mappedColumns,hasHeader)
                    : BankStatementCsv.ReadPreview(File.ReadAllText(dialog.FileName,Encoding.UTF8),mappedColumns,hasHeader);
                if(table.Rows.Count==0)throw new InvalidOperationException("Statement file contains no lines.");
                var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow row in table.Rows)
                {
                    string reference=(Convert.ToString(row["Reference"])??"").Trim();
                    if(reference.Length>0 && reference.Length<=100)references.Add(reference);
                }
                var existing = BankReconciliationService.FindExistingReferences(SelectedAccount, references);
                using (var preview = new Form { Text=LocalizationService.IsPersian?"بررسی و اصلاح صورتحساب":"Review and correct statement", Width=900,Height=570,StartPosition=FormStartPosition.CenterParent })
                {
                    var grid = UiFactory.Grid();
                    grid.DataSource=table.DefaultView;grid.Dock=DockStyle.Fill;
                    grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
                    grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(246,249,253);
                    grid.RowHeadersWidth=55;
                    grid.SelectionMode=DataGridViewSelectionMode.CellSelect;
                    grid.RightToLeft=LocalizationService.IsPersian ? RightToLeft.Yes : RightToLeft.No;
                    grid.ReadOnly=false;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;
                    grid.Columns["Status"].ReadOnly=true;
                    if (grid.Columns.Contains("SourceRow")) grid.Columns["SourceRow"].ReadOnly=true;
                    var status = new Label { Dock=DockStyle.Bottom,Height=34,TextAlign=ContentAlignment.MiddleLeft,
                        Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(12,3,12,3),
                        BackColor=Color.FromArgb(230,238,249),ForeColor=Color.FromArgb(29,62,99) };
                    var issueFilter = new CheckBox { Dock=DockStyle.Bottom,Height=30,
                        Text=LocalizationService.IsPersian?"فقط ردیف‌های دارای خطا":"Show issues only" };
                    var validate = new Button { Dock=DockStyle.Bottom,Height=38,
                        Text=LocalizationService.IsPersian?"اعتبارسنجی دوباره":"Revalidate rows" };
                    var confirm = new Button { Dock=DockStyle.Bottom,Height=44,
                        Text=LocalizationService.IsPersian?"ثبت همه ردیف‌های معتبر":"Import all valid rows",Enabled=false };
                    List<BankStatementCsvRow> validated = null;
                    Action review = () =>
                    {
                        grid.EndEdit();
                        var manager = grid.BindingContext[table.DefaultView] as CurrencyManager;
                        if (manager != null) manager.EndCurrentEdit();
                        validated = BankImportPreviewValidator.Validate(table, existing, LocalizationService.IsPersian);
                        int invalid = table.Rows.Count - validated.Count;
                        status.Text=(LocalizationService.IsPersian?"آماده: ":"Ready: ")+validated.Count+
                            (LocalizationService.IsPersian?" | نیازمند اصلاح: ":" | Issues: ")+invalid;
                        confirm.Enabled=validated.Count>0 && invalid==0;
                        status.ForeColor=invalid==0 ? Color.FromArgb(24,112,79) : Color.FromArgb(154,63,38);
                        table.DefaultView.RowFilter=issueFilter.Checked ? "Status <> 'Ready'" : "";
                        foreach(DataGridViewRow visual in grid.Rows)
                        {
                            if(visual.IsNewRow)continue;
                            var data=visual.DataBoundItem as DataRowView;
                            if(data==null)continue;
                            bool valid=String.Equals(Convert.ToString(data.Row["Status"]),"Ready",StringComparison.Ordinal);
                            visual.DefaultCellStyle.BackColor=valid ? Color.Honeydew : Color.MistyRose;
                            visual.ErrorText=valid ? "" : Convert.ToString(data.Row["Status"]);
                        }
                    };
                    grid.CellValueChanged+=(sender,args)=>{validated=null;confirm.Enabled=false;};
                    issueFilter.CheckedChanged+=(sender,args)=>{try { review(); } catch(Exception ex) { MessageBox.Show(ex.Message,preview.Text); }};
                    grid.DataError+=(sender,args)=>{args.ThrowException=false;status.Text=LocalizationService.IsPersian?"خطا در ویرایش سلول":"Cell edit error";};
                    validate.Click+=(sender,args)=>{try { review(); } catch(Exception ex) { MessageBox.Show(ex.Message,preview.Text); }};
                    confirm.Click+=(sender,args)=>
                    {
                        try
                        {
                            review();
                            if (!confirm.Enabled || validated==null) return;
                            if (MessageBox.Show(LocalizationService.IsPersian?"تمام ردیف‌ها ثبت شوند؟":"Import all rows?",
                                preview.Text,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
                            // Re-check duplicates just before the atomic database import.
                            var currentReferences=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach(DataRow r in table.Rows) currentReferences.Add((Convert.ToString(r["Reference"])??"").Trim());
                            var latest=BankReconciliationService.FindExistingReferences(SelectedAccount,currentReferences);
                            // Use the fresh database snapshot for the final validation; do not
                            // accidentally reuse rows validated against an older snapshot.
                            validated=BankImportPreviewValidator.Validate(table,latest,LocalizationService.IsPersian);
                            if (validated.Count!=table.Rows.Count)
                            {
                                existing=latest;
                                review();
                                MessageBox.Show(LocalizationService.IsPersian ? "یک یا چند ردیف نیازمند اصلاح است. دوباره بررسی کنید." : "One or more rows require correction. Review again.",preview.Text);
                                return;
                            }
                            int added=BankReconciliationService.ImportStatementBatch(SelectedAccount,validated);
                            preview.DialogResult=DialogResult.OK;
                            MessageBox.Show((LocalizationService.IsPersian?"ردیف‌های ثبت‌شده: ":"Imported rows: ")+added,Text);
                        }
                        catch(System.Data.SqlClient.SqlException ex)
                        {
                            // SQL unique constraints remain authoritative under concurrent imports.
                            bool duplicate=ex.Number==2601 || ex.Number==2627;
                            BankReconciliationService.TryRecordImportFailure(SelectedAccount,table.Rows.Count,
                                duplicate ? "DuplicateReference" : "DatabaseError");
                            MessageBox.Show(duplicate
                                ? (LocalizationService.IsPersian ? "یک شناسه مرجع هم‌زمان توسط کاربر دیگری ثبت شده است. پیش‌نمایش را دوباره بررسی کنید." : "A bank reference was imported concurrently. Revalidate the preview.")
                                : (LocalizationService.IsPersian ? "ثبت بانکی ناموفق بود. هیچ ردیفی از این دسته نباید به‌صورت ناقص ثبت شود. جزئیات: " : "Bank import failed; the batch transaction was rolled back. Details: ")+ex.Message,
                                preview.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);
                        }
                        catch(Exception ex)
                        {
                            BankReconciliationService.TryRecordImportFailure(SelectedAccount,table.Rows.Count,"ValidationError");
                            MessageBox.Show(ex.Message,preview.Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);
                        }
                    };
                    var exportIssues = new Button { Dock=DockStyle.Bottom,Height=38,
                        Text=LocalizationService.IsPersian?"خروجی خطاها (CSV)":"Export issues (CSV)" };
                    exportIssues.Click+=(sender,args)=>{review();ExportImportIssues(table);};
                    preview.Controls.Add(grid);preview.Controls.Add(status);preview.Controls.Add(issueFilter);preview.Controls.Add(confirm);
                    preview.Controls.Add(exportIssues);preview.Controls.Add(validate);
                    review();
                    if(preview.ShowDialog(this)==DialogResult.OK)LoadLines();
                }
            }
        }
        private bool PromptColumnMapping(out int[] columns, out bool hasHeader)
        {
            columns = null; hasHeader = true;
            using (var dialog = new Form { Text=LocalizationService.IsPersian ? "نگاشت ستون‌های بانک" : "Bank column mapping", Width=420, Height=360, StartPosition=FormStartPosition.CenterParent })
            {
                var panel = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=6, Padding=new Padding(14) };
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,45));
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,55));
                var profiles = BankImportProfileStore.Load();
                var profileSelect = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, Width=150 };
                profileSelect.Items.Add(LocalizationService.IsPersian ? "انتخاب قالب ذخیره‌شده" : "Select saved profile");
                foreach (var profile in profiles) profileSelect.Items.Add(profile.Name);
                profileSelect.SelectedIndex=0;
                var selectors = new ComboBox[4];
                string[] labels = { "Date / تاریخ", "Reference / مرجع", "Amount / مبلغ", "Description / شرح" };
                for (int i=0;i<4;i++)
                {
                    panel.Controls.Add(new Label { Text=labels[i], Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft },0,i);
                    var selector = new ComboBox { Dock=DockStyle.Fill, DropDownStyle=ComboBoxStyle.DropDownList };
                    for (char c='A';c<='Z';c++) selector.Items.Add(c.ToString());
                    selector.SelectedIndex=i; selectors[i]=selector; panel.Controls.Add(selector,1,i);
                }
                var header=new CheckBox { Text=LocalizationService.IsPersian ? "ردیف اول عنوان ستون‌هاست" : "First row contains headers", Checked=true, AutoSize=true };
                var profileBar=new FlowLayoutPanel { Dock=DockStyle.Top, AutoSize=true, WrapContents=true };
                var saveProfile=new Button { Text=LocalizationService.IsPersian?"ذخیره قالب":"Save profile",AutoSize=true };
                var deleteProfile=new Button { Text=LocalizationService.IsPersian?"حذف قالب":"Delete profile",AutoSize=true };
                profileBar.Controls.Add(profileSelect); profileBar.Controls.Add(saveProfile); profileBar.Controls.Add(deleteProfile);
                profileSelect.SelectedIndexChanged+=(sender,args)=>
                {
                    if(profileSelect.SelectedIndex<1)return;
                    var selected=profiles[profileSelect.SelectedIndex-1];
                    for(int i=0;i<4;i++)selectors[i].SelectedIndex=selected.Columns[i];
                    header.Checked=selected.HasHeader;
                };
                saveProfile.Click+=(sender,args)=>
                {
                    using(var nameDialog=new Form { Text=LocalizationService.IsPersian?"نام قالب":"Profile name",Width=370,Height=145,StartPosition=FormStartPosition.CenterParent })
                    {
                        var nameBox=new TextBox { Dock=DockStyle.Top,MaxLength=60 };
                        var save=new Button { Text=LocalizationService.IsPersian?"ذخیره":"Save",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK };
                        nameDialog.Controls.Add(nameBox);nameDialog.Controls.Add(save);nameDialog.AcceptButton=save;
                        if(nameDialog.ShowDialog(dialog)!=DialogResult.OK)return;
                        try
                        {
                            var mapping=new int[4];for(int i=0;i<4;i++)mapping[i]=selectors[i].SelectedIndex;
                            BankStatementCsv.ValidateColumns(mapping);
                            BankImportProfileStore.Save(new BankImportProfile { Name=nameBox.Text,Columns=mapping,HasHeader=header.Checked });
                            profiles=BankImportProfileStore.Load();profileSelect.Items.Clear();
                            profileSelect.Items.Add(LocalizationService.IsPersian?"انتخاب قالب ذخیره‌شده":"Select saved profile");
                            foreach(var profile in profiles)profileSelect.Items.Add(profile.Name);
                            profileSelect.SelectedIndex=0;
                        }
                        catch(Exception ex) { MessageBox.Show(ex.Message,dialog.Text); }
                    }
                };
                deleteProfile.Click+=(sender,args)=>
                {
                    if(profileSelect.SelectedIndex<1)return;
                    if(MessageBox.Show(LocalizationService.IsPersian?"قالب انتخابی حذف شود؟":"Delete selected profile?",dialog.Text,MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
                    try
                    {
                        BankImportProfileStore.Delete(profiles[profileSelect.SelectedIndex-1].Name);
                        profiles=BankImportProfileStore.Load();profileSelect.Items.Clear();
                        profileSelect.Items.Add(LocalizationService.IsPersian?"انتخاب قالب ذخیره‌شده":"Select saved profile");
                        foreach(var profile in profiles)profileSelect.Items.Add(profile.Name);
                        profileSelect.SelectedIndex=0;
                    }
                    catch(Exception ex) { MessageBox.Show(ex.Message,dialog.Text); }
                };
                panel.Controls.Add(header,0,4);panel.SetColumnSpan(header,2);
                var ok=new Button { Text=LocalizationService.IsPersian?"ادامه":"Continue",DialogResult=DialogResult.OK,Dock=DockStyle.Fill };
                panel.Controls.Add(ok,0,5);panel.SetColumnSpan(ok,2);
                dialog.Controls.Add(panel);dialog.Controls.Add(profileBar);dialog.AcceptButton=ok;
                if(dialog.ShowDialog(this)!=DialogResult.OK)return false;
                var result=new int[4]; for(int i=0;i<4;i++)result[i]=selectors[i].SelectedIndex;
                BankStatementCsv.ValidateColumns(result);
                columns=result;hasHeader=header.Checked;return true;
            }
        }
        private void ExportImportIssues(DataTable preview)
        {
            using(var dialog=new SaveFileDialog { Filter="CSV (*.csv)|*.csv",FileName="bank-import-issues.csv" })
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                var sb=new StringBuilder("SourceRow,Date,Reference,Amount,Description,Status\r\n");
                int rowNumber=1;
                foreach(DataRow row in preview.Rows)
                {
                    rowNumber++;
                    int sourceRow = preview.Columns.Contains("SourceRow") ? Convert.ToInt32(row["SourceRow"]) : rowNumber;
                    if (Convert.ToString(row["Status"])=="Ready") continue;
                    var cells=new[] { sourceRow.ToString(),Convert.ToString(row["Date"]),
                        Convert.ToString(row["Reference"]),Convert.ToString(row["Amount"]),
                        Convert.ToString(row["Description"]),Convert.ToString(row["Status"]) };
                    for(int i=0;i<cells.Length;i++)
                    {
                        if(i>0)sb.Append(',');
                        var cell=cells[i]??"";
                        if(cell.Length>0 && "=+-@\t\r\n".IndexOf(cell[0])>=0)cell="'"+cell;
                        sb.Append('"').Append(cell.Replace("\"","\"\"")).Append('"');
                    }
                    sb.Append("\r\n");
                }
                File.WriteAllText(dialog.FileName,sb.ToString(),new UTF8Encoding(true));
                MessageBox.Show(LocalizationService.IsPersian?"گزارش ذخیره شد.":"Report saved.",Text);
            }
        }
        private void ExportCsv()
        {
            var table=BankReconciliationService.GetStatement(SelectedAccount,_from.Value,_until.Value);
            using(var dialog=new SaveFileDialog { Filter="CSV (*.csv)|*.csv",FileName="bank-reconciliation.csv" })
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                File.WriteAllText(dialog.FileName,BankStatementCsv.Export(table),new UTF8Encoding(true));
                MessageBox.Show(LocalizationService.IsPersian?"گزارش ذخیره شد.":"Report saved.",Text);
            }
        }
        private void SuggestSelected()
        {
            if(_grid.CurrentRow==null)throw new InvalidOperationException("Select an unmatched statement line.");
            var row=_grid.CurrentRow.DataBoundItem as DataRowView;
            if(row==null || Convert.ToString(row["Status"])!="Open")
                throw new InvalidOperationException("Select an open statement line.");
            int statementId=Convert.ToInt32(row["Id"]);
            var suggestions=BankReconciliationService.SuggestPayments(statementId,3);
            if(suggestions.Rows.Count==0)
            {
                MessageBox.Show(LocalizationService.IsPersian?"مورد مشابهی پیدا نشد.":"No candidates found.",Text);return;
            }
            using(var dialog=new Form { Text=LocalizationService.IsPersian?"پیشنهادهای تطبیق":"Suggested payments",Width=680,Height=380,StartPosition=FormStartPosition.CenterParent })
            {
                var grid=UiFactory.Grid();grid.DataSource=suggestions;grid.Dock=DockStyle.Fill;
                var approve=new Button { Text=LocalizationService.IsPersian?"تأیید تطبیق انتخابی":"Confirm selected match",Dock=DockStyle.Bottom,Height=42 };
                approve.Click+=(sender,args)=>
                {
                    var selected=grid.CurrentRow==null?null:grid.CurrentRow.DataBoundItem as DataRowView;
                    if(selected==null)return;
                    if(MessageBox.Show(LocalizationService.IsPersian?"این تراکنش تطبیق داده شود؟":"Match this payment?",Text,
                        MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
                    try { BankReconciliationService.MatchPayment(statementId,Convert.ToInt32(selected["PaymentId"]));dialog.DialogResult=DialogResult.OK;dialog.Close(); }
                    catch(Exception ex) { MessageBox.Show(ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning); }
                };
                dialog.Controls.Add(grid);dialog.Controls.Add(approve);
                dialog.ShowDialog(this);
            }
            LoadLines();
        }
        private void UnmatchSelected()
        {
            if (_grid.CurrentRow == null) throw new InvalidOperationException("Select a matched statement line.");
            var row = _grid.CurrentRow.DataBoundItem as DataRowView;
            if (row == null || Convert.ToString(row["Status"]) != "Matched")
                throw new InvalidOperationException("Select a matched statement line.");
            using (var dialog = new Form { Text=LocalizationService.IsPersian?"علت لغو تطبیق":"Reason for undo", Width=470,Height=220,StartPosition=FormStartPosition.CenterParent })
            {
                var panel = new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(12) };
                var label = new Label { Text=LocalizationService.IsPersian?"علت لغو تطبیق (الزامی):":"Reason for undo (required):",AutoSize=true };
                var reason = new TextBox { Width=410,MaxLength=500 };
                var save = new Button { Text=LocalizationService.IsPersian?"تأیید لغو":"Confirm undo",Width=160 };
                save.Click+=(sender,args)=> { dialog.DialogResult=DialogResult.OK; dialog.Close(); };
                panel.Controls.Add(label);panel.Controls.Add(reason);panel.Controls.Add(save);
                dialog.Controls.Add(panel);
                if (dialog.ShowDialog(this)!=DialogResult.OK) return;
                if (MessageBox.Show(LocalizationService.IsPersian?"تطبیق لغو شود؟":"Undo this match?",Text,
                    MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
                BankReconciliationService.Unmatch(Convert.ToInt32(row["Id"]),reason.Text);
                LoadLines();
            }
        }

        private void MatchSelected()
        {
            if(_grid.CurrentRow==null)throw new InvalidOperationException("Select a statement line.");
            var row=_grid.CurrentRow.DataBoundItem as DataRowView;
            if(row==null || Convert.ToString(row["Status"])!="Open")throw new InvalidOperationException("Select an unmatched statement line.");
            using(var dialog=new Form{Text=LocalizationService.IsPersian?"تطبیق تراکنش":"Match transaction",Width=380,Height=230,StartPosition=FormStartPosition.CenterParent})
            {
                var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,Padding=new Padding(12)};
                var type=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};type.Items.AddRange(new object[]{"Payment","Transfer"});type.SelectedIndex=0;
                var id=new NumericUpDown{Dock=DockStyle.Top,Minimum=1,Maximum=Int32.MaxValue};
                var save=new Button{Text=LocalizationService.IsPersian?"تطبیق":"Match",DialogResult=DialogResult.OK,Dock=DockStyle.Top};
                panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"نوع تراکنش":"Transaction type"});panel.Controls.Add(type);
                panel.Controls.Add(new Label{Text=LocalizationService.IsPersian?"شناسه تراکنش":"Transaction ID"});panel.Controls.Add(id);panel.Controls.Add(save);
                dialog.Controls.Add(panel);dialog.AcceptButton=save;
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                var lineId=Convert.ToInt32(row["Id"]);
                if(type.SelectedIndex==0)BankReconciliationService.MatchPayment(lineId,Convert.ToInt32(id.Value));
                else BankReconciliationService.MatchTransfer(lineId,Convert.ToInt32(id.Value));
                LoadLines();
            }
        }
    }
}
