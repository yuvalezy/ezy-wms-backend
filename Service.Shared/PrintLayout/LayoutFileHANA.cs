using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Service.Shared.PrintLayout.Models;
using Sap.Data.Hana;
using Service.Crystal.Shared;
using Service.Shared.Company;

namespace Service.Shared.PrintLayout; 

public class LayoutFileHANA : LayoutFile {
    private HanaConnection conn;

    private static readonly Mutex ConnectionMutex = new(false, "lmserconn");

    protected virtual void OpenConnection() {
        ConnectionMutex.WaitOne();
        conn ??= new HanaConnection(ConnectionController.ConnectionString);
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
            using var hanaCm = new HanaCommand($"select \"Package\" from \"{Common.LayoutsTable}\" where ID = :ID", conn);
            hanaCm.Parameters.Add("ID", HanaDbType.Integer).Value = id;
            byte[] content = (byte[])hanaCm.ExecuteScalar();
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
            using var cm = new HanaCommand(string.Format(Queries.PrintLayoutVariables, id, Common.LayoutManagerUDT, Common.LayoutsTable), conn);
            using var da = new HanaDataAdapter(cm);
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
            using var cm = new HanaCommand(query, conn);
            return ReadValue<T>(cm.ExecuteScalar());
        }
        finally {
            CloseConnection();
        }
    }

    public override List<LayoutData> GetLayoutsData(int type, string filter, int? specificID, SpecificFilter specificFilter) {
        OpenConnection();
        try {
            using var da = new HanaDataAdapter(Common.GetLayoutsStoredProcedureName, conn);
            var       cm = da.SelectCommand;
            cm.CommandType = CommandType.StoredProcedure;

            cm.Parameters.Add("Entry", HanaDbType.Integer).Value        = type;
            cm.Parameters.Add("Filter", HanaDbType.NVarChar, 254).Value = filter;
            if (specificID.HasValue)
                cm.Parameters.Add("ID", HanaDbType.Integer).Value = specificID.Value;
            if (specificFilter != null) {
                if (!string.IsNullOrWhiteSpace(specificFilter.ItemCode))
                    cm.Parameters.Add("ItemCode", HanaDbType.NVarChar, 50).Value = specificFilter.ItemCode;
                if (!string.IsNullOrWhiteSpace(specificFilter.CardCode))
                    cm.Parameters.Add("CardCode", HanaDbType.NVarChar, 50).Value = specificFilter.CardCode;
                if (!string.IsNullOrWhiteSpace(specificFilter.ShipToCode))
                    cm.Parameters.Add("ShipToCode", HanaDbType.NVarChar, 50).Value = specificFilter.ShipToCode;
                if (!string.IsNullOrWhiteSpace(specificFilter.CardCode2))
                    cm.Parameters.Add("CardCode2", HanaDbType.NVarChar, 50).Value = specificFilter.CardCode2;
                if (!string.IsNullOrWhiteSpace(specificFilter.ShipToCode2))
                    cm.Parameters.Add("ShipToCode2", HanaDbType.NVarChar, 50).Value = specificFilter.ShipToCode2;
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
            using var cm = new HanaCommand(query, conn);
            cm.ExecuteNonQuery();
        }
        finally {
            CloseConnection();
        }
    }


    private int AddLayout(int type, byte[] content, string fileName, string md5, string mainVariable) {
        var cm = new HanaCommand($"select ID from \"{Common.LayoutsTable}\" where \"Type\" = :Type and \"FileName\" = :FileName", conn);
        cm.Parameters.Add("Type", HanaDbType.Integer).Value           = type;
        cm.Parameters.Add("FileName", HanaDbType.NVarChar, 100).Value = fileName;
        using var dr    = cm.ExecuteReader();
        bool      check = false;
        int       id    = -1;
        if (dr.Read()) {
            id    = dr.GetInt32(0);
            check = true;
        }

        dr.Close();
        cm.CommandText = !check
            ? $@"insert into ""{Common.LayoutsTable}""(ID, ""FileName"", ""Package"", ""Variable"", ""MD5"", ""Type"") 
select IFNULL((select Max(ID)+1 from ""{Common.LayoutsTable}""), 0) ID, :FileName, :Package, :Variable, :MD5, :Type
FROM DUMMY"
            : $"update \"{Common.LayoutsTable}\" set \"Package\" = :Package, \"Variable\" = :Variable, MD5 = :MD5 where \"Type\" = :Type and \"FileName\" = :FileName";
        cm.Parameters.Add("Package", HanaDbType.Blob).Value           = content;
        cm.Parameters.Add("Variable", HanaDbType.NVarChar, 100).Value = mainVariable;
        cm.Parameters.Add("MD5", HanaDbType.NVarChar, 32).Value       = md5;
        cm.ExecuteNonQuery();
        cm.Dispose();
        if (!check) {
            cm = new HanaCommand($"select Max(ID) from \"{Common.LayoutsTable}\"", conn);
            id = (int)cm.ExecuteScalar();
        }
        else {
            cm = new HanaCommand($"delete from \"{Common.LayoutsTable}Variables\" where ID = :ID", conn);

            cm.Parameters.Add("ID", HanaDbType.Integer).Value = id;
            cm.ExecuteNonQuery();
            cm.CommandText = $"delete from \"{Common.LayoutsTable}VariablesValues\" where ID = :ID";
            cm.ExecuteNonQuery();
        }

        cm.Dispose();

        return id;
    }

    private void AddParameters(int id, CrystalLayoutParameters parameters) {
        using var cmVar = new HanaCommand($"insert into \"{Common.LayoutsTable}Variables\"(ID, \"VarID\", \"Variable\", \"Type\") values(:ID, :VarID, :Variable, :Type)", conn);
        cmVar.Parameters.Add("ID", HanaDbType.Integer).Value = id;
        cmVar.Parameters.Add("VarID", HanaDbType.Integer);
        cmVar.Parameters.Add("Variable", HanaDbType.NVarChar, 100);
        cmVar.Parameters.Add("Type", HanaDbType.NVarChar, 100);
        var cmVal = new HanaCommand($"insert into \"{Common.LayoutsTable}VariablesValues\"(ID, \"VarID\", \"Value\", \"Description\") values(:ID, :VarID, :Value, :Description)", conn);
        cmVal.Parameters.Add("ID", HanaDbType.Integer).Value = id;
        cmVal.Parameters.Add("VarID", HanaDbType.Integer);
        cmVal.Parameters.Add("Value", HanaDbType.NVarChar, 100);
        cmVal.Parameters.Add("Description", HanaDbType.NVarChar, 256);
        foreach (var param in parameters) {
            cmVar.Parameters["VarID"].Value    = param.ID;
            cmVar.Parameters["Variable"].Value = param.Name;
            cmVar.Parameters["Type"].Value     = param.Type;
            cmVar.ExecuteNonQuery();
            if (param.Values.Count <= 0) continue;
            cmVal.Parameters["VarID"].Value = param.ID;
            foreach (var value in param.Values) {
                cmVal.Parameters["Value"].Value       = value.Value;
                cmVal.Parameters["Description"].Value = !string.IsNullOrWhiteSpace(value.Description) ? (object)value.Description : DBNull.Value;
                cmVal.ExecuteNonQuery();
            }
        }
    }

    public override void Dispose() {
        conn?.Dispose();
        GC.SuppressFinalize(this);
    }
}