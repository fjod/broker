using Domain.Models;

namespace Domain.Interfaces;

public interface ISquashService
{
    Task<ResponseMessage> Push(RequestMessage message, CancellationToken token);
}