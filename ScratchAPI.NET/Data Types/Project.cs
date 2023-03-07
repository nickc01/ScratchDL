using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Scratch_Downloader
{
	public record class Project(
		long id,
		string title,
		string description,
		string instructions,
		string visibility,
        //[field:JsonProperty("public")]
        [property:JsonPropertyName("public")]
		bool is_public,
		bool comments_allowed,
		bool is_published,
		Project.Author author,
		string image,
		Dictionary<string, string> images,
		Project.ProjectHistory history,
		Project.ProjectStats stats,
		Project.ProjectRemix remix
	)
	{
		public record class Author
		(
			long id,
			string username,
			bool scratchteam,
			Dictionary<string, string> history,
			Profile profile
		);

		public record class Profile
		(
			Dictionary<string, string> images
		);

		public record class ProjectHistory
		(
			DateTime created,
			DateTime modified,
			DateTime? shared
		);

		public record class ProjectStats
		(
			long views,
			long loves,
			long favorites,
			long remixes
		);

		public record class ProjectRemix
		(
			string parent,
			string root
		);

		public Project(StudioProject project) : this(
			project.id,
			project.title,
			"",
			"",
			"visible",
			true,
			true,
			true,
			new Author
				(
					project.actor_id,
					"",
					false,
					new Dictionary<string, string>(),
					new Profile
						(
							new Dictionary<string, string>()
						)
				),
			project.image,
			new Dictionary<string, string>(),
			new ProjectHistory
				(
					DateTime.UnixEpoch,
					DateTime.UnixEpoch,
					DateTime.UnixEpoch
				),
			new ProjectStats
				(
					0,
					0,
					0,
					0
				),
			new ProjectRemix
				(
					"",
					""
				)

			) { }
	}
}
