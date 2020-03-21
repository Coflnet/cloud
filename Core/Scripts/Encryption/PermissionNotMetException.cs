namespace Coflnet
{
    /// <summary>
    /// Insuficient permission exception.
    /// Thrown when at least one Permission required is not fullfilled
    /// </summary>
    public class PermissionNotMetException : CoflnetException {
		public PermissionNotMetException (long msgId = -1, string message = "You are currently not allowed to execute this command. ", string userMessage = "No permission", string info = null) 
		: base ("permission_not_met", message, userMessage, 403, null, msgId) { }
	
	
		public PermissionNotMetException(string permissionSlug,SourceReference targetId,SourceReference senderId,string commandSlug,long messageId = -1) 
		: base("permission_not_met", $"The permission {permissionSlug} required for executing the command {commandSlug} on {targetId} wasn't met by {senderId}","No permission",403,null,messageId)
		{}
	}

}