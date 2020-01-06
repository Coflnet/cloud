using Coflnet.Core;
using MessagePack;

namespace Coflnet
{
    /// <summary>
    /// Application specific data for some user
    /// </summary>
    [MessagePackObject]
    public class ApplicationData : Referenceable,IMessagePackSerializationCallbackReceiver
    {
		private static CommandController commands = new CommandController();

		[Key("aId")]
		public SourceReference ApplicationId;

		[Key("kv")]
		public RemoteDictionary<string,string> KeyValues;

		/// <summary>
		/// The <see cref="KeyPair"/> Visible to other members of a chat
		/// </summary>
		[Key("kp")]
		public SigningKeyPair communicationKeyPair;


		/// <summary>
		/// Creates a new Instance of the <see cref="ApplicationData"/> class.
		/// </summary>
		/// <param name="applicationId">The application this data coresponds to</param>
		public ApplicationData(SourceReference applicationId)
		{
			this.ApplicationId = applicationId;
			KeyValues = new RemoteDictionary<string, string>();
			OnAfterDeserialize();
		}

		/// <summary>
		/// Creates a new Instance of the <see cref="ApplicationData"/> class.
		/// This constructor doesn't initialize any of the values and is meant for deserialization only.
		/// If you use it, call <see cref="ApplicationData.OnAfterDeserialize()"/> afterwards.
		/// </summary>
		public ApplicationData()
		{}

		static ApplicationData()
		{
			RemoteDictionary<string,string>.AddCommands(commands,nameof(KeyValues),
					m=>m.GetTargetAs<ApplicationData>().KeyValues,
					(m,newD)=>{m.GetTargetAs<ApplicationData>().KeyValues.Value = newD;});
		}

        public override CommandController GetCommandController()
        {
            return commands;
        }

        public void OnBeforeSerialize()
        {
            return;
        }

        public void OnAfterDeserialize()
        {
            KeyValues.SetDetails(nameof(KeyValues),this);
        }
    }
}