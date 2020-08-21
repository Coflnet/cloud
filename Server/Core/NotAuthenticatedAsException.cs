using Coflnet;

public class NotAuthenticatedAsException : CoflnetException
{
	public NotAuthenticatedAsException(EntityId inquestion, string userMessage = null, int responseCode = 403, string info = null, long msgId = -1) : base("not_authenticated_as", $"You didn't authenticated as {inquestion.ToString()} over this connection", userMessage, responseCode, info)
	{
		
	}
}






