using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Service.Shared;
using Service.Administration.Controllers;
using Service.Administration.Views;
using Form = System.Windows.Forms.Form;

namespace Service.Administration; 

public partial class APISettings : Form, IAPISettings {
    #region Variables

    private readonly APISettingsControllers controller;

    #endregion

    #region Properties

    public int CurrentPort {
        get => (int)txtPort.Value;
        set => txtPort.Value = value;
    }

    public bool EnableRedisServer {
        get => chkRedisServer.Checked;
        set => chkRedisServer.Checked = value;
    }

    public string RedisServer {
        get => txtRedisServer.Text;
        set => txtRedisServer.Text = value;
    }

    public int Nodes {
        get => (int)txtNodes.Value;
        set => txtNodes.Value = value;
    }

    public int NodesRestart {
        get => (int)txtNodesRestart.Value;
        set => txtNodesRestart.Value = value;
    }

    public int OperationsRestart {
        get => (int)txtOpRestart.Value;
        set => txtOpRestart.Value = value;
    }

    public string          Database { get; set; }
    public RestAPISettings Settings { get; set; }

    public bool EnableLoadBalancing {
        get => chkLB.Checked;
        set => chkLB.Checked = value;
    }

    // ReSharper disable ConvertToAutoProperty
    public DataTable NodesTable => dtNodes;

    public DataTable TokensTable => dtTokens;

    public bool EnableRedisServerName { 
        set => txtRedisServer.ReadOnly = !value;
    }
    // ReSharper restore ConvertToAutoProperty

    public bool Active {
        get => chkActive.Checked;
        set => chkActive.Checked = value;
    }

    public bool Loaded { get; set; }

    #endregion

    #region Events

    public APISettings(string database) {
        Database = database;
        InitializeComponent();
        gridNodes.AutoGenerateColumns = false;
        controller                    = new APISettingsControllers(this, this);
    }

    private void FormLoad(object sender, EventArgs e) {
        Text += " - " + Database;
        controller.LoadSettings();
    }


    private void AcceptClicked(object sender, EventArgs e) {
        if (!controller.ValidateForm())
            return;
        controller.SaveSettings();
        DialogResult = DialogResult.OK;
    }


    private void KeyDownEvent(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Escape)
            DialogResult = DialogResult.Cancel;
    }

    private void TokensCellEdit(object sender, DataGridViewCellEventArgs e) {
        if (!Loaded)
            return;
        var row = gridTokens.Rows[e.RowIndex];
        if (row.Cells["colTokenID"].Value != DBNull.Value)
            return;
        row.Cells["colTokenID"].Value  = Guid.NewGuid();
        row.Cells["colTokenKey"].Value = Guid.NewGuid();
        lblTokenAlert.Visible          = true;
    }

    private void LoadBalancingCheckedChanged(object sender, EventArgs e) => controller.LoadBalancingChanged();

    private void NodesValueChanged(object  sender, EventArgs e) => controller.AddRemoveNodes();
    private void EnableRedisChanged(object sender, EventArgs e) => controller.EnableRedisChanged();

    #endregion

    public void DisplayLoadBalancing() {
        switch (EnableLoadBalancing) {
            case true when !tabControl.TabPages.Contains(tabLB):
                tabControl.TabPages.Add(tabLB);
                break;
            case false when tabControl.TabPages.Contains(tabLB):
                tabControl.TabPages.Remove(tabLB);
                break;
        }
    }

    internal static string SettingsFilePath(string database) => Path.Combine(Application.StartupPath, "Settings", $"{database}.json");
}