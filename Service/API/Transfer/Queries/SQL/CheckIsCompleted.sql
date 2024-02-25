-- declare @ID int = 2;
select top 1 1
from "@LW_YUVAL08_TRANS1" T0
where T0.U_ID = @ID and T0."U_LineStatus" <> 'C'
group by T0."U_ItemCode", T0."U_Type"
Having Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) <> Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0))
