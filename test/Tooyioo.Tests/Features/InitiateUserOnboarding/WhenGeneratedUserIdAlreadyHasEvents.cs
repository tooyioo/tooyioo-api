using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.Extensions;
using Tooyioo.Tests.Support;
using Tooyioo.Tests.Support.Http;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User;
using Tooyioo.User.Features.InitiateUserOnboarding;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.Tests.Features.InitiateUserOnboarding;

[Category(TestCategories.Conflict)]
public sealed class WhenGeneratedUserIdAlreadyHasEvents
    : CommandVerticalSliceTest
{
    private FixedIdCommandMapper _mapper = null!;
    private UserId _userId = null!;
    private UserOnboardingId _userOnboardingId = null!;
    private HttpResponseMessage _response = null!;
    private HttpProblemDetails _body = null!;

    protected override Task<VerticalSliceTestHost> CreateHost()
    {
        _mapper = new FixedIdCommandMapper();

        return VerticalSliceTestHost.Start(services =>
        {
            services.RemoveAll<ICommandMapper<InitiateUserOnboardingRequest, HttpContext,
                InitiateUserOnboardingCommand>>();
            services.AddSingleton<ICommandMapper<InitiateUserOnboardingRequest, HttpContext,
                InitiateUserOnboardingCommand>>(_mapper);
        });
    }

    protected override async Task Given()
    {
        _userId = new UserId(1.ToGuid().ToString());
        _userOnboardingId = new UserOnboardingId(2.ToGuid().ToString());

        _mapper.UserId = _userId;
        _mapper.UserOnboardingId = _userOnboardingId;

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(
            new UserPersonalDataId(_userId.Value),
            DomainEvent.UserPersonalDataRecorded().Build());
    }

    protected override async Task When()
    {
        var token = Host.GoogleIdentityToken()
            .WithSubject("google-sub-123")
            .Build();

        _response = await Host.HttpClient.PostJson(
            "/user-onboarding/initiate",
            new InitiateUserOnboardingRequest(),
            token);

        _body = await _response.ReadJson<HttpProblemDetails>();
    }

    [Test]
    public async Task Then_response_is_conflict()
        => await Assert.That(_response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

    [Test]
    public async Task Then_response_title_is_onboarding_state_unexpected()
        => await Assert.That(_body.Title).IsEqualTo("onboarding_state_unexpected");

    private sealed class FixedIdCommandMapper
        : ICommandMapper<InitiateUserOnboardingRequest, HttpContext, InitiateUserOnboardingCommand>
    {
        public UserId UserId { get; set; } = null!;
        public UserOnboardingId UserOnboardingId { get; set; } = null!;

        public InitiateUserOnboardingCommand Map(
            InitiateUserOnboardingRequest request,
            HttpContext context)
        {
            var productionMapper = new InitiateUserOnboardingMappers.CommandMapper(
                context.RequestServices.GetRequiredService<IExternalIdentityRetriever>(),
                context.RequestServices.GetRequiredService<IUserLoginKeyFactory>());

            var command = productionMapper.Map(request, context);

            return command with
            {
                UserId = UserId,
                UserOnboardingId = UserOnboardingId
            };
        }
    }
}
