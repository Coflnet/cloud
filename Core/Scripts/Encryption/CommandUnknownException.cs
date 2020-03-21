namespace Coflnet
{
    public class CommandUnknownException : CoflnetException {
		public CommandUnknownException (string slug, long msgId = -1) : base ("unknown_command", $"The command `{slug}` is unknown. It may not be registered on the target Resource yet.", null, 404, null, msgId) { }

		public CommandUnknownException (string slug, Referenceable target, long msgId = -1) 
		: base ("unknown_command", $"The command `{slug}` wasn't found on the Resource {target.Id} ({target.GetType().Name}).", null, 404, null, msgId) { }
	}

}