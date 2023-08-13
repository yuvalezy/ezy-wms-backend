select T2."Variable", T1."U_Value" "Value"
from "@{1}" T0
inner join "@{1}V" T1 on T1.U_ID = T0."Code"
inner join "{2}Variables" T2 on T2.ID = T0."U_FileID" and T2."VarID" = T1."U_VarID"
where T0."Code" = {0} 