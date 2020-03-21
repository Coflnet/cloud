namespace Coflnet
{
    /// <summary>
    /// interface for adding commands
    /// </summary>
    public interface IRegisterCommands {
		/// <summary>
		/// Should call controller.RegisterCommand
		/// </summary>
		void RegisterCommands (CommandController controller);
	}

}