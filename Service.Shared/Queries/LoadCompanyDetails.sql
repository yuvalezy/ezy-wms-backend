select top 1 T0."DecSep",
             T0."PriceDec",
             T0."QtyDec",
             T0."SumDec",
             T0."RateDec",
             T0."MeasureDec",
             T0."PercentDec",
             T0."ThousSep",
             T0."MainCurncy",
             T0."SysCurrncy",
             T0."DateFormat",
             T0."DateSep",
             T0."TimeFormat",
             T0."MultiLang",
             T0."PriceSys",
             Case When Left(T1."Version", 2) = '92' Then 0 When Left(T1."Version", 2) = '93' Then 1 When Left(T1."Version", 3) = '100' Then 2 Else 2 End "SBOVersion",
             Case When T1."Version" < 1000180 Then 1 Else 0 End                                                                                          "CrystalLegacy"
from OADM T0
         cross join CINF T1 