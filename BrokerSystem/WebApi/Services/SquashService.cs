using System.Collections.Concurrent;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace WebApi.Services;

public class SquashService : ISquashService
{
    private readonly IBroker _broker;
    private readonly IKeyComputeService _keyComputeService;
    private readonly SquashSettings _settings;
    private object CreateLock = new Object();
    private static SemaphoreSlim SquashLock = new SemaphoreSlim(1);
    private static readonly ResponseMessage Squashed = new ResponseMessage(StatusCodes.Status202Accepted, string.Empty);

    private static readonly ConcurrentDictionary<string, ConcurrentBag<RequestMessage>> Store = new();

    public SquashService(IBroker broker, IOptions<SquashSettings> settings, IKeyComputeService keyComputeService)
    {
        _broker = broker;
        _keyComputeService = keyComputeService;
        _settings = settings.Value;
    }
    
    public async Task<ResponseMessage> Push(RequestMessage message, CancellationToken token)
    {
        var key = _keyComputeService.CalculateSquashKey(message);
        if (!Store.TryGetValue(key, out var bag))
        {
            lock (CreateLock)
            {
                if (!Store.ContainsKey(key))
                {
                    var newBag = new ConcurrentBag<RequestMessage>();
                    newBag.Add(message);
                    //  false if the key already exists.
                    Store.TryAdd(key, newBag);
                }
            }
        }
        else
        {
            bag.Add(message);
            if (bag.Count > _settings.Limit)
            {
                await SquashLock.WaitAsync(token);
                if (bag.Count > _settings.Limit)
                {
                    // получится если брокер завис, то все новые входящие сообщения будут висеть в локе
                    // нужно как-то почистить bag и запостить в брокер сообщения, но дать возможность добавлять новые
                    // сообщения
                    var msg = Squash(bag);
                    bag.Clear();
                    
                    // отпускаем лок с пустым bag, теперь следующие сообщения не зависят от результата работы брокера
                    // на предыдущем bag
                    SquashLock.Release();
                    return await _broker.Post(msg, token);
                }
            }
        }

        return Squashed;
    }
    
    // требуется схлопывать идентичные запросы в один запрос брокеру - можно даже не хранить сами запросы, а просто их количество
    private static RequestMessage Squash(ConcurrentBag<RequestMessage> messages)
    {
        return messages.First();
    }
}