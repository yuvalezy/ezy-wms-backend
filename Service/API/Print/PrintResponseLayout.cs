namespace Service.API.Print; 

public class PrintResponseLayout {
    public int    ID   { get; }
    public string Name { get; }

    public PrintResponseLayout(int id, string name) {
        ID   = id;
        Name = name;
    }
}