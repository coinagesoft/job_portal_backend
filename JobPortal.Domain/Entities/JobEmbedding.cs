using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Domain.Entities;

[Table("job_embeddings")]
public class JobEmbedding
{
    [Key]
    [Column("job_id")]
    public Guid JobId { get; set; }

    [Column("embedding_json")]
    public string EmbeddingJson { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public JobPosting JobPosting { get; set; } = default!;
}