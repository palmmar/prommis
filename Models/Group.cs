using System.ComponentModel.DataAnnotations;

namespace Prommis.Models;

public class Group
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
}
