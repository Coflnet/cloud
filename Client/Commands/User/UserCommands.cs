using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coflnet.Client
{
	public class UserCommands
	{
		public class GetFilesInfo : Command
		{
			public override void Execute(MessageData data)
			{
				throw new System.NotImplementedException();
			}

			public override CommandSettings GetSettings()
			{
				return new CommandSettings(true);
			}

			public override string GetSlug()
			{
				return "get:user/files";
			}
		}

		public class SetName : Command
		{
			public override void Execute(MessageData data)
			{
				throw new System.NotImplementedException();
			}

			public override CommandSettings GetSettings()
			{
				return new CommandSettings();
			}

			public override string GetSlug()
			{
				return "put:user/name";
			}
		}


		public class GetName : Command
		{
			public override void Execute(MessageData data)
			{
				throw new System.NotImplementedException();
			}

			public override CommandSettings GetSettings()
			{
				throw new System.NotImplementedException();
			}

			public override string GetSlug()
			{
				throw new System.NotImplementedException();
			}
		}



		public void GetFileInfo(MessageData data)
		{

			UserFile file = data.GetTargetAs<CoflnetUser>().Files[data.GetAs<long>()];
		}

		public void GetFileInfoByReference(MessageData data)
		{
			//UserFile file = ReferenceManager.Instance.GetResource<UserFile>(data.GetAs<long>());
		}
	}
}
