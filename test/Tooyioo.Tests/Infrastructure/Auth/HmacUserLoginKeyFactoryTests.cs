using Microsoft.Extensions.Options;
using Tooyioo.Api.Infrastructure.Auth;

namespace Tooyioo.Tests.Infrastructure.Auth;

[Category(TestCategories.Infrastructure)]
public sealed class HmacUserLoginKeyFactoryTests
{
    [Test]
    public async Task Then_the_issuer_comes_from_the_token()
    {
        var key = CreateFactory().ForOidc("https://accounts.google.com", "subject-1");

        await Assert.That(key.Kind).IsEqualTo("oidc");
        await Assert.That(key.Issuer).IsEqualTo("https://accounts.google.com");
    }

    [Test]
    public async Task Then_different_subjects_produce_different_keys()
    {
        var factory = CreateFactory();

        var first = factory.ForOidc("https://accounts.google.com", "subject-1");
        var second = factory.ForOidc("https://accounts.google.com", "subject-2");

        await Assert.That(first.Key).IsNotEqualTo(second.Key);
    }

    private static HmacUserLoginKeyFactory CreateFactory()
        => new(Options.Create(new UserLoginOptions
        {
            Pepper = Convert.ToBase64String(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray())
        }));
}
