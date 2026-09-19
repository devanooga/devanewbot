namespace devanewbot.Models;

using System;

public record ReceivedSpot(
    string Source,
    string Callsign,
    string Reporter,
    string? ReporterGrid,
    string? SpottedGrid,
    long FrequencyHz,
    string Mode,
    int? Snr,
    int? Wpm,
    string? Comment,
    DateTime HeardAt)
{
    public string Band => HamBand.FromFrequency(FrequencyHz);
}
