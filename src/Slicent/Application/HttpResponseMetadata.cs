namespace Slicent.Application;

/// <summary>
/// The status codes an endpoint mapper can return. 400, 401 and 403 are added by the endpoint
/// conventions because they are produced before the mapper runs.
/// </summary>
public sealed record HttpResponseMetadata(
    IReadOnlyList<int> SuccessStatusCodes,
    IReadOnlyList<int> ErrorStatusCodes);
