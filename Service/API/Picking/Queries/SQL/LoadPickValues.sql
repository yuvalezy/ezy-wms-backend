select "U_PickEntry" "PickEntry", Sum("U_Quantity") "Quantity"
from "@LW_YUVAL08_PKL1"
where "U_AbsEntry" = {0} and "U_Status" in ('O', 'P')
group by "U_PickEntry"
