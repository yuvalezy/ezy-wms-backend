namespace Service.API.Models;

public class PickValueCheckData {
    public bool   HasData                  { get; set; }
    public string Status                   { get; set; }
    public bool   EnableBIN                { get; set; }
    public bool   CheckBIN                 { get; set; }
    public bool   CheckSerialBatchNumbers  { get; set; }
    public bool   EnableSerialBatchNumbers { get; set; }
    public string WhsCode                  { get; set; }
    public string ItemCode                 { get; set; }
    public int   SysNumber                { get; set; }
    public int    BaseObject               { get; set; }
    public int    BaseEntry                { get; set; }
    public int    BaseLine                 { get; set; }
}