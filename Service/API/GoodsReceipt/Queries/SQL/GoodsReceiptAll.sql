select T0."U_ItemCode"                                                 "ItemCode",
       T7."ItemName",
       Count(1)                                                        "Quantity",
       Sum(Case When T0."U_TargetType" in (13, 17) Then 1 Else 0 End)  "Delivery",
       Sum(Case When T0."U_TargetType" = 1250000001 Then 1 Else 0 End) "Showroom",
       COALESCE(T8."OnHand", 0)                                        "OnHand"
from "@LW_YUVAL08_GRPO1" T0
         inner join "@LW_YUVAL08_GRPO" T1 on T1."Code" = T0."U_ID"
         inner join OITM T7 on T7."ItemCode" = T0."U_ItemCode"
         left outer join OITW T8 on T8."ItemCode" = T7."ItemCode" and T8."WhsCode" = T1."U_WhsCode"
where T0.U_ID = @ID
group by T7."ItemName", T0."U_ItemCode", T0."U_TargetType", T8."OnHand"
order by "ItemCode"
