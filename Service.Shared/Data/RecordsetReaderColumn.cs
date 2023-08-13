using System;

namespace Service.Shared.Data; 

/// <summary>
/// Recordset Reader Column Attribute. See <see cref="SBO.RecordsetReader{T}" /> class
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RecordsetReaderColumn : ReaderColumn {
    /// <summary>
    /// Initialize Recordset Reader Column.
    /// </summary>
    /// <remarks>
    /// Reader will consider the object property where attribute is applied as the ID
    /// </remarks>
    public RecordsetReaderColumn() : base(null) {
    }

    /// <summary>
    /// Initialize Recordset Reader Column.
    /// </summary>
    /// <remarks>
    /// It could be that the column in the query is Code but the property is ItemCode, this way you can map between a query column the the reader object property
    /// </remarks>
    /// <param name="id">ID of the column to read</param>
    public RecordsetReaderColumn(string id) : base(id) {
    }

}