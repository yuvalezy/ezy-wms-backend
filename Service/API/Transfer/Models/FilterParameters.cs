using System;
using Service.API.Models;

namespace Service.API.Transfer.Models;

public class FilterParameters {
    internal string WhsCode { get; set; }

    public DateTime?        Date     { get; set; }
    public DocumentStatus[] Status   { get; set; }
    public OrderBy?         OrderBy  { get; set; }
    public int?             ID       { get; set; }
    public int?             Number   { get; set; }
    public bool             Desc     { get; set; }
    public bool             Progress { get; set; }
}