insert into [@{0}S](Code, Name, U_ID, U_LineID, U_ItemCode, U_CardCode, U_ShipToCode) 
select IsNull((select Max(Cast(Code as bigint))+1 from [@{0}S]), 0), 
  IsNull((select Max(Cast(Code as bigint))+1 from [@{0}S]), 0), {1}, {2}, {3}, {4}, {5}