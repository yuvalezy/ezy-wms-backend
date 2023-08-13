select case when exists(select ''
	from [@{1}] T0
	inner join {2} T1 on T1.ID = T0.U_FileID
	where T0.U_Type = {0} and T0.U_Active = 'Y'
) Then 1 Else 0 End