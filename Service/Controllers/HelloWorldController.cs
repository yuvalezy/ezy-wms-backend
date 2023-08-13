using System;
using Console = System.Console;

namespace Service.Controllers;

public class HelloWorldController : BaseBackgroundController {
    public override void Elapsed() {
        Execute();
    }

    public void Execute() {
        try {
            Console.WriteLine("Hello World Controller has executed...");
        }
        catch (Exception ex) {
            Global.LogError("Error executing Clear Expired Bin Locks: " + ex.Message);
        }
    }
}