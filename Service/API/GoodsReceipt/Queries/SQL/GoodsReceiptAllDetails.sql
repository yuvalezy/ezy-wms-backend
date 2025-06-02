-- declare @ID int = 1045;
-- declare @ItemCode nvarchar(50) = 'SCS';
-- 
select T0."U_LineID"                        "LineID"
     , T2."firstName" + ' ' + T2."lastName" "EmployeeName"
     , T0."U_Date"                          "TimeStamp"
     , T0."U_Quantity"                      "Quantity"
     , T0."U_Unit"                          "Unit"
from "@LW_YUVAL08_GRPO1" T0
         inner join "@LW_YUVAL08_GRPO" T1 on T1."Code" = T0."U_ID"
         inner join OITM T7 on T7."ItemCode" = T0."U_ItemCode"
         inner join OHEM T2 on T2."empID" = T0."U_empID"
where T0.U_ID = @ID
  and T0."U_ItemCode" = @ItemCode
  and T0."U_LineStatus" <> 'C'
