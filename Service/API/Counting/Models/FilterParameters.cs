using System;
using Core.Enums;
using Service.API.Models;

namespace Service.API.Counting.Models;

public class FilterParameters {
    internal string WhsCode { get; set; }

    public DateTime?      Date    { get; set; }
    public string         Name    { get; set; }
    public ObjectStatus[] Status  { get; set; }
    public OrderBy?       OrderBy { get; set; }
    public int?           ID      { get; set; }
    public bool           Desc    { get; set; }
}