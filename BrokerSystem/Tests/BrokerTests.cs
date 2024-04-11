using Broker;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests;

public class BrokerTests
{
    private IOptions<BrokerSettings> _settings;
    private IKeyComputeService _keyComputeService = new KeyComputeService();
    public BrokerTests()
    {
        var settingsMock = new Mock<IOptions<BrokerSettings>>();
        settingsMock
            .Setup(o => o.Value)
            .Returns(new BrokerSettings{Path = Path.GetTempPath(), TimeOutInSeconds = 1});
        _settings = settingsMock.Object;
    }
    
    [Fact]
    public async Task MessageResponse_Fine()
    {
        int code = 200;
        var msgBody = "body";
        var request = new RequestMessage(1, "2", "3");
        var key = _keyComputeService.CalculatePathKey(request);
        var responsePath = Path.Combine(_settings.Value.Path, $"{key}.resp");
        
        var sut = new Broker.Broker(_settings, _keyComputeService);
        var sutResponse = sut.Post(request, CancellationToken.None);
        File.WriteAllText(responsePath, $"{code} {Environment.NewLine}{msgBody}");
      
        var response = await sutResponse;
        Assert.True(response.Code == code);
        Assert.True(response.Body == msgBody);
        Assert.False(File.Exists(responsePath));
        Assert.False(File.Exists(Path.Combine(_settings.Value.Path, $"{key}.req")));
    }
    
    [Fact]
    public async Task MessageResponse_ThrowsOnTimeout()
    {
        var request = new RequestMessage(1, "2", "3");
        var sut = new Broker.Broker(_settings, _keyComputeService);
        Func<Task<ResponseMessage>> sutResponse = () => sut.Post(request, CancellationToken.None);
        await Assert.ThrowsAsync<BrokerTimeoutException>(sutResponse);
    }
}