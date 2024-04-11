using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class BrokerController : ControllerBase
{
    [HttpPost("Naive")]
    public async Task<ActionResult<ResponseMessage>> Naive(RequestMessage msg, [FromServices] IBroker broker, CancellationToken token)
    {
        try
        {
            // В “наивной” (primitive) реализации системы все входящие запросы поступают в брокер и ожидают ответа от него, который и передают вызывающему
            var response = await broker.Post(msg, token);
            // первой строкой ответа будет http код
            return StatusCode(response.Code, response.Body);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
    
    [HttpPost("Advanced")]
    public async Task<ActionResult<ResponseMessage>> Advanced(RequestMessage msg, [FromServices] ISquashService service, CancellationToken token)
    {
        try
        {
            // В продвинутой (advanced) реализации требуется схлопывать идентичные запросы в один запрос брокеру. 
            var response = await service.Push(msg, token);
            // первой строкой будет http код
            return StatusCode(response.Code, response.Body);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}
