using System;
using Service.API.Models;

namespace Service.API.GoodsReceipt.Models;

public class Document {
    public int             ID              { get; set; }
    public string          Name            { get; set; }
    public DateTime        Date            { get; set; }
    public UserInfo        Employee        { get; set; }
    public DocumentStatus  Status          { get; set; }
    public DateTime        StatusDate      { get; set; }
    public UserInfo        StatusEmployee  { get; set; }
    public BusinessPartner BusinessPartner { get; set; }
}