using JobPortal.Domain.Entities;

namespace JobPortal.Services.AI;

public static class JobTextBuilder
{
    public static string BuildJob(JobPosting job)
    {
        return $"""
        Job Title:
        {job.JobTitle}

        Trade:
        {job.TradeCategory}

        Description:
        {job.JobDescription}

        Required Skills:
        {job.KeySkills}

      

        Location:
        {job.OnshoreCity}, {job.OnshoreState}
        """;
    }
}