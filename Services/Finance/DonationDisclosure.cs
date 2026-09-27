namespace devanewbot.Services.Finance;

using System.Collections.Generic;
using System.Linq;
using devanewbot.Data.Models;

// The code of conduct names donors for a one-time gift over $50, over $600 in a calendar year, or on request.
// Once any of a donor's gifts qualifies, all of their gifts are shown under their name.
public static class DonationDisclosure
{
    public const decimal OneTimeThreshold = 50m;
    public const decimal YearlyThreshold = 600m;

    public static Dictionary<Donation, string?> Reasons(IReadOnlyCollection<Donation> donations)
    {
        var yearly = donations
            .GroupBy(donation => (Key(donation.Donor), donation.DonatedAt.Year))
            .ToDictionary(group => group.Key, group => group.Sum(donation => donation.Amount));

        string? Own(Donation donation) =>
            donation.NamedOnRequest ? "at the donor's request"
            : !donation.Recurring && donation.Amount > OneTimeThreshold ? "one-time gift over $50"
            : yearly[(Key(donation.Donor), donation.DonatedAt.Year)] > YearlyThreshold ? "over $600 that year"
            : null;

        var publicDonors = donations
            .Where(donation => Own(donation) is not null)
            .Select(donation => Key(donation.Donor))
            .ToHashSet();

        return donations.ToDictionary(donation => donation, donation =>
            Own(donation) ?? (publicDonors.Contains(Key(donation.Donor)) ? "named for another gift" : null));
    }

    private static string Key(string donor) => donor.Trim().ToLowerInvariant();
}
