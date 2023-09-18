-- declare @ID int = 1;
select "U_ItemCode" "ItemCode", Count(1) "Quantity", "U_POEntry" "BaseEntry", "U_POLine" "BaseLine"
from "@LW_YUVAL08_GRPO1"
where U_ID = @ID
group by Case U_POEntry When -1 Then 2 Else 0 End, U_POEntry, U_POLine, "U_ItemCode"