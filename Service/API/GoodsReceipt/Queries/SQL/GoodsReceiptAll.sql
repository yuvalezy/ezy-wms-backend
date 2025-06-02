-- declare @ID int = 1037;
select T0."U_ItemCode"                 "ItemCode",
       T7."ItemName",
       Sum(T0."U_Quantity")            "Quantity",
       COALESCE(Sum(T9."Delivery"), 0) "Delivery",
       COALESCE(Sum(T9."Showroom"), 0) "Showroom",
       COALESCE(T8."OnHand", 0)        "OnHand",
       COALESCE(T7."NumInBuy", 1)      "NumInBuy",
       T7."BuyUnitMsr",
       COALESCE(T7."PurPackUn", 1)     "PurPackUn",
       T7."PurPackMsr"
from "@LW_YUVAL08_GRPO1" T0
         inner join "@LW_YUVAL08_GRPO" T1 on T1."Code" = T0."U_ID"
         inner join OITM T7 on T7."ItemCode" = T0."U_ItemCode"
         left outer join OITW T8 on T8."ItemCode" = T7."ItemCode" and T8."WhsCode" = T1."U_WhsCode"
         left outer join (select "U_LineID",
                                 Sum(Case When "U_TargetType" in (13, 17) Then "U_TargetQty" Else 0 End)  "Delivery",
                                 Sum(Case When "U_TargetType" = 1250000001 Then "U_TargetQty" Else 0 End) "Showroom"
                          from "@LW_YUVAL08_GRPO2"
                          where U_ID = @ID
                          Group By "U_LineID") T9 on T9.U_LineID = T0.U_LineID
where T0.U_ID = @ID
  and T0."U_LineStatus" <> 'C'
group by T7."ItemName", T0."U_ItemCode", T8."OnHand", T7."PurPackUn", T7."NumInBuy", T7."BuyUnitMsr", T7."NumInSale", T7."PurPackMsr"
order by "ItemCode"
