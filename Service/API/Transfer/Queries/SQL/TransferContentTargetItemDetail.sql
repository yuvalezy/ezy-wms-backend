-- declare @ID int = 2;
-- declare @ItemCode nvarchar(50) = '0000002';
-- declare @BinEntry int = 3430;
-- 
select T0."U_LineID"                        "LineID"
     , T2."firstName" + ' ' + T2."lastName" "EmployeeName"
     , T0."U_Date"                          "TimeStamp"
     , T0."U_Quantity"                      "Quantity"
from "@LW_YUVAL08_TRANS1" T0
         inner join "@LW_YUVAL08_TRANS" T1 on T1."Code" = T0."U_ID"
         inner join OITM T7 on T7."ItemCode" = T0."U_ItemCode"
         inner join OHEM T2 on T2."empID" = T0."U_empID"
where T0.U_ID = @ID
  and T0."U_ItemCode" = @ItemCode
  and T0."U_BinEntry" = @BinEntry
  and T0."U_Type" = 'T'
  and T0."U_LineStatus" <> 'C'
