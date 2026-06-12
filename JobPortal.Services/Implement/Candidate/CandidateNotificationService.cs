// GetNotificationsAsync
using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

var userId = await _context.CandidateProfiles
    .Where(x => x.CandidateId == candidateId)
    .Select(x => x.UserId)
    .FirstOrDefaultAsync();

var query = _context.Notifications.Where(x => x.UserId == userId);
// rest is identical to recruiter