select T0."U_PickEntry" "PickEntry", Sum(T0."U_Quantity") "Quantity", T0."U_Unit" "Unit", Cast(COALESCE(T1."NumInBuy", 1) as int) "NumInBuy"
from "@LW_YUVAL08_PKL1" T0
inner join OITM T1 on T1."ItemCode" = T0."U_ItemCode"
where T0."U_AbsEntry" = {0} 
and T0."U_Status" in ('O', 'P')
group by T0."U_PickEntry", T0."U_Unit", T1.NumInBuy
