using System.ComponentModel.DataAnnotations;

namespace Prommis.Models;

public class StepEntry
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Range(1, 200000)]
    public int Steps { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
