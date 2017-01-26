using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace MiddleStack.Testing
{
    /// <summary>
    ///		A collection of utility methods for IP networking in tests.
    /// </summary>
    public static class IPTestingUtilities
    {
        private const string PortReleaseGuid = "c92660156d4844afa097af53e28a2601";
        private static int _lastPortFound = 0;

        /// <summary>
        /// 	<para>Finds the first available TCP port, starting from port 1024,
        ///     on the local machine for setting up servers in unit tests.</para>
        /// </summary>
        /// <returns>
        /// 	<para>The first available TCP port number.</para>
        /// </returns>
        /// <exception cref="ApplicationException">
        /// 	<para>No available port could be found.</para>
        /// </exception>
        public static int FindAvailableTcpPort()
        {
            using (var mutex = new Mutex(false, PortReleaseGuid))
            {
                mutex.WaitOne();

                try
                {
                    var port = EnumerateCandidatePorts().First(IsLocalTcpPortFree);

                    _lastPortFound = port;

                    return port;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private const int StartingPort = 1024;
        private static readonly int _examinedPortCount = IPEndPoint.MaxPort - StartingPort + 1;

        private static IEnumerable<int> EnumerateCandidatePorts()
        {
            int port = _lastPortFound == 0 ? 1024 : _lastPortFound + 1;

            for (var i = 0; i < _examinedPortCount; i++)
            {
                port++;

                if (port > IPEndPoint.MaxPort)
                {
                    port = StartingPort;
                }

                yield return port;
            }
        }

        /// <summary>
        ///     Determines whether the specified local TCP port is free.
        /// </summary>
        /// <param name="port">
        ///     The port to examine.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the specified TCP port is free;
        ///     <see langword="false"/> if the port is being used.
        /// </returns>
        public static bool IsLocalTcpPortFree(int port)
        {
            var globalProperties = IPGlobalProperties.GetIPGlobalProperties();

            return !globalProperties.GetActiveTcpListeners()
                .Concat(globalProperties.GetActiveTcpConnections()
                    .Select(c => c.LocalEndPoint))
                .Select(e => e.Port).Contains(port);
        }
    }
}
