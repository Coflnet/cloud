﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Coflnet.Server {
	public class LeaderBoardController : IRegisterCommands {
		public static LeaderBoardController Instance;
		Dictionary<SourceReference, Leaderboard> leaderboards = new Dictionary<SourceReference, Leaderboard> ();

		static LeaderBoardController () {
			Instance = new LeaderBoardController ();
		}

		public void RegisterCommands (CommandController controller) {
			controller.RegisterCommand<GetCurrentScore> ();
		}

		public class GetCurrentScore : DefaultServerCommand {

			public override void Execute (MessageData data) {
				Reference<CoflnetUser> user = new Reference<CoflnetUser> (new SourceReference ());
				int currentScore = LeaderBoardController.Instance.leaderboards[data.GetAs<SourceReference> ()].scores[user].score;
				SendBack (data, currentScore);
			}

			public override string Slug {
				get {

					return "getLeaderbaordScore";
				}
			}

		}

	}

	public class Leaderboard : Referenceable {
		public string publicId;
		public string name;
		public bool smallerIsBetter;

		public Dictionary<Reference<CoflnetUser>, LeaderboardScore> scores;
		public List<LeaderboardPercentage> cachedPercentages;

		public int GetPercentageRank (Reference<CoflnetUser> reference) {
			int score = scores[reference].score;
			foreach (var item in cachedPercentages) {
				if (item.lowerBorder < score) {
					return item.percentage;
				}
			}
			return 0;
		}

		public void SubmitScore (Reference<CoflnetUser> user, int score) {
			if (!scores.ContainsKey (user)) {
				scores[user] = new LeaderboardScore (user);
			}

			scores[user].score = score;
			// check if next percentage is reached
			if (scores[user].percentage.upperBorder < score) {
				UpdatePercentage (scores[user]);
			}
		}

		/// <summary>
		/// Points remaining the till next percentage mark is reached.
		/// </summary>
		/// <returns>The points till next percentage.</returns>
		/// <param name="user">User.</param>
		public int PointsTillNextPercentage (Reference<CoflnetUser> user) {
			LeaderboardScore score = scores[user];
			return score.percentage.upperBorder - score.score;
		}

		public int PlayersWorseThan (Reference<CoflnetUser> user) {
			// Finds out in what percentage this user is and gets the average distribution
			return scores[user].percentage.percentage * scores.Count / 100;
		}

		public int CurrentScoreOf (Reference<CoflnetUser> user) {
			return scores[user].score;
		}

		/// <summary>
		/// Searches the leaderboard for all scores of friends
		/// </summary>
		/// <returns>The scores of friends.</returns>
		/// <param name="friends">Friends to search the leaderboard for.</param>
		public FriendLeaderBoard GetScoresOfFriends (List<Reference<CoflnetUser>> friends) {
			FriendLeaderBoard friendLeaderBoard = new FriendLeaderBoard (friends.Count);
			foreach (var friend in friends) {
				if (scores.ContainsKey (friend)) {
					friendLeaderBoard.AddScoreForFriend (scores[friend]);
				}
			}
			return friendLeaderBoard;
		}

		protected void UpdatePercentage (LeaderboardScore score) {
			// find new percentage
			LeaderboardPercentage percentage = null;
			foreach (var item in cachedPercentages) {
				if (percentage.lowerBorder < score.score) {
					score.percentage = item;
				}
			}
		}

		public void UpdateCache () {
			// Sort scores 
			// needs using System.Linq;
			var result = scores.OrderBy ((arg) => arg.Value.score);

			var playerInOneStep = scores.Count / 100;
			// Calcualte and store the percentage cache
			for (int i = 0; i < 100; i++) {
				LeaderboardPercentage percentage = cachedPercentages[i];
				percentage.percentage = i + 1;
				percentage.lowerBorder = scores.ElementAt (playerInOneStep * i).Value.score;
				percentage.upperBorder = scores.ElementAt (playerInOneStep * (i + 1)).Value.score;
				percentage.playerCount = playerInOneStep;
			}
		}

		public override CommandController GetCommandController () {
			throw new System.NotImplementedException ();
		}
	}

	public class LeaderboardScore {
		public int score;
		public Reference<CoflnetUser> user;
		public LeaderboardPercentage percentage;

		public LeaderboardScore (Reference<CoflnetUser> user) {
			this.user = user;
		}
	}

	public class LeaderboardPercentage {
		public int lowerBorder;
		public int upperBorder;
		public int percentage;
		public int playerCount;
	}

	/// <summary>
	/// Scores for friends of a specific user
	/// may need to be sorted
	/// </summary>
	public class FriendLeaderBoard {
		public List<LeaderboardScore> scores;

		public FriendLeaderBoard (List<LeaderboardScore> scores) {
			this.scores = scores;
		}

		public FriendLeaderBoard (int friendCount) {
			this.scores = new List<LeaderboardScore> (friendCount);
		}

		public void AddScoreForFriend (LeaderboardScore score) {
			scores.Add (score);
		}
	}

}