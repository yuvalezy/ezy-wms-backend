-- declare @ID int = 3011;
select COALESCE((select top 1 1
                 from "@LW_YUVAL08_TRANS1" T0
                 where T0.U_ID = @ID
                   and T0."U_LineStatus" <> 'C'
                 group by T0."U_ItemCode"
                 Having Sum(IIF(T0."U_Type" = 'S', T0."U_Quantity", 0)) <> Sum(IIF(T0."U_Type" = 'T', T0."U_Quantity", 0))), 0) "IsComplete",
       case
           when exists(select 1 from "@LW_YUVAL08_TRANS1" T0 where T0.U_ID = @ID and T0."U_LineStatus" <> 'C') Then 1
           Else 0 End                                                                                                           "HasItems"
