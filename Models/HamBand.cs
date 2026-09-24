namespace devanewbot.Models;

public static class HamBand
{
    private static readonly (long LowHz, long HighHz, string Name)[] Bands =
    [
        (1_800_000, 2_000_000, "160m"),
        (3_500_000, 4_000_000, "80m"),
        (5_250_000, 5_450_000, "60m"),
        (7_000_000, 7_300_000, "40m"),
        (10_100_000, 10_150_000, "30m"),
        (14_000_000, 14_350_000, "20m"),
        (18_068_000, 18_168_000, "17m"),
        (21_000_000, 21_450_000, "15m"),
        (24_890_000, 24_990_000, "12m"),
        (28_000_000, 29_700_000, "10m"),
        (50_000_000, 54_000_000, "6m"),
        (70_000_000, 71_000_000, "4m"),
        (144_000_000, 148_000_000, "2m"),
        (222_000_000, 225_000_000, "1.25m"),
        (420_000_000, 450_000_000, "70cm"),
        (902_000_000, 928_000_000, "33cm"),
        (1_240_000_000, 1_300_000_000, "23cm")
    ];

    public static string FromFrequency(long frequencyHz)
    {
        foreach (var (low, high, name) in Bands)
        {
            if (frequencyHz >= low && frequencyHz <= high)
            {
                return name;
            }
        }

        return $"{frequencyHz / 1_000_000.0:0.###}MHz";
    }
}
