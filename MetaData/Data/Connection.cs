using SAPbobsCOM;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace MetaData;

public class ConnectionProperties {
    public string            ServerName        { get; set; }
    public string            DatabaseName      { get; set; }
    public string            DbUsername        { get; set; }
    public string            DbPassword        { get; set; }
    public string            SapUsername       { get; set; }
    public string            SapPassword       { get; set; }
    public BoDataServerTypes DbServerType      { get; set; }
    public bool              TrustedConnection { get; set; }
    public string            ConnectionId      => $"{ServerName}_{DatabaseName}";
}

public class Connection {
    private const string ConnectionsFilePath = "connections.json";

    private Company                                  company = new();
    private Dictionary<string, ConnectionProperties> savedConnections;

    public Connection() {
        LoadSavedConnections();
    }

    private void LoadSavedConnections() {
        if (File.Exists(ConnectionsFilePath)) {
            try {
                string json = File.ReadAllText(ConnectionsFilePath);
                savedConnections = JsonSerializer.Deserialize<Dictionary<string, ConnectionProperties>>(json);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error loading saved connections: {ex.Message}");
                savedConnections = new Dictionary<string, ConnectionProperties>();
            }
        }
        else {
            savedConnections = new Dictionary<string, ConnectionProperties>();
        }
    }

    private void SaveConnections() {
        try {
            string json = JsonSerializer.Serialize(savedConnections, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConnectionsFilePath, json);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error saving connections: {ex.Message}");
        }
    }

    public bool Initialize() {
        // Check if we have saved connections
        if (savedConnections.Count > 0) {
            Console.WriteLine("Saved connections found:");
            int index         = 1;
            var connectionIds = new List<string>();

            foreach (var connection in savedConnections) {
                Console.WriteLine($"{index}. {connection.Value.ServerName} - {connection.Value.DatabaseName}");
                connectionIds.Add(connection.Key);
                index++;
            }

            Console.WriteLine($"{index}. Create new connection");
            Console.Write("Select an option: ");

            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0) {
                if (selection <= connectionIds.Count) {
                    string selectedId = connectionIds[selection - 1];
                    var    props      = savedConnections[selectedId];

                    // Try to connect with saved properties
                    if (ConnectToCompany(props)) {
                        Console.WriteLine("Connected successfully using saved connection.");
                        return true;
                    }

                    Console.WriteLine("Failed to connect using saved connection. Please enter new details.");
                }
            }
        }

        // If no saved connections or user chose to create a new one
        var newProps = PromptForConnectionProperties();
        if (ConnectToCompany(newProps)) {
            // Save the successful connection
            savedConnections[newProps.ConnectionId] = newProps;
            SaveConnections();
            Console.WriteLine("Connection successful and saved for future use.");
            return true;
        }

        Console.WriteLine("Failed to connect to the company.");
        return false;
    }

    private ConnectionProperties PromptForConnectionProperties() {
        var props = new ConnectionProperties();

        Console.Write("Enter Server Name: ");
        props.ServerName = Console.ReadLine();

        Console.Write("Enter Database Name: ");
        props.DatabaseName = Console.ReadLine();

        Console.Write("Use trusted connection (Y)es, (N)o: ");
        props.TrustedConnection = Console.ReadLine().Equals("Y");

        if (!props.TrustedConnection) {
            Console.Write("Enter Server Username: ");
            props.DbUsername = Console.ReadLine();

            Console.Write("Enter Server Password: ");
            props.DbPassword = Console.ReadLine();
        }

        Console.Write("Enter SAP Username: ");
        props.SapUsername = Console.ReadLine();

        Console.Write("Enter SAP Password: ");
        props.SapPassword = Console.ReadLine();

        Console.WriteLine("Select Database Server Type:");
        Console.WriteLine("1. MSSQL");
        Console.WriteLine("2. DB_2");
        Console.WriteLine("3. SYBASE");
        Console.WriteLine("4. MSSQL2005");
        Console.WriteLine("5. MAXDB");
        Console.WriteLine("6. MSSQL2008");
        Console.WriteLine("7. MSSQL2012");
        Console.WriteLine("8. MSSQL2014");
        Console.WriteLine("9. HANADB");
        Console.WriteLine("10. MSSQL2016");
        Console.WriteLine("11. MSSQL2017");
        Console.WriteLine("12. MSSQL2019");
        Console.Write("Enter selection (1-12): ");

        if (int.TryParse(Console.ReadLine(), out int dbTypeSelection)) {
            switch (dbTypeSelection) {
                case 1:  props.DbServerType = BoDataServerTypes.dst_MSSQL; break;
                case 2:  props.DbServerType = BoDataServerTypes.dst_DB_2; break;
                case 3:  props.DbServerType = BoDataServerTypes.dst_SYBASE; break;
                case 4:  props.DbServerType = BoDataServerTypes.dst_MSSQL2005; break;
                case 5:  props.DbServerType = BoDataServerTypes.dst_MAXDB; break;
                case 6:  props.DbServerType = BoDataServerTypes.dst_MSSQL2008; break;
                case 7:  props.DbServerType = BoDataServerTypes.dst_MSSQL2012; break;
                case 8:  props.DbServerType = BoDataServerTypes.dst_MSSQL2014; break;
                case 9:  props.DbServerType = BoDataServerTypes.dst_HANADB; break;
                case 10: props.DbServerType = BoDataServerTypes.dst_MSSQL2016; break;
                case 11: props.DbServerType = BoDataServerTypes.dst_MSSQL2017; break;
                case 12: props.DbServerType = BoDataServerTypes.dst_MSSQL2019; break;
                default: props.DbServerType = BoDataServerTypes.dst_MSSQL2014; break;
            }
        }

        return props;
    }

    private bool ConnectToCompany(ConnectionProperties props) {
        try {
            company.Server    = props.ServerName;
            company.CompanyDB = props.DatabaseName;
            if (props.TrustedConnection) {
                company.UseTrusted = true;
            }
            else {
                company.DbUserName = props.DbUsername;
                company.DbPassword = props.DbPassword;
            }

            company.UserName     = props.SapUsername;
            company.Password     = props.SapPassword;
            company.DbServerType = props.DbServerType;

            if (company.Connect() == 0) {
                return true;
            }

            int    errorCode    = company.GetLastErrorCode();
            string errorMessage = company.GetLastErrorDescription();
            Console.WriteLine($"Connection failed. Error code: {errorCode}, Message: {errorMessage}");
            return false;
        }
        catch (Exception ex) {
            Console.WriteLine($"Exception during connection: {ex.Message}");
            return false;
        }
    }

    public Company GetCompany() {
        return company;
    }
}