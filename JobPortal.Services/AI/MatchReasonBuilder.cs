namespace JobPortal.Services.AI;

public static class MatchReasonBuilder
{
    public static string Build(
        int trade,
        int skill,
        int experience,
        int location)
    {
        var reasons = new List<string>();

        if (trade == 100)
            reasons.Add("Strong trade match");

        if (skill >= 70)
            reasons.Add("Good skill alignment");

        if (experience >= 80)
            reasons.Add("Experience matches requirement");

        if (location == 100)
            reasons.Add("Location matches");

        return string.Join(", ", reasons);
    }
}