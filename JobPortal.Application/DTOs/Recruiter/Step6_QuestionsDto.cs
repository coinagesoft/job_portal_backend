namespace JobPortal.Application.DTOs.JobPosting;

public class ScreeningQuestion
{
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerType { get; set; } = "Yes_No";  // Yes_No | Text | Number
    public bool IsMandatory { get; set; } = true;
}

public class QuestionsRequestDto
{
    /// <summary>
    /// Screening questions — max 5
    /// </summary>
    public List<ScreeningQuestion> Questions { get; set; } = new();
}