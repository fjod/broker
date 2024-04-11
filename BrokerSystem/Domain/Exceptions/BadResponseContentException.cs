namespace Domain.Exceptions;

public class BadResponseContentException(string msg) : Exception(msg);