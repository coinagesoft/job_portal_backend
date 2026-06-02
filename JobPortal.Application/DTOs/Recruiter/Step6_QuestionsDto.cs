namespace JobPortal.Application.DTOs.JobPosting;

public class QuestionsRequestDto
{
    public List<ScreeningQuestion> Questions { get; set; } = new();
}

public class ScreeningQuestion
{
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerType { get; set; } = "Yes_No";
    public bool IsMandatory { get; set; } = true;
}