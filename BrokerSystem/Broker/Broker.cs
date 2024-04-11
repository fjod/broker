using System.Diagnostics;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Broker;

public class Broker : IBroker
{
    private readonly IKeyComputeService _keyComputeService;
    private readonly BrokerSettings _settings;

    public Broker(IOptions<BrokerSettings> settings, IKeyComputeService keyComputeService)
    {
        _keyComputeService = keyComputeService;
        _settings = settings.Value;
    }

    public async Task<ResponseMessage> Post(RequestMessage message, CancellationToken token)
    {
        var key = _keyComputeService.CalculatePathKey(message);
        File.WriteAllText(Path.Combine(_settings.Path, $"{key}.req"), string.Empty);
        
        // Ответ брокера ожидается в файле “ключ запроса.resp”,где первой строкой будет http код,
        //а остальное - тело ответа для вызывающего. После вычитки ответа файлы ответа и запроса должны удаляться с диска сервисом.
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var resp = Path.Combine(_settings.Path, $"{key}.resp");
        while (true)
        {
            if (token.IsCancellationRequested)
            {
                Console.Out.WriteLine($"Запрос {message.Body} {message.Code} {message.Path} отменен");
                return new ResponseMessage(204, string.Empty);
            }

            if (sw.ElapsedMilliseconds > _settings.TimeOutInSeconds*1000)
            {
                Console.Out.WriteLine($"Запрос {message.Body} {message.Code} {message.Path} отменен по таймауту");
                throw new BrokerTimeoutException();
            }
            
            if (File.Exists(resp) == false)
            {
                await Task.Delay(1, token);
                continue;
            }

            return GenerateResponse(resp, key);
        }
    }

    private ResponseMessage GenerateResponse(string resp, string key)
    {
        var responseContent = File.ReadLines(resp);

        int lineCounter = 0;
        int statusCode = 0;
        string msg = string.Empty;
        foreach (var line in responseContent)
        {
            // первой строкой будет http код, а остальное - тело ответа для вызывающего
            if (lineCounter == 0)
            {
                var code = int.TryParse(line, out var temp);
                if (!code)
                {
                    Console.Out.WriteLine($"Ответ содержит невалидные данные {line}");
                    throw new BadResponseContentException("Первая строка - не число");
                }

                statusCode = temp;
                lineCounter++;
                continue;
            }
            if (lineCounter == 1)
            {
                msg = line;
                lineCounter++;
                continue;
            }

            if (lineCounter >= 2)
            {
                Console.Out.WriteLine($"Ответ содержит невалидные данные {line}");
                throw new BadResponseContentException("Слишком много строк в файле");
            }
        }
            
        if (lineCounter == 0)
        {
            Console.Out.WriteLine("Ответ пуст");
            throw new BadResponseContentException("Пустой ответ");
        }
            
        // После вычитки ответа файлы ответа и запроса должны удаляться с диска сервисом.
        try
        {
            File.Delete(Path.Combine(_settings.Path, $"{key}.req"));
            File.Delete(resp);
        }
        catch (Exception e)
        {
            Console.Out.WriteLine($"Не получилось удалить файлы ответа или запроса {e.Message}");
            throw new UnableToDeleteFilesException(e.Message);
        }
           
        return new ResponseMessage(statusCode, msg);
    }
}