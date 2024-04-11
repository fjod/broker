using Domain.Models;

namespace Domain.Interfaces;

public interface IKeyComputeService
{
     string CalculateSquashKey(RequestMessage message);
     string CalculatePathKey(RequestMessage message);
}