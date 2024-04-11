namespace Domain.Models;

public record RequestMessage(int Code, string Path, string Body);