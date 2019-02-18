﻿using Coflnet;
using MessagePack;

namespace Coflnet
{
	public class LoginUser : Command
	{
		public override void Execute(MessageData data)
		{
			throw new CommandExistsOnServer();
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings();
		}

		public override string GetSlug()
		{
			return "loginUser";
		}
	}

	[MessagePackObject]
	public class LoginParams
	{
		[Key(0)]
		public SourceReference id;
		[Key(1)]
		public byte[] secret;

	}
}

