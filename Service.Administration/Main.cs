using System;
using System.ComponentModel;
using System.Data;
using System.ServiceProcess;
using System.Windows.Forms;
using Service.Shared;
using Service.Administration.Controllers;
using Service.Administration.Views;
using Service.Shared.Utils;

namespace Service.Administration; 

public partial class Main : Form, IMain {
    #region Variables

    private readonly MainController controller;

    private bool close;
    private int  currentRow;

    #endregion

    #region Properties

    public string ServerName {
        get => txtServer.Text;
        set => txtServer.Text = value;
    }

    public int CurrentRow {
        get => currentRow;
        set {
            currentRow = value;
            Database   = value >= 0 ? dg.Rows[value].Cells["gridName"].Value.ToString() : string.Empty;
        }
    }

    public string Database { get; set; }


    public bool ActiveOnly {
        get => chkShowActiveOnly.Checked;
        set => chkShowActiveOnly.Checked = value;
    }

    public DataView Source {
        set => dg.DataSource = value;
    }

    #endregion

    #region Initializer

    public Main() {
        CheckForIllegalCrossThreadCalls = false;
        InitializeComponent();
        dg.AutoGenerateColumns = false;
        controller             = new MainController(this, this);
    }

    #endregion

    #region Events

    private void NotifyIconDoubleCLick(object sender, EventArgs e) => RestoreForm();

    private void MainFormClosing(object sender, FormClosingEventArgs e) {
        if (close || e.CloseReason == CloseReason.WindowsShutDown)
            return;
        e.Cancel = true;
        MinimizeForm();
    }

    private void NotifyMenuItemClick(object sender, ToolStripItemClickedEventArgs e) {
        switch (e.ClickedItem.Name) {
            case "mnuOpen":
                RestoreForm();
                break;
            case "mnuAbout":
                var about = new About();
                about.ShowDialog();
                break;
            case "mnuExit":
                close = true;
                Application.Exit();
                break;
        }
    }

    private void CloseButtonClicked(object sender, EventArgs e) => MinimizeForm();

    private void FormLoad(object sender, EventArgs e) => controller.LoadData();

    private void chkShowActiveOnly_CheckedChanged(object sender, EventArgs e) => controller.FilterData();

    private void dg_CellContentClick(object sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex == -1)
            return;
        string value = dg.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
        if (string.IsNullOrEmpty(value))
            return;
        switch (dg.Columns[e.ColumnIndex].Name) {
            case "gridStartStop":
                CurrentRow = e.RowIndex;
                switch (value) {
                    case "Stop":
                        controller.StopService();
                        break;
                    case "Start":
                        controller.StartService();
                        break;
                }

                break;
            case "gridRestart" when value == "Restart":
                CurrentRow = e.RowIndex;
                controller.RestartService();
                break;
            case "gridActive":
                CurrentRow = e.RowIndex;
                controller.ChangeServiceActive();
                break;
        }
    }

    private void dg_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
        if (dg.CurrentCell.ColumnIndex == gridActive.Index)
            dg.CancelEdit();
    }

    private void dg_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e) {
        if (e.Button != MouseButtons.Right)
            return;
        CurrentRow = e.RowIndex;
        bool active = e.RowIndex >= 0 && IsActive;
        mnuReloadSettings.Enabled = active;
        rightClickMenu.Enabled    = active;
        CurrentRow                = e.RowIndex;
    }

    private void dg_CellMouseEnter(object sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0)
            return;
        var column = dg.Columns[e.ColumnIndex];
        if (column.Name is not ("gridName" or "gridDesc" or "gridAccount"))
            return;
        var cell = dg.Rows[e.RowIndex].Cells[column.Name];
        if (cell.Value is string value)
            cell.ToolTipText = value;
    }

    private void rightClickMenu_Opening(object sender, CancelEventArgs e) {
        bool enabled = false;
        if (CurrentRow >= 0 && IsActive) {
            using var service = controller.LoadController();
            enabled = service.Status == ServiceControllerStatus.Running;
        }

        mnuReTestProcess.Enabled    = enabled;
        mnuReloadSettings.Enabled   = enabled;
        mnuAPISettings.Enabled      = enabled;
        mnuServiceAccount.Enabled   = IsActive;
        mnuPrintAPISettings.Enabled = enabled && RestAPISettings.Load(APISettings.SettingsFilePath(Database)).Enabled;
    }

    private void ReloadSettingsClicked(object  sender, EventArgs e) => controller.ReloadSettings();
    private void APISettingsClicked(object     sender, EventArgs e) => controller.OpenAPISettings();
    private void PrintAPISettings_Click(object sender, EventArgs e) => controller.OpenPrintAPISettings();
    private void ServiceAccountClicked(object  sender, EventArgs e) => controller.OpenServiceAccount();

    private void MainKeyDown(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Escape)
            btnClose.PerformClick();
    }

    #endregion

    #region Methods

    private void RestoreForm() {
        try {
            //WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Visible       = true;
        }
        catch (Exception ex) {
#if DEBUG
            MessageBox.Show(ex.Message);
#endif
        }
    }


    public void MinimizeForm() {
        try {
            Visible = false;
            //WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }
        catch (Exception ex) {
#if DEBUG
            MessageBox.Show(ex.Message);
#endif
        }
    }

    public bool IsActive => dg.Rows[CurrentRow].Cells["gridActive"].Value.ToString() == "Y";

    public void ClearSelection() {
        dg.ClearSelection();
        dg.CurrentCell = null;
    }

    public void SetIsActive(bool value) => dg.Rows[CurrentRow].Cells["gridActive"].Value = value.ToYesNo();

    #endregion
}