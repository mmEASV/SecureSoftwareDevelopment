namespace Template.Api.Domain.Exceptions;

public class NotFoundException(string error) : CustomException(error)
{
    public NotFoundException() : this("Not found") { }
}
