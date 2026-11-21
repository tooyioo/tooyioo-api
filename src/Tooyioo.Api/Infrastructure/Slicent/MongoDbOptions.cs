using System.ComponentModel.DataAnnotations;

namespace Tooyioo.Api.Infrastructure.Slicent;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string Database { get; init; } = "Tooyioo";
}
