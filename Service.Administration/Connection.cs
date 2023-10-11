using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Service.Shared;
using Microsoft.Win32;
using SAPbobsCOM;
using Service.Shared.Company;
using Service.Shared.Data;
using Service.Shared.Utils;

namespace Service.Administration;

public partial class Connection : Form {
    private bool serverChange;

    private readonly List<BoDataServerTypes> fallbackIndex = new() {
        BoDataServerTypes.dst_MSSQL2014,
        BoDataServerTypes.dst_MSSQL2016,
        BoDataServerTypes.dst_MSSQL2017,
        BoDataServerTypes.dst_MSSQL2019
    };

    public Connection() => InitializeComponent();

    private void frmConn_Load(object sender, EventArgs e) {
        cmbType.SelectedIndex = 1;
        ActiveControl         = txtServer;
    }

    private void exit_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;

    private void btnAccept_Click(object sender, EventArgs e) {
        if (!ValidateValues())
            return;

        var vCmp = new Company {
            Server   = txtServer.Text,
            language = BoSuppLangs.ln_Spanish_La
        };

        switch (cmbType.Text) {
            case "HANA":
                vCmp.DbServerType = BoDataServerTypes.dst_HANADB;
                break;
            case "SQL 2012":
                vCmp.DbServerType = BoDataServerTypes.dst_MSSQL2012;
                break;
            case "SQL 2014":
                vCmp.DbServerType = BoDataServerTypes.dst_MSSQL2014;
                break;
            case "SQL 2016":
                SetDbTypeSql(BoDataServerTypes.dst_MSSQL2016);
                break;
            case "SQL 2017":
                SetDbTypeSql(BoDataServerTypes.dst_MSSQL2017);
                break;
            case "SQL 2019":
                SetDbTypeSql(BoDataServerTypes.dst_MSSQL2019);
                break;
        }

        void SetDbTypeSql(BoDataServerTypes serverType) {
            try {
                vCmp.DbServerType = serverType;
                if (vCmp.DbServerType == 0)
                    Fallback();
            }
            catch {
                Fallback();
            }

            void Fallback() => SetDbTypeSql(fallbackIndex[fallbackIndex.IndexOf(serverType) - 1]);
        }

        try {
            var rs = vCmp.GetCompanyList();

            if (!CheckAdoConnection(vCmp.DbServerType)) {
                DialogResult = DialogResult.Cancel;
                return;
            }

            SaveRegistry(vCmp.DbServerType);
            if (vCmp.Connected)
                vCmp.Disconnect();
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception) {
            MessageBox.Show(exception.Message);
        }
    }

    private string ServerUser => txtServerUser.Text;

    private string ServerPassword => txtServerPassword.Text;

    private bool ValidateValues() {
        if (string.IsNullOrEmpty(txtServer.Text)) {
            MessageBox.Show("You must enter a valid server name or address", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtServer.Focus();
            return false;
        }

        if (string.IsNullOrEmpty(ServerUser)) {
            MessageBox.Show("You must enter the server user name", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtServerUser.Focus();
            return false;
        }

        if (string.IsNullOrEmpty(ServerPassword)) {
            MessageBox.Show("You must enter the server password", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtServerPassword.Focus();
            return false;
        }

        return true;
    }

    private bool CheckAdoConnection(BoDataServerTypes dbType) {
        DataConnector data = dbType switch {
            BoDataServerTypes.dst_HANADB => new HANADataConnector(ConnectionString.HanaConnectionString(txtServer.Text, ServerUser, ServerPassword, "SBOCOMMON")),
            _                            => new SQLDataConnector(ConnectionString.SqlConnectionString(txtServer.Text, ServerUser, ServerPassword, "SBO-Common"))
        };
        try {
            data.CheckConnection();
        }
        catch (Exception e) {
            MessageBox.Show(e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            data.Dispose();
            Application.Exit();
        }

        try {
            ConnectionController.DatabaseType = dbType == BoDataServerTypes.dst_HANADB ? DatabaseType.HANA : DatabaseType.SQL;
        }
        catch (Exception e) {
            MessageBox.Show(e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally {
            data.Dispose();
        }

        return true;
    }

    private void SaveRegistry(BoDataServerTypes dbType) {
        try {
            var key = Registry.LocalMachine.OpenSubKey(Const.RegistryPath, true) ?? Registry.LocalMachine.CreateSubKey(Const.RegistryPath);
            key.SetValue("Server", txtServer.Text);
            key.SetValue("ServerType", (int)dbType, RegistryValueKind.DWord);
            key.SetValue("ServerUser", ServerUser.EncryptData());
            key.SetValue("ServerPassword", ServerPassword.EncryptData());
        }
        catch (Exception ex) {
            throw new Exception("Save Registry Error: " + ex.Message);
        }
    }


    private void txtKeyDown(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Enter) btnAccept.PerformClick();
    }
}