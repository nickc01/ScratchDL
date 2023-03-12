﻿using System.Text.Json.Serialization;

namespace ScratchDL
{
    static class _container_
    {
        public static readonly Dictionary<string, string> emptyDictionary = new Dictionary<string, string>();
    }


    public record class Project(
        long id,
        string title,
        string description,
        string instructions,
        string visibility,
        [property:JsonPropertyName("public")]
        bool is_public,
        bool comments_allowed,
        bool is_published,
        Project.Author author,
        string image,
        Dictionary<string, string> images,
        Project.ProjectHistory history,
        Project.ProjectStats stats,
        Project.ProjectRemix remix,
        string project_token
    )
    {
        public Project() : this(0, string.Empty, string.Empty, string.Empty, string.Empty, false, false, false, new Author(0, string.Empty, false, _container_.emptyDictionary, new Profile(_container_.emptyDictionary)), string.Empty, _container_.emptyDictionary, new ProjectHistory(DateTime.Now, DateTime.Now, DateTime.Now), new ProjectStats(0, 0, 0, 0), new ProjectRemix(null, null), string.Empty)
        {

        }

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
            long? parent,
            long? root
        );
    }
}