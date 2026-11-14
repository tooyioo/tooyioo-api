using Tooyioo.User;

namespace Tooyioo.Tests.Infrastructure;

[Category(TestCategories.Infrastructure)]
public sealed class WhenUserIdIsRendered
{
    [Test]
    public async Task Then_the_stream_identity_is_the_raw_value()
    {
        var userId = new UserId("00000000-0000-0000-0000-000000000010");

        await Assert.That(userId.ToString()).IsEqualTo(userId.Value);
        await Assert.That(userId.ToString()).IsEqualTo("00000000-0000-0000-0000-000000000010");
    }
}
