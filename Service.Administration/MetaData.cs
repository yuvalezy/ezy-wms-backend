using Service.Shared.Data;
using Service.Shared.Utils;

namespace Service.Administration; 

public class MetaData {
    private readonly string        dbName;
    private readonly DataConnector data;

    public MetaData(DataConnector data) => this.data = data;


    public void Check() {
        //todo verify common data
        string user     = "manager".EncryptData();
        string password = "be1s".EncryptData();
        string sqlStr = $"""
                        insert into [@LW_YUVAL08_COMMON](Code, Name, U_Version, U_User, U_Password)
                        select '1', '1', '1.0.0', '{user.ToQuery()}', '{password.ToQuery()}'
                        where not exists(select 1 from [@LW_YUVAL08_COMMON])
                        """;
        data.Execute(sqlStr);
        
        //create roles
        // select "typeID", "name" from OHTY where "name" in ({Const.GoodsReceipt}', '{Const.GoodsReceiptSupervisor})
    }
}