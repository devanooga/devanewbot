namespace devanewbot.Services.Ham;

using System;
using System.Collections.Generic;

public record HamSessionView(
    string Callsign,
    string Band,
    string Mode,
    string? Grid,
    string? SlackUserId,
    long? FrequencyHz,
    DateTime OpenedAt,
    DateTime LastHeardAt,
    DateTime? ClosedAt,
    HamSessionState State,
    int SpotCount,
    int ReporterCount,
    HamSessionView.Reporter? Furthest,
    HamSessionView.Reporter? Loudest,
    IReadOnlyList<HamSessionView.CountryCount> Countries,
    IReadOnlyList<string> Sources,
    string? Comment)
{
    public record Reporter(string Callsign, string? Country, double? DistanceKm, int? Snr);

    public record CountryCount(string Country, int Reporters);
}
