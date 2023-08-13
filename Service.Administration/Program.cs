using System;
using System.Threading;
using System.Windows.Forms;
using Service.Shared.Data;

namespace Service.Administration; 

internal static class Program {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args) {
        if (IsRunning()) {
            Application.Exit();
            return;
        }

        SBOAssembly.RedirectAssembly();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Run(args);
    }

    private static Mutex mutex;

    private static bool IsRunning() {
        string mutexName = $"LW_ADM_{Environment.UserName}".Replace(".", "");
        mutex = new Mutex(true, mutexName, out bool createdNew);
        if (createdNew)
            return false;
        MessageBox.Show("Light WMS Service Administration application is already running!\nExiting the application.", "Light WMS Service Administration", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        return true;
    }

    private static void Run(string[] args) => Application.Run(new Main());
}