using Domain.Models;

namespace Domain.Interfaces;

public interface IBroker
{
    Task<ResponseMessage> Post(RequestMessage message, CancellationToken token);
}