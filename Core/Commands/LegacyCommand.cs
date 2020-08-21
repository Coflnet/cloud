namespace Coflnet
{
    public class LegacyCommand : Command {
		// disable the deprecated warning
		#pragma warning disable 612, 618
		CoflnetCommand oldCommand;

		public override void Execute (CommandData data) {
			oldCommand.GetCommand ().Invoke (data);
		}

		public override string Slug {
			get {

				return oldCommand.Slug;
			}
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (oldCommand.IsThreadAble (), oldCommand.IsEncrypted ());
		}

		public LegacyCommand (CoflnetCommand legacyCommand) {
			this.oldCommand = legacyCommand;
		}

		public LegacyCommand (string slug, CoflnetCommand.Command command, bool threadable = false, bool encrypted = false) {
			this.oldCommand = new CoflnetCommand (slug, command, threadable, encrypted);
		}

		// enable warning for obsulete again
		#pragma warning restore 612, 618
	}

}