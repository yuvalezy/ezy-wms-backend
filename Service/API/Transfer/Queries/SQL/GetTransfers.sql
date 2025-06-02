select DISTINCT TRANSFERS."Code"                                                                    ID,
                TRANSFERS."Name",
                TRANSFERS."U_Date"                                                                  "Date",
                TRANSFERS."U_empID"                                                                 "EmployeeID",
                COALESCE(T1."firstName", 'NO_NAME') + ' ' + COALESCE(T1."lastName", 'NO_LAST_NAME') "EmployeeName",
                TRANSFERS."U_Status"                                                                "Status",
                TRANSFERS."U_StatusDate"                                                            "StatusDate",
                TRANSFERS."U_StatusEmpID"                                                           "StatusEmployeeID",
                COALESCE(T2."firstName", 'NO_NAME') + ' ' + COALESCE(T2."lastName", 'NO_LAST_NAME') "StatusEmployeeName",
                TRANSFERS."U_WhsCode"                                                               "WhsCode",
                Cast(TRANSFERS."U_Comments" as varchar(8000))                                       "Comments"
--{0}
from "@LW_YUVAL08_TRANS" TRANSFERS
         inner join OHEM T1 on T1."empID" = TRANSFERS."U_empID"
         inner join OHEM T2 on T2."empID" = TRANSFERS."U_StatusEmpID"
