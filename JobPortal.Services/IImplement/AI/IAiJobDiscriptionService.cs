using JobPortal.Application.DTOs.Recruiter.AIJobDescription;

namespace JobPortal.Services.IImplement.AI;

public interface IAiJobDescriptionService
{
    /// <summary>
    /// Auto-generates a complete JD the moment the employer finishes filling
    /// title / role / category / exp / job type.  No manual button needed.
    /// </summary>
    Task<AutoGenerateJdResponseDto> AutoGenerateAsync(
        AutoGenerateJdRequestDto request);

    /// <summary>
    /// Returns 3 short inline suggestions while the employer is typing
    /// in the JD textarea (call on debounced keyup, ~600 ms).
    /// </summary>
    Task<JdInlineSuggestionResponseDto> GetInlineSuggestionsAsync(
        JdInlineSuggestionRequestDto request);

    /// <summary>
    /// Suggest key skills for a given job title / trade (Step 3).
    /// </summary>
    Task<AiSkillSuggestionResponseDto> SuggestSkillsAsync(
        AiSkillSuggestionRequestDto request);
}
