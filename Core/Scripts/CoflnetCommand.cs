using System;

namespace Coflnet
{
    [Obsolete ("You should now derive from the abstract class 'Command'")]
	public class CoflnetCommand {
		public delegate void Command (CommandData commandData);
		private string slug;
		private Command command;
		private bool threadAble;
		private bool encrypted;

		public CoflnetCommand (string slug, Command command, bool threadAble, bool encrypted) {
			this.slug = slug;
			this.command = command;
			this.threadAble = threadAble;
			this.encrypted = encrypted;
		}

		/// <summary>
		/// Is this command encrypted while in trasmit?
		/// </summary>
		public bool IsEncrypted () {
			return encrypted;
		}

		/// <summary>
		/// Is this command able to be executed in another thread
		/// </summary>
		public bool IsThreadAble () {
			return threadAble;
		}

		/// <summary>
		/// Gets the command actual function behind this command.
		/// </summary>
		/// <returns>The command.</returns>
		public Command GetCommand () {
			return command;
		}

		public string Slug {
			get {

				return slug;
			}
		}
	}

}