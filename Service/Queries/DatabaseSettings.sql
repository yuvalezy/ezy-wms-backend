select T0."U_Version"                                                           "Version",
       T0."U_User"                                                              "User",
       T0."U_Password"                                                          "Password",
       'N'                                                                      "TestHelloWorld",
       T0.U_DEBUG                                                               DEBUG,
       T0."U_PrintThread"                                                       "PrintThread",
       Case When (select top 1 "Version" from CINF) < 1000180 Then 1 Else 0 End "CrystalLegacy",
       T0."U_GRPODraft"                                                         "GRPODraft",
       T0."U_GRPOModSup"                                                        "GRPOModSup",
       T0."U_GRPOCreateSup"                                                     "GRPOCreateSup",
       T0."U_TransferTargetItems"                                               "TransferTargetItems",
       T1."CompnyName"                                                          "CompanyName"
from "@LW_YUVAL08_COMMON" T0
         cross join OADM T1
