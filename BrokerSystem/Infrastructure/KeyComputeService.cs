using System.Text;
using Domain.Interfaces;
using Domain.Models;
using SharpHash.Base;

namespace Infrastructure;

public class KeyComputeService : IKeyComputeService
{
    public string CalculateSquashKey(RequestMessage message)
    {
        return HashFactory.Crypto.CreateMD5().ComputeString(message.Body, Encoding.Default).ToString();
    }

    public string CalculatePathKey(RequestMessage message)
    {
        // Расчет ключа для сохранения файла производить по формуле md5(http method + http path). 
        return HashFactory.Crypto.CreateMD5().ComputeString($"{message.Code}{message.Path}", Encoding.Default).ToString();
    }
}