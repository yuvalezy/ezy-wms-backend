using System.Data;

namespace Service.Administration.Views;

public interface IMain {
    string   Database   { get; set; }
    string   Text       { get; set; }
    string   ServerName { get; set; }
    int      CurrentRow { get; set; }
    void     MinimizeForm();
    bool     IsActive   { get; }
    bool     ActiveOnly { get; set; }
    DataView Source     { set; }
    void     ClearSelection();
    void     SetIsActive(bool value);
}