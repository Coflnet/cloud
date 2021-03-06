﻿using Coflnet;
using MessagePack;

namespace Coflnet {
	public class RegisteredUser : Command {
		public override void Execute (CommandData data) {
			var response = data.GetAs<RegisterUserResponse> ();
			ConfigController.UserSettings.userId = response.id;
			ConfigController.UserSettings.userSecret = response.secret;

			// Login

		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug {
			get {

				return "registeredUser";
			}
		}
	}
}