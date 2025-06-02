-- declare @ID int = 1045;
-- declare @BaseType int = 22;
-- declare @BaseEntry int = 456;
-- declare @BaseLine int = 4;
-- declare @ID int = 1045;


select T1."U_Date"                          "TimeStamp",
       T2."firstName" + ' ' + T2."lastName" "EmployeeName",
       T0."U_Quantity"                      "Quantity",
       T1."U_Quantity"                      "ScannedQuantity",
       T1."U_Unit"                          "Unit"
from "@LW_YUVAL08_GRPO4" T0
         inner join "@LW_YUVAL08_GRPO1" T1 on T1.U_ID = T0.U_ID and T1."U_LineID" = T0."U_LineID"
         inner join OHEM T2 on T2."empID" = T1."U_EmpID"
where T0.U_ID = @ID
  and T0."U_SourceType" = @BaseType
  and T0."U_SourceEntry" = @BaseEntry
  and T0."U_SourceLine" = @BaseLine
  and T1."U_LineStatus" <> 'C'
order by 1
