using System;
using System.Collections.Generic;
using Service.Crystal.Shared;

namespace Service.Shared.PrintLayout; 

public abstract class LayoutFile : IDisposable {
    public abstract byte[]                    Get(int            id);
    public abstract int                       Set(int            @object, byte[] content, string fileName, string md5, CrystalLayoutParameters parameters);
    public abstract List<PrintLayoutVariable> GetVariables(int   id);
    public abstract T                         GetValue<T>(string query);
    public abstract List<LayoutData>          GetLayoutsData(int type, string filter, int? specificID, SpecificFilter specificFilter);
    public abstract void                      Execute(string     query);
    public abstract void                      Dispose();

    protected static T ReadValue<T>(object readData) {
        if (readData is T data)
            return data;

        if (readData is null || readData == DBNull.Value)
            return default;

        if (typeof(T) == typeof(bool))
            return (T)Convert.ChangeType(readData.ToString() is "Y" or "1" or "true", typeof(T));

        try {
            return (T)Convert.ChangeType(readData, typeof(T));
        }
        catch (InvalidCastException) {
            return default;
        }
    }
}