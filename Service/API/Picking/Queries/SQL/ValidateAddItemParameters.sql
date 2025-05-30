-- DECLARE @ID INT = 29,
--     @SourceType INT = 17,
--     @SourceEntry INT = 542,
--     @ItemCode NVARCHAR(50) = 'BOX',
--     @empID INT = 1,
--     @Quantity INT = 2,
--     @BinEntry INT = 3413,
--     @UNIT smallint = 2;

DECLARE @WhsCode NVARCHAR(8) = (SELECT U_LW_Branch
                                FROM OHEM
                                WHERE empID = @empID);

If @Unit > 0
    Begin
        select @Quantity = @Quantity * COALESCE("NumInBuy", 1) * Case When @Unit = 2 Then COALESCE("PurPackUn", 1) Else 1 End
        from OITM
        where "ItemCode" = @ItemCode;
    end

drop table if exists #tmp_validate_add_item_parameters;
-- select * from PKL1 where AbsEntry = @ID;
-- return
SELECT TOP 1 
       T0.PickEntry,
       CASE
           WHEN @Quantity > T0.RelQtty - COALESCE(T5.Quantity, 0) THEN -7
           WHEN @Quantity > COALESCE(T6."OnHandQty", 0) - COALESCE(T7."Quantity", 0) THEN -13
           ELSE 0
           END AS Result
--         ,
--              T0.RelQtty,
--              COALESCE(T5.Quantity, 0),
--              COALESCE(T6."OnHandQty", 0),
--              COALESCE(T7."Quantity", 0)
FROM PKL1 T0
         INNER JOIN OILM T1 ON T1.TransType = T0.BaseObject AND T1.DocEntry = T0.OrderEntry AND T1.DocLineNum = T0.OrderLine AND T1.ItemCode = @ItemCode AND T1.LocCode = @WhsCode
         LEFT OUTER JOIN (SELECT U_PickEntry AS PickEntry, SUM(U_Quantity) AS Quantity
                          FROM "@LW_YUVAL08_PKL1"
                          WHERE U_AbsEntry = @ID
                            AND U_Status IN ('O', 'P')
                          GROUP BY U_PickEntry) T5 ON T5.PickEntry = T0.PickEntry
         left outer join OIBQ T6 on T6."ItemCode" = @ItemCode and T6."BinAbs" = @BinEntry
         cross join (select Sum("Quantity") "Quantity"
                     from (select X0."U_Quantity" "Quantity"
                           from "@LW_YUVAL08_PKL1" X0
                           where X0."U_ItemCode" = @ItemCode
                             and X0."U_BinEntry" = @BinEntry
                             and X0."U_Status" in ('O', 'P')
                           union all
                           select "U_Quantity"
                           from "@LW_YUVAL08_TRANS1" X0
                           where X0."U_ItemCode" = @ItemCode
                             and X0."U_BinEntry" = @BinEntry
                             and X0."U_LineStatus" in ('O', 'I')) CommitedData) T7
WHERE T0.AbsEntry = @ID
  AND T0.BaseObject = @SourceType
  AND T0.OrderEntry = @SourceEntry
order by 2 desc