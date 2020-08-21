using System.Collections;
using System.Collections.Generic;

namespace Coflnet.Client
{
	public class UserCommands
	{
		public class GetFilesInfo : Command
		{
			public override void Execute(CommandData data)
			{
				throw new System.NotImplementedException();
			}

			protected override CommandSettings GetSettings()
			{
				return new CommandSettings(true);
			}

			public override string Slug
{
	get
	{
	
				return "getFiles";
			}
}
		}

		public class SetName : Command
		{
			public override void Execute(CommandData data)
			{
				throw new System.NotImplementedException();
			}

			protected override CommandSettings GetSettings()
			{
				return new CommandSettings();
			}

			public override string Slug
{
	get
	{
	
				return "put:user/name";
			}
		}
		}


		public class GetName : Command
		{
			public override void Execute(CommandData data)
			{
				throw new System.NotImplementedException();
			}

			protected override CommandSettings GetSettings()
			{
				throw new System.NotImplementedException();
			}

			public override string Slug
{
	get
	{
	
				throw new System.NotImplementedException();
			}
		}
		}



		public void GetFileInfo(CommandData data)
		{

			UserFile file = data.GetTargetAs<CoflnetUser>().Files[data.GetAs<long>()];
		}

		public void GetFileInfoByReference(CommandData data)
		{
			//UserFile file = ReferenceManager.Instance.GetEntity<UserFile>(data.GetAs<long>());
		}
	}
}
