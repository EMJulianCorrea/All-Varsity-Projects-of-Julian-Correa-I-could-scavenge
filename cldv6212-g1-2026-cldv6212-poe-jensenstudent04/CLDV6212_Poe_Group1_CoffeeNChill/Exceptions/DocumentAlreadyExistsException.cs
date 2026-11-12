namespace CoffeeNChill.Functions.Exceptions;

public class DocumentAlreadyExistsException : Exception
{
    public DocumentAlreadyExistsException(string fileName)
        : base($"A staff document named '{fileName}' already exists.")
    {
    }
}
