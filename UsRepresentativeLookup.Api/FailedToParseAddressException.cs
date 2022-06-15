namespace UsRepresentativeLookup.Api;

public class FailedToParseAddressException : Exception
{
    public FailedToParseAddressException(Exception innerException) : base(innerException.Message, innerException)
    {
    }
}