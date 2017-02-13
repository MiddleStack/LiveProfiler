using System;
using System.Runtime.Serialization;

namespace MiddleStack.Profiling.Streaming
{
    /// <summary>
    ///     Configures the behaviors of <see cref="ProfilerEventStreamer"/>
    /// </summary>
    [DataContract]
    public class StreamingConfiguration
    {
        /// <summary>
        ///     Gets or sets the URL of the server that events are streamed to.
        ///     If unspecified, a default of "http://localhost:7700" is used.
        /// </summary>
        /// <value>
        ///     A <see cref="Uri"/> object providing the overriding server URL.
        ///     <see langword="null"/> if the default value is to be used.
        /// </value>
        [DataMember(Name = "serverUrl")]
        public Uri ServerUrl { get; set; }

        /// <summary>
        ///     Gets or sets whether streaming is enabled for this <see cref="ProfilerEventStreamer"/>.
        ///     If unspecified, the default value is <see langword="true"/>.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> or <see langword="null"/> if streaming is enabled. <see langword="false"/>
        ///     if streaming is explicitly disabled.
        /// </value>
        [DataMember(Name = "enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        ///     Gets or sets the name of the current application the events are streamed from.
        ///     If unspecified, the name of the current process is used, which often isn't specific enough,
        ///     particularly for web apps.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of the current application. 
        ///     <see langword="null"/> if the process name is used as the application name.
        /// </value>
        public string AppName { get; set; }

        /// <summary>
        ///     Gets or sets the name of the current host.
        ///     If unspecified, <see cref="Environment.MachineName"/> is used.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of the current host.
        ///     <see langword="null"/> if <see cref="Environment.MachineName"/> is used.
        /// </value>
        public string HostName { get; set; }
    }
}
