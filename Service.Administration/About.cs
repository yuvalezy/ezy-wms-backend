using System.Reflection;
using System.Windows.Forms;

namespace Service.Administration; 

internal partial class About : Form {
    public About() {
        InitializeComponent();
        Text                    = $"About {AssemblyTitle}";
        labelProductName.Text   = AssemblyProduct;
        labelVersion.Text       = $"Version {AssemblyVersion}";
        labelCopyright.Text     = AssemblyCopyright;
        labelCompanyName.Text   = AssemblyCompany;
        textBoxDescription.Text = AssemblyDescription;
    }

    #region Assembly Attribute Accessors

    private static string AssemblyTitle {
        get {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length <= 0) 
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            var titleAttribute = (AssemblyTitleAttribute)attributes[0];
            return titleAttribute.Title != "" ? titleAttribute.Title : System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
        }
    }

    private static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    private static string AssemblyDescription {
        get {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }
    }

    private static string AssemblyProduct {
        get {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    private static string AssemblyCopyright {
        get {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    private static string AssemblyCompany {
        get {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }

    #endregion

    private void labelCompanyName_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { System.Diagnostics.Process.Start("https://beonesolutions.com/"); }
}