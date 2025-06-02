using System;
using Service.API.Models;

namespace Service.API.Counting.Models;

public class Counting {
    public int            ID              { get; set; }
    public string         Name            { get; set; }
    public DateTime       Date            { get; set; }
    public UserInfo       Employee        { get; set; }
    public DocumentStatus Status          { get; set; }
    public DateTime       StatusDate      { get; set; }
    public UserInfo       StatusEmployee  { get; set; }
    public string         WhsCode         { get; set; }
    public bool           Error           { get; set; }
    public int            ErrorCode       { get; set; }
    public object[]       ErrorParameters { get; set; }
}

