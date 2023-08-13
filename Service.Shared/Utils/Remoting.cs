using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Service.Shared.Utils {
    /// <summary>
    /// This utility contains functions for tcp / udo communication functions
    /// </summary>
    public class Remoting {
        /// <summary>
        /// Function to scan available tcp / udp port
        /// </summary>
        /// <param name="startPort">Start Port to Scan From. Minimum value is 1.</param>
        /// <param name="endPort">End Port to Scan To. Maximum value is 65535</param>
        /// <example>
        ///   <para></para>
        ///   <code lang="C#"><![CDATA[int port = Remoting.GetPortNumberFromRange(500, 600);]]></code>
        ///   <para>Result will be the next available port number.</para>
        /// </example>
        public static int GetPortNumberFromRange(int startPort, int endPort) {
            var portArray = new List<int>();

            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Ignore active connections
            var connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                where n.LocalEndPoint.Port >= startPort && n.LocalEndPoint.Port <= endPort
                select n.LocalEndPoint.Port);

            // Ignore active tcp listeners
            var endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startPort && n.Port <= endPort
                select n.Port);

            // Ignore active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                where n.Port >= startPort && n.Port <= endPort
                select n.Port);

            portArray.Sort();

            for (int i = startPort; i < endPort; i++)
                if (!portArray.Contains(i))
                    return i;

            return 0;
        }

        /// <summary>
        /// Check if a port is currently available on local machine
        /// </summary>
        /// <param name="port">Port number</param>
        public static bool IsPortAvailable(int port) {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray   = ipGlobalProperties.GetActiveTcpListeners();
            return tcpConnInfoArray.All(endpoint => endpoint.Port != port);
        }
    }
}