namespace devanewbot.Services.Ham;

using System.Collections.Generic;

public class HamAlertOptions
{
    public string ChannelId { get; set; } = null!;
    public string Callsign { get; set; } = "W3DEV";
    public int IdleTimeoutMinutes { get; set; } = 60;
    public double LocationChangeKm { get; set; } = 50;
    public int UpdateIntervalSeconds { get; set; } = 60;
    public int SpotRetentionDays { get; set; } = 30;
    public string PskReporterHost { get; set; } = "mqtt.pskreporter.info";
    public int PskReporterPort { get; set; } = 1883;
    public List<TelnetNode> TelnetNodes { get; set; } = [];
    public HamQthOptions HamQth { get; set; } = new();

    public class TelnetNode
    {
        public string Name { get; set; } = null!;
        public string Host { get; set; } = null!;
        public int Port { get; set; }
    }

    public class HamQthOptions
    {
        public string? Username { get; set; }
        public string? Password { get; set; }

        public bool Configured =>
            !string.IsNullOrWhiteSpace(Username) && !Username.StartsWith("#{")
            && !string.IsNullOrWhiteSpace(Password) && !Password.StartsWith("#{");
    }
}
