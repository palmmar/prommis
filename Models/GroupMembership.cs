using System.ComponentModel.DataAnnotations;

namespace StegStat.Models;

public class GroupMembership
{
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    public Group? Group { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public GroupRole Role { get; set; } = GroupRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
