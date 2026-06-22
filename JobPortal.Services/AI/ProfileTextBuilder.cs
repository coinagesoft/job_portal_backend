using JobPortal.Domain.Entities;

namespace JobPortal.Services.AI;

public static class ProfileTextBuilder
{
    public static string BuildCandidate(CandidateProfile profile)
    {
        var skills = string.Join(", ",
            profile.Skills.Select(x => x.SkillName));

        return $"""
        Name: {profile.FullName}

        Trade: {profile.PrimaryTrade}

        Experience: {profile.TotalExperienceYears} years

        Skills:
        {skills}

        Professional Summary:
        {profile.ProfessionalSummary}

        About:
        {profile.About}

        Location:
        {profile.CurrentCity}, {profile.CurrentState}
        """;
    }
}