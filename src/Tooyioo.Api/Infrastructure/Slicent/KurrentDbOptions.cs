using System.ComponentModel.DataAnnotations;

namespace Tooyioo.Api.Infrastructure.Slicent;

public sealed class KurrentDbOptions
{
    public const string SectionName = "KurrentDb";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
