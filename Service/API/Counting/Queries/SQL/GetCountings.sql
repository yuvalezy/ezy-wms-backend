select DISTINCT COUNTS."Code"                                                                         ID,
                COUNTS."Name",
                COUNTS."U_Date"                                                                       "Date",
                COUNTS."U_empID"                                                                      "EmployeeID",
                COALESCE(T1."firstName", 'NO_NAME') + ' ' + COALESCE(T1."lastName", 'NO_LAST_NAME') "EmployeeName",
                COUNTS."U_Status"                                                                     "Status",
                COUNTS."U_StatusDate"                                                                 "StatusDate",
                COUNTS."U_StatusEmpID"                                                                "StatusEmployeeID",
                COALESCE(T2."firstName", 'NO_NAME') + ' ' + COALESCE(T2."lastName", 'NO_LAST_NAME') "StatusEmployeeName",
                COUNTS."U_WhsCode"                                                                    "WhsCode"
from [@LW_YUVAL08_OINC] COUNTS
         inner join OHEM T1 on T1."empID" = COUNTS."U_empID"
         inner join OHEM T2 on T2."empID" = COUNTS."U_StatusEmpID"