using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Service.Crystal.Shared;
using Service.Shared.Company;

namespace Service.Shared.PrintLayout; 

public class LayoutFileSQL : LayoutFile {
    private SqlConnection conn;

    private static readonly Mutex ConnectionMutex = new(false, "lmserconn");

    protected virtual void OpenConnection() {
        ConnectionMutex.WaitOne();
        conn ??= new SqlConnection(ConnectionController.ConnectionString);
        conn.Open();
    }

    protected virtual void CloseConnection() {
        conn.Close();
        conn.Dispose();
        conn = null;
        ConnectionMutex.ReleaseMutex();
    }

    public override byte[] Get(int id) {
        OpenConnection();
        try {
            using var sqlCm = new SqlCommand($"select Package from {Common.LayoutsTable} where ID = @ID", conn);
            sqlCm.Parameters.Add("@ID", SqlDbType.Int).Value = id;
            byte[] content = (byte[])sqlCm.ExecuteScalar();
            return content;
        }
        finally {
            CloseConnection();
        }
    }

    public override int Set(int type, byte[] content, string fileName, string md5, CrystalLayoutParameters parameters) {
        OpenConnection();
        try {
            int id = AddLayout(type, content, fileName, md5, parameters[0].Name);
            AddParameters(id, parameters);
            return id;
        }
        finally {
            CloseConnection();
        }
    }


    public override List<PrintLayoutVariable> GetVariables(int id) {
        OpenConnection();
        try {
            using var cm = new SqlCommand(string.Format(Queries.PrintLayoutVariables, id, Common.LayoutManagerUDT, Common.LayoutsTable), conn);
            using var da = new SqlDataAdapter(cm);
            using var dt = new DataTable();
            da.Fill(dt);
            return (from DataRow dr in dt.Rows select new PrintLayoutVariable(dr)).ToList();
        }
        finally {
            CloseConnection();
        }
    }

    public override T GetValue<T>(string query) {
        OpenConnection();
        try {
            using var cm = new SqlCommand(query, conn);
            return ReadValue<T>(cm.ExecuteScalar());
        }
        finally {
            CloseConnection();
        }
    }


    public override List<LayoutData> GetLayoutsData(int type, string filter, int? specificID, SpecificFilter specificFilter) {
        OpenConnection();
        try {
            using var da = new SqlDataAdapter(Common.GetLayoutsStoredProcedureName, conn);
            using var cm = da.SelectCommand;
            cm.CommandType                                              = CommandType.StoredProcedure;
            cm.Parameters.Add("@Entry", SqlDbType.Int).Value            = type;
            cm.Parameters.Add("@Filter", SqlDbType.NVarChar, 254).Value = filter;
            if (specificID.HasValue)
                cm.Parameters.Add("@ID", SqlDbType.Int).Value = specificID.Value;
            if (specificFilter != null) {
                if (!string.IsNullOrWhiteSpace(specificFilter.ItemCode))
                    cm.Parameters.Add("@ItemCode", SqlDbType.NVarChar, 50).Value = specificFilter.ItemCode;
                if (!string.IsNullOrWhiteSpace(specificFilter.CardCode))
                    cm.Parameters.Add("@CardCode", SqlDbType.NVarChar, 50).Value = specificFilter.CardCode;
                if (!string.IsNullOrWhiteSpace(specificFilter.ShipToCode))
                    cm.Parameters.Add("@ShipToCode", SqlDbType.NVarChar, 50).Value = specificFilter.ShipToCode;
                if (!string.IsNullOrWhiteSpace(specificFilter.CardCode2))
                    cm.Parameters.Add("@CardCode2", SqlDbType.NVarChar, 50).Value = specificFilter.CardCode2;
                if (!string.IsNullOrWhiteSpace(specificFilter.ShipToCode2))
                    cm.Parameters.Add("@ShipToCode2", SqlDbType.NVarChar, 50).Value = specificFilter.ShipToCode2;
            }

            using var dt = new DataTable();
            da.Fill(dt);
            return (from DataRow dr in dt.Rows select new LayoutData(dr)).ToList();
        }
        finally {
            CloseConnection();
        }
    }

    public override void Execute(string query) {
        OpenConnection();
        try {
            using var cm = new SqlCommand(query, conn);
            cm.ExecuteNonQuery();
        }
        finally {
            CloseConnection();
        }
    }

    private int AddLayout(int type, byte[] content, string fileName, string md5, string mainVariable) {
        using var cm = new SqlCommand($@"
set @ID = (select ID from {Common.LayoutsTable} where [Type] = @Type and FileName = @FileName)
if @ID is null Begin 
    set @ID = (select IsNull((select Max(ID)+1 from {Common.LayoutsTable}), 0))
    insert {Common.LayoutsTable}(ID, FileName, Package, Variable, MD5, [Type])
    select @ID, @FileName, @Package, @Variable, @MD5, @Type
end else begin
    update {Common.LayoutsTable} set Package = @Package, Variable = @Variable, MD5 = @MD5 where ID = @ID
    delete {Common.LayoutsTable}Variables where ID = @ID
    delete {Common.LayoutsTable}VariablesValues where ID = @ID
end",
            conn);
        cm.Parameters.Add("@ID", SqlDbType.Int).Direction             = ParameterDirection.Output;
        cm.Parameters.Add("@Type", SqlDbType.Int).Value               = type;
        cm.Parameters.Add("@FileName", SqlDbType.NVarChar, 100).Value = fileName;
        cm.Parameters.Add("@Package", SqlDbType.Binary).Value         = content;
        cm.Parameters.Add("@Variable", SqlDbType.NVarChar, 15).Value  = mainVariable;
        cm.Parameters.Add("@MD5", SqlDbType.NVarChar, 32).Value       = md5;
        cm.ExecuteNonQuery();
        return (int)cm.Parameters["@ID"].Value;
    }

    private void AddParameters(int id, CrystalLayoutParameters parameters) {
        using var cm = new SqlCommand($"insert into {Common.LayoutsTable}Variables(ID, VarID, Variable, Type) values(@ID, @VarID, @Variable, @Type)", conn);
        cm.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        cm.Parameters.Add("@VarID", SqlDbType.Int);
        cm.Parameters.Add("@Variable", SqlDbType.NVarChar, 100);
        cm.Parameters.Add("@Type", SqlDbType.NVarChar, 100);
        var cmVal = new SqlCommand($"insert into {Common.LayoutsTable}VariablesValues(ID, VarID, Value, Description) values(@ID, @VarID, @Value, @Description)", conn);
        cmVal.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        cmVal.Parameters.Add("@VarID", SqlDbType.Int);
        cmVal.Parameters.Add("@Value", SqlDbType.NVarChar, 100);
        cmVal.Parameters.Add("@Description", SqlDbType.NVarChar, 256);
        foreach (var param in parameters) {
            cm.Parameters["@VarID"].Value    = param.ID;
            cm.Parameters["@Variable"].Value = param.Name;
            cm.Parameters["@Type"].Value     = param.Type;
            cm.ExecuteNonQuery();
            if (param.Values.Count == 0)
                continue;
            cmVal.Parameters["@VarID"].Value = param.ID;
            foreach (var value in param.Values) {
                cmVal.Parameters["@Value"].Value       = value.Value;
                cmVal.Parameters["@Description"].Value = !string.IsNullOrWhiteSpace(value.Description) ? (object)value.Description : DBNull.Value;
                cmVal.ExecuteNonQuery();
            }
        }
    }

    public override void Dispose() {
        conn?.Dispose();
        GC.SuppressFinalize(this);
    }
}