using JobPortal.Application.DTOs.Public;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IPublic
{
    public interface ILegalDocumentPublicService
    {
        /// <summary>
        /// Returns the currently published version of a legal document
        /// ("privacy" or "terms"), or null if nothing has been published yet.
        /// </summary>
        Task<LegalDocumentPublicDto?> GetPublishedAsync(string type);
    }
}