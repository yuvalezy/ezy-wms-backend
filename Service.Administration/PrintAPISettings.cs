using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Service.Shared;
using Newtonsoft.Json;
using Service.Shared.Data;
using Form = System.Windows.Forms.Form;
using Object = Service.Shared.Object;

namespace Service.Administration; 

public partial class PrintAPISettings : Form {
    #region Variables

    private readonly string        database;
    private readonly DataConnector data;

    private bool            ready;
    private RestAPISettings settings;
    private int             CurrentObject => (int) cmbObject.SelectedValue;

    #endregion

    #region Form

    public PrintAPISettings(DataConnector data, string database) {
        this.data     = data;
        this.database = database;
        InitializeComponent();
    }

    private void FormLoad(object sender, EventArgs e) {
        Text += " - " + database;
        LoadSettings();
        ready = true;
    }

    private void ErrorMessage(string id, string message) => MessageBox.Show($"{id} Error: {message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void AcceptClicked(object sender, EventArgs e) {
        SaveSettings();
        DialogResult = DialogResult.OK;
    }



    private void KeyDownEvent(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Escape)
            DialogResult = DialogResult.Cancel;
    }

    #endregion

    #region Settings

    private void LoadSettings() {
        try {
            settings = RestAPISettings.Load(APISettings.SettingsFilePath(database));
            LoadPrinters();
            LoadObjects();
            cmbDefaultPrinter.SelectedItem = settings.DefaultPrinter;
        }
        catch (Exception ex) {
            ErrorMessage("Load Settings", ex.Message);
        }
    }

    private void SaveSettings() {
        try {
            //remove empty printer for selection
            settings.Printers.Remove("");
            settings.DefaultPrinter = (string) cmbDefaultPrinter.SelectedItem;
            settings.Save();
        }
        catch (Exception ex) {
            ErrorMessage("Save Ports Manager", ex.Message);
        }
    }

    #endregion

    #region Printers

    #region Printers

    private void LoadPrinters() {
        try {
            var requestParameters = new Dictionary<string, string> {
                {"grant_type", "password"}
            };
            string postData = string.Join("&", requestParameters.Select(kv => kv.Key + "=" + kv.Value).ToArray());
            byte[] bytes    = new ASCIIEncoding().GetBytes(postData);

            var request = (HttpWebRequest) WebRequest.Create($"http://localhost:{settings.Port}/token");
            request.Method        = "POST";
            request.ContentType   = "application/x-www-form-urlencoded";
            request.ContentLength = bytes.Length;
            request.GetRequestStream().Write(bytes, 0, bytes.Length);
            request.ServerCertificateValidationCallback += (_, _, _, _) => true;
            Token token;
            using (var response = (HttpWebResponse) request.GetResponse()) {
                var    stream = response.GetResponseStream();
                var    sr     = new StreamReader(stream);
                string result = sr.ReadToEnd();
                token = JsonConvert.DeserializeObject<Token>(result);
            }

            request        = (HttpWebRequest) WebRequest.Create($"http://localhost:{settings.Port}/api/print/printers?all=true");
            request.Accept = "application/json";
            request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
            using (var response = (HttpWebResponse) request.GetResponse()) {
                var    stream = response.GetResponseStream();
                var    sr     = new StreamReader(stream);
                string result = sr.ReadToEnd();
                var    values = JsonConvert.DeserializeObject<List<string>>(result);
                values.ForEach(value => {
                    int index = chkPrinters.Items.Add(value);
                    if (settings.Printers.Contains(value))
                        chkPrinters.SetItemChecked(index, true);
                });
            }

            //add empty printer for selection
            settings.Printers.Insert(0, "");
            SetPrinterComboBoxSource();
        }
        catch (Exception ex) {
            ErrorMessage("Load Printers", ex.Message);
        }
    }

    private void SetPrinterComboBoxSource(bool keepValue = false) {
        string                          defValue  = string.Empty, objValue = string.Empty;
        List<(int index, string value)> colValues = new();
        if (keepValue) {
            defValue = (string) cmbDefaultPrinter.SelectedItem;
            objValue = (string) cmbObjectDefaultPrinter.SelectedItem;
            for (int i = 0; i < dtLayouts.Rows.Count; i++)
                colValues.Add((i, dtLayouts.Rows[i]["Printer"].ToString()));
        }

        cmbDefaultPrinter.DataSource       = new BindingSource {DataSource = settings.Printers};
        cmbObjectDefaultPrinter.DataSource = new BindingSource {DataSource = settings.Printers};
        colPrinter.DataSource              = new BindingSource {DataSource = settings.Printers};
        if (!keepValue) return;
        cmbDefaultPrinter.SelectedItem       = settings.Printers.Contains(defValue) ? defValue : "";
        cmbObjectDefaultPrinter.SelectedItem = settings.Printers.Contains(objValue) ? objValue : "";
        colValues.ForEach(v => {
            (int index, string value)        = v;
            dtLayouts.Rows[index]["Printer"] = settings.Printers.Contains(value) ? value : "";
        });
    }

    private void chkPrinters_ItemCheck(object sender, ItemCheckEventArgs e) {
        if (!ready) return;
        if (e.NewValue == CheckState.Checked)
            settings.Printers.Add((string) chkPrinters.Items[e.Index]);
        else
            settings.Printers.Remove((string) chkPrinters.Items[e.Index]);
        SetPrinterComboBoxSource(true);
    }

    #endregion

    #endregion

    #region Layouts

    private void gridLayouts_CurrentCellDirtyStateChanged(object sender, EventArgs e) => gridLayouts.CommitEdit(DataGridViewDataErrorContexts.Commit);

    private void gridLayouts_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
        if (!ready || e.RowIndex < 0) return;
        var @object = settings.Objects.FirstOrDefault(o => o.ID == CurrentObject);
        if (@object == null) {
            @object = new Object(CurrentObject);
            settings.Objects.Add(@object);
        }

        string value  = dtLayouts.Rows[e.RowIndex]["Printer"].ToString();
        int    id     = (int) dtLayouts.Rows[e.RowIndex]["ID"];
        var    layout = @object.Layouts.FirstOrDefault(l => l.ID == id);
        if (layout == null && !string.IsNullOrWhiteSpace(value)) {
            layout = new LayoutDefinition {ID = id};
            @object.Layouts.Add(layout);
        }
        else if (layout != null && string.IsNullOrWhiteSpace(value)) {
            @object.Layouts.Remove(layout);
            CheckRemoveObject(@object);
            return;
        }

        layout.DefaultPrinter = value;
    }

    #endregion

    #region Objects

    private void LoadObjects() {
        try {
            cmbObject.ValueMember   = "ID";
            cmbObject.DisplayMember = "Name";
            cmbObject.DataSource    = PrintObjects.ObjectsList;
            cmbObject.SelectedIndex = 0;
            LoadObjectData();
        }
        catch (Exception ex) {
            ErrorMessage("Load Objects", ex.Message);
        }
    }

    private void LoadObjectData() {
        try {
            dtLayouts.Clear();
            var @object = settings.Objects.FirstOrDefault(o => o.ID == CurrentObject);
            cmbObjectDefaultPrinter.SelectedItem = @object != null ? @object.DefaultPrinter : "";
            var dt = data.GetDataTable($@"select T0.""Code"" ID, T0.""U_Name"" ""Name"", T0.""U_Active"" ""Active""
from ""@LWPLM"" T0
inner join ""LWLayouts"" T1 on T1.ID = T0.""U_FileID""
where T0.""U_Type"" = {QueryHelper.Var}Type 
order by 2", new Parameters(new Parameter("Type", SqlDbType.Int, CurrentObject)));
            foreach (DataRow dataRow in dt.Rows) {
                var dr = dtLayouts.NewRow();
                int id = int.Parse((string) dataRow["ID"]);
                dr["ID"]      = id;
                dr["Name"]    = (string) dataRow["Name"];
                dr["Active"]  = (string) dataRow["Active"] == "Y";
                dr["Printer"] = "";
                var layout = @object?.Layouts.FirstOrDefault(l => l.ID == id);
                if (layout != null)
                    dr["Printer"] = layout.DefaultPrinter;
                dtLayouts.Rows.Add(dr);
            }
        }
        catch (Exception ex) {
            ErrorMessage("Load Object Default Printer", ex.Message);
        }
    }

    private void cmbObject_SelectedIndexChanged(object sender, EventArgs e) {
        if (!ready) return;
        LoadObjectData();
        try {
        }
        catch (Exception ex) {
            ErrorMessage("Object Change", ex.Message);
        }
    }

    private void cmbObjectDefaultPrinter_SelectedIndexChanged(object sender, EventArgs e) {
        if (!ready) return;
        try {
            string value   = (string) cmbObjectDefaultPrinter.SelectedItem;
            var    @object = settings.Objects.FirstOrDefault(o => o.ID == CurrentObject);
            if (@object == null && !string.IsNullOrWhiteSpace(value)) {
                @object = new Object(CurrentObject);
                settings.Objects.Add(@object);
            }
            else if (@object != null && string.IsNullOrWhiteSpace(value)) {
                @object.DefaultPrinter = "";
                CheckRemoveObject(@object);
                return;
            }

            if (@object != null)
                @object.DefaultPrinter = value;
        }
        catch (Exception ex) {
            ErrorMessage("Object Default Printer", ex.Message);
        }
    }

    private void CheckRemoveObject(Object @object) {
        if (@object.Layouts.Count == 0 && string.IsNullOrWhiteSpace(@object.DefaultPrinter))
            settings.Objects.Remove(@object);
    }

    #endregion
}