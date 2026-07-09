//using JobPortal.Domain.Entities;

//namespace JobPortal.Services.AI;

//public static class ProfileTextBuilder
//{
//    public static string BuildCandidate(CandidateProfile profile)
//    {
//        var skills = string.Join(", ",
//            profile.Skills.Select(x => x.SkillName));

//        return $"""
//        Name: {profile.FullName}

//        Trade: {profile.PrimaryTrade}

//        Experience: {profile.TotalExperienceYears} years

//        Skills:
//        {skills}

//        Professional Summary:
//        {profile.ProfessionalSummary}

//        About:
//        {profile.About}

//        Location:
//        {profile.CurrentCity}, {profile.CurrentState}
//        """;
//    }
//}





using JobPortal.Domain.Entities;

namespace JobPortal.Services.AI;

public static class ProfileTextBuilder
{
    public static string BuildCandidate(CandidateProfile profile)
    {
        var skills = string.Join(", ",
            profile.Skills
                .Where(x => x.SkillType == "Skill")   // भाषा वगळून फक्त actual skills
                .Select(x => x.SkillName));

        var education = string.Join(", ",
            profile.Educations?
                .Select(e => $"{e.EducationLevel}" +
                    (!string.IsNullOrWhiteSpace(e.InstituteName) ? $" from {e.InstituteName}" : ""))
            ?? new List<string>());

        var workHistory = string.Join("; ",
            profile.WorkHistories?
                .OrderByDescending(w => w.StartDate)
                .Select(w => $"{w.JobTitle} at {w.CompanyName}" +
                    (!string.IsNullOrWhiteSpace(w.JobDescription) ? $" — {w.JobDescription}" : ""))
            ?? new List<string>());

        return $"""
        Name: {profile.FullName}

        Trade: {profile.PrimaryTrade}

        Experience: {profile.TotalExperienceYears} years

        Skills:
        {skills}

        Education:
        {education}

        Work History:
        {workHistory}

        Professional Summary:
        {profile.ProfessionalSummary}

        About:
        {profile.About}

        Location:
        {profile.CurrentCity}, {profile.CurrentState}
        """;
    }
}