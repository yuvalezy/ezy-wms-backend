namespace Service.Shared; 

public class PrintObjects {
    public static readonly PrintObject[] ObjectsList = {
        new(1, "Hello World!")
    };
}

public class PrintObject {
    public int    ID   { get; set; }
    public string Name { get; set; }

    public PrintObject(int id, string name) {
        ID   = id;
        Name = name;
    }
}

public enum PrintObjectType {
    HELLO_WORLD = 1,
}