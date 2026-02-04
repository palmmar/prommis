using System.ComponentModel.DataAnnotations;

namespace StegStat.Models;

public class Invitation
{
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    public Group? Group { get; set; }

    [Required]
    [StringLength(100)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string CreatedById { get; set; } = string.Empty;

    public ApplicationUser? CreatedBy { get; set; }

    public string? AcceptedById { get; set; }

    public ApplicationUser? AcceptedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public bool IsActive(DateTime now)
    {
        return UsedAt is null && now <= ExpiresAt;
    }
}
