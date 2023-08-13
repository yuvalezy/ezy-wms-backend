namespace Service.Shared;

public class Node {
    public Node() {
        
    }
    public Node(int port) => Port = port;
    public int Port { get; set; }
}