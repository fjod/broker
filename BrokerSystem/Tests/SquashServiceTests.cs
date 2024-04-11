using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using WebApi;
using WebApi.Services;
using Xunit;

namespace Tests;

public class SquashServiceTests
{
    private const int SquashLimit = 3;
    private IOptions<SquashSettings> _settings;
    private IBroker Broker;
    private IKeyComputeService _keyComputeService = new KeyComputeService();
    private static readonly ResponseMessage BrokerResponse = new ResponseMessage(StatusCodes.Status202Accepted, "resp");

    public SquashServiceTests()
    {
        var settingsMock = new Mock<IOptions<SquashSettings>>();
        settingsMock
            .Setup(o => o.Value)
            .Returns(new SquashSettings{ Limit = SquashLimit});
        _settings = settingsMock.Object;

        var brokerMock = new Mock<IBroker>();
        brokerMock
            .Setup(b => b.Post(It.IsAny<RequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BrokerResponse);
        Broker = brokerMock.Object;
    }

    [Fact]
    public async Task SimpleSquash_Works()
    {
        var sut = new SquashService(Broker, _settings, _keyComputeService);
        var request = new RequestMessage(1, "2", "3");

        for (int i = 0; i < SquashLimit; i++)
        {
            var r1 = await sut.Push(request, CancellationToken.None);
            Assert.True(String.IsNullOrEmpty(r1.Body));
        }
      
        var r4 = await sut.Push(request, CancellationToken.None);
        Assert.True(r4.Body == "resp");
    }
}