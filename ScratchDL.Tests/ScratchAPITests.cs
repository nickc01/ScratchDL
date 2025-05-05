namespace ScratchDL.Tests
{
    public class ScratchAPITests
    {
        private DirectoryInfo tempFolder;

        public ScratchAPI API = ScratchAPI.Create();

        public ScratchAPITests()
        {
            tempFolder = new DirectoryInfo(Path.GetTempPath() + "/Scratch_API_TESTS");
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }
            tempFolder.Create();
        }

        private async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> values)
        {
            List<T> list = new List<T>();
            const int limit = 40;
            await foreach (T? item in values)
            {
                list.Add(item);
                if (list.Count >= limit)
                {
                    break;
                }
            }
            return list;
        }

        [Fact]
        public async Task GetWebsiteHealth_ShouldNotBeNull()
        {
            Assert.NotNull(await API.GetWebsiteHealth());
        }

        [Fact]
        public async Task GetNews_ShouldDownloadAtLeast40()
        {
            int newsRetrieved = 0;
            int counter = 0;
            await foreach (News newsItem in API.GetNews())
            {
                if (!string.IsNullOrEmpty(newsItem.headline))
                {
                    newsRetrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.Equal(20, newsRetrieved);
        }

        [Fact]
        public async Task DownloadFeatured_ShouldNotBeNull()
        {
            Assert.NotNull(await API.DownloadFeatured());
        }

        [Fact]
        public async Task CheckUsername_ShouldNotBeNull()
        {
            Assert.NotNull(await API.CheckUsername("mres"));
        }

        [Fact]
        public async Task CheckUsername_MresShouldAlreadyExist()
        {
            Assert.Equal(CheckUsernameResponse.AlreadyExists, await API.CheckUsername("mres"));
        }

        [Fact]
        public async Task CheckUsername_ShouldBeValid()
        {
            Assert.Equal(CheckUsernameResponse.Valid, await API.CheckUsername("ThisIsADemoUsername"));
        }

        [Fact]
        public async Task CheckUsername_ShouldBeInvalid()
        {
            Assert.Equal(CheckUsernameResponse.Invalid, await API.CheckUsername("ThisIsADemoUsername123"));
        }

        [Fact]
        public async Task CheckUsername_ShouldBeBadUsername()
        {
            Assert.Equal(CheckUsernameResponse.BadUsername, await API.CheckUsername("kill"));
        }

        [Fact]
        public async Task ExploreProjects_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (Project project in API.ExploreProjects("pacman"))
            {
                if (!string.IsNullOrEmpty(project.title))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.Equal(20, retrieved);
        }

        [Fact]
        public async Task ExploreStudios_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (Studio studio in API.ExploreStudios("mario"))
            {
                Assert.True(studio.id > 0);
                if (!string.IsNullOrEmpty(studio.title))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.Equal(20, retrieved);
        }

        [Fact]
        public async Task SearchProjects_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (Project project in API.SearchProjects("sonic"))
            {
                Assert.True(project.id > 0);
                if (!string.IsNullOrEmpty(project.title))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.Equal(20, retrieved);
        }

        [Fact]
        public async Task SearchStudios_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (Studio studio in API.SearchStudios("fun"))
            {
                Assert.True(studio.id > 0);
                if (!string.IsNullOrEmpty(studio.title))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.Equal(20, retrieved);
        }

        [Fact]
        public async Task GetProjectInfo_ShouldNotBeNull()
        {
            Assert.NotNull(await API.GetProjectInfo(797389150));
        }

        [Fact]
        public async Task GetProjectInfo_ShouldBeNull()
        {
            Assert.Null(await API.GetProjectInfo(999));
        }

        [Fact]
        public async Task GetProjectStudios_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (Studio item in API.GetProjectStudios("Ricky-Jan", 795072056))
            {
                Assert.True(item.id > 0);
                if (!(string.IsNullOrEmpty(item.title) && string.IsNullOrEmpty(item.description)))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.Equal(20, retrieved);
        }

        [Fact]
        public async Task GetProjectStudios_ShouldNotShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (Studio item in API.GetProjectStudios("Ricky-JanFake", 795072056))
            {
                Assert.True(item.id > 0);
                if (!(string.IsNullOrEmpty(item.title) && string.IsNullOrEmpty(item.description)))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.NotEqual(20, retrieved);
        }

        [Fact]
        public async Task GetProjectRemixes_ShouldNotBeNull()
        {
            long projectId = 60917032;

            List<Project> projectRemixes = await ToListAsync(API.GetProjectRemixes(projectId));

            Assert.NotEmpty(projectRemixes);
        }

        [Fact]
        public async Task GetProjectComments_ShouldNotBeNull()
        {

            string username = "griffpatch";
            long projectId = 60917032;


            List<Comment> projectComments = await ToListAsync(API.GetProjectComments(username, projectId));


            Assert.NotEmpty(projectComments);
        }

        [Fact]
        public async Task GetRepliesToComment_ShouldNotBeNull()
        {

            string username = "griffpatch";
            long projectId = 60917032;
            long commentId = 329901088;


            List<Comment> commentReplies = await ToListAsync(API.GetRepliesToComment(username, projectId, commentId));


            Assert.NotEmpty(commentReplies);
        }

        [Fact]
        public async Task GetProjectCommentInfo_ShouldNotBeNull()
        {
            Comment? commentInfo = await API.GetProjectCommentInfo("griffpatch", 60917032, 329901088);
            Assert.NotNull(commentInfo);
        }

        [Fact]
        public async Task GetStudioInfo_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            Studio? studio = await API.GetStudioInfo(studio_id);
            Assert.NotNull(studio);
        }

        [Fact]
        public async Task GetProjectsInStudio_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            List<StudioProject> projects = await ToListAsync(API.GetProjectsInStudio(studio_id));
            Assert.NotEmpty(projects);
            Assert.All(projects, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task DownloadStudioInfo_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            Studio? studio = await API.DownloadStudioInfo(studio_id);
            Assert.NotNull(studio);
        }

        [Fact]
        public async Task GetStudioManagers_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            List<User> managers = await ToListAsync(API.GetStudioManagers(studio_id));
            Assert.NotEmpty(managers);
            Assert.All(managers, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetStudioCurators_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            List<User> curators = await ToListAsync(API.GetStudioCurators(studio_id));
            Assert.NotEmpty(curators);
            Assert.All(curators, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetProjectRemixes_ShouldReturnSomeRemixes()
        {
            long project_id = 113321949;
            List<Project> remixes = await ToListAsync(API.GetProjectRemixes(project_id));

            Assert.True(remixes.Count > 0);
            Assert.All(remixes, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetProjectComments_ShouldReturnSomeComments()
        {
            string username = "griffpatch";
            long project_id = 60917032;
            List<Comment> comments = await ToListAsync(API.GetProjectComments(username, project_id));

            Assert.True(comments.Count > 0);
            Assert.All(comments, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetRepliesToComment_ShouldReturnSomeReplies()
        {
            string username = "griffpatch";
            long project_id = 60917032;
            long comment_id = 329994283;
            List<Comment> replies = await ToListAsync(API.GetRepliesToComment(username, project_id, comment_id));

            Assert.True(replies.Count > 0);
            Assert.All(replies, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetStudioComments_ShouldReturnSomeComments()
        {
            long studio_id = 32774157;
            List<Comment> comments = await ToListAsync(API.GetStudioComments(studio_id));

            Assert.True(comments.Count > 0);
            Assert.All(comments, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task DownloadStudioComment_ShouldReturnSomeComment()
        {
            long studio_id = 32774157;
            long comment_id = 220320340;
            Comment? comment = await API.DownloadStudioComment(studio_id, comment_id);

            Assert.NotNull(comment);
        }

        [Fact]
        public async Task DownloadStudioCommentReplies_ShouldReturnSomeReplies()
        {
            long studio_id = 32774157;
            long comment_id = 220320340;
            List<Comment> replies = await ToListAsync(API.DownloadStudioCommentReplies(studio_id, comment_id));

            Assert.True(replies.Count > 0);
            Assert.All(replies, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetStudioActivity_ShouldReturnSomeActivities()
        {
            long studio_id = 32774157;
            List<StudioStatus> activies = await ToListAsync(API.GetStudioActivity(studio_id, null));

            Assert.True(activies.Count > 0);
            Assert.All(activies, v => Assert.True(!string.IsNullOrEmpty(v.id)));
        }

        [Fact]
        public async Task GetStudioActivity_ShouldFilterActivitiesByDateLimit()
        {
            long studio_id = 55641;
            DateTime limit = DateTime.UtcNow.AddDays(-28);

            List<StudioStatus> activities = await ToListAsync(API.GetStudioActivity(studio_id, limit));

            Assert.NotEmpty(activities);
            Assert.All(activities, a => Assert.True(!string.IsNullOrEmpty(a.id) && a.datetime_created < limit));
        }

        [Fact]
        public async Task GetUserInfo_ShouldNotBeNull()
        {
            string username = "scratchU8";


            User? result = await API.GetUserInfo(username);


            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPublishedProjects_ShouldNotBeNull()
        {
            string username = "scratchU8";


            List<Project> result = await ToListAsync(API.GetPublishedProjects(username));


            Assert.True(result.Count > 0);
            Assert.All(result, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetCuratedStudios_ShouldNotBeNull()
        {
            string username = "scratchU8";


            List<Studio> result = await ToListAsync(API.GetCuratedStudios(username));


            Assert.True(result.Count > 0);
            Assert.All(result, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetFavoriteProjects_ShouldNotBeNull()
        {
            string username = "scratchU8";


            List<Project> result = await ToListAsync(API.GetFavoriteProjects(username));


            Assert.True(result.Count > 0);
            Assert.All(result, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetUserMessageCount_ShouldReturnValidData()
        {
            const string username = "Griffpatch";


            long? count = await API.GetUserMessageCount(username);


            Assert.NotNull(count);
            Assert.True(count.Value > 0);
        }

        [Fact]
        public async Task DownloadProfileImage_ShouldReturnValidData()
        {
            const string username = "Griffpatch";


            Stream? stream = await API.DownloadProfileImage(username);


            Assert.NotNull(stream);
            Assert.True(stream!.Length > 0);
        }

        [Fact]
        public async Task DownloadProfileImage_WithUserID_ShouldReturnValidData()
        {
            const long userId = 1;


            Stream stream = await API.DownloadProfileImage(userId);


            Assert.NotNull(stream);
            Assert.True(stream!.Length > 0);
        }

        [Fact]
        public async Task GetFollowers_ShouldReturnValidData()
        {
            const string username = "griffpatch";


            List<User> followers = await ToListAsync(API.GetFollowers(username));


            Assert.NotEmpty(followers);
            Assert.All(followers, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task GetFollowingUsers_ShouldNotBeNull()
        {
            string username = "griffpatch";


            List<User> followingUsers = await ToListAsync(API.GetFollowingUsers(username));

            Assert.NotEmpty(followingUsers);
            Assert.All(followingUsers, v => Assert.True(v.id > 0));
        }

        [Fact]
        public async Task DownloadAndExportProject_ByProjectId_ShouldReturnOutputDirectory()
        {

            long projectId = 788908450;
            DirectoryInfo outputDirectory = new DirectoryInfo(Path.Combine(tempFolder.FullName, "output1"));


            DirectoryInfo result = await API.DownloadAndExportProject(projectId, outputDirectory);


            Assert.NotNull(result);
            Assert.True(result.Exists);
        }

        [Fact]
        public async Task DownloadAndExportProject_ByProjectInfo_ShouldReturnOutputDirectory()
        {

            Project? projectInfo = await API.GetProjectInfo(788908450);
            DirectoryInfo outputDirectory = new DirectoryInfo(Path.Combine(tempFolder.FullName, "output2"));

            Assert.NotNull(projectInfo);

            DirectoryInfo result = await API.DownloadAndExportProject(projectInfo, outputDirectory);


            Assert.NotNull(result);
            Assert.True(result.Exists);
        }

        [Fact]
        public async Task DownloadAndExportProject_NonExistentProject_ShouldThrowProjectDownloadException()
        {

            long projectId = -1;
            DirectoryInfo outputDirectory = new DirectoryInfo(Path.Combine(tempFolder.FullName, "output3"));

            await Assert.ThrowsAsync<ProjectDownloadException>(() => API.DownloadAndExportProject(projectId, outputDirectory));
        }

        [Fact]
        public async Task DownloadAndExportProject_PrivateProject_ShouldThrowProjectDownloadException()
        {

            long projectId = 100;
            DirectoryInfo outputDirectory = new DirectoryInfo(Path.Combine(tempFolder.FullName, "output4"));

            await Assert.ThrowsAsync<ProjectDownloadException>(() => API.DownloadAndExportProject(projectId, outputDirectory));
        }

        [Fact]
        public async Task DownloadProject_ByProjectInfo_ShouldNotBeNull()
        {
            Project? projectInfo = await API.GetProjectInfo(10128407);

            Assert.NotNull(projectInfo);

            DownloadedProject downloadedProject = await API.DownloadProject(projectInfo);

            Assert.NotNull(downloadedProject);
        }

        [Fact]
        public async Task DownloadProject_ByProjectId_ShouldNotBeNull()
        {
            DownloadedProject downloadedProject = await API.DownloadProject(612229554);

            Assert.NotNull(downloadedProject);
        }

        [Fact]
        public async Task DownloadProject_ByProjectInfo_ShouldSaveToDisk()
        {
            Project? projectInfo = await API.GetProjectInfo(10128407);

            Assert.NotNull(projectInfo);

            DownloadedProject downloadedProject = await API.DownloadProject(projectInfo);

            string filePath = Path.Combine(tempFolder.FullName, $"{projectInfo.title}.sb2");

            await downloadedProject.ExportProject(tempFolder, $"{projectInfo.title}");

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task DownloadProject_ByProjectId_ShouldSaveToDisk()
        {
            const int ID = 612229554;

            DownloadedProject downloadedProject = await API.DownloadProject(ID);

            string filePath = Path.Combine(tempFolder.FullName, $"{ID}.sb2");

            await downloadedProject.ExportProject(tempFolder, $"{ID}");

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task DownloadProject_NonExistentProjectId_ShouldThrowException()
        {
            await Assert.ThrowsAsync<ProjectDownloadException>(async () => await API.DownloadProject(999999999));
        }

        [Fact]
        public async Task DownloadProject_PrivateProjectId_ShouldThrowException()
        {
            await Assert.ThrowsAsync<ProjectDownloadException>(async () => await API.DownloadProject(100));
        }

        [Fact]
        public async Task DownloadFromURL_ReturnsStream()
        {

            string url = "https://api.scratch.mit.edu/status";


            Stream stream = await API.DownloadFromURL(url);


            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task DownloadProjectImage_WithProjectID_ReturnsStream()
        {

            long projectId = 522557780;


            Stream? stream = await API.DownloadProjectImage(projectId);


            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task DownloadProjectImage_WithProjectInfo_ReturnsStream()
        {

            long projectId = 823872487;
            Project? info = await API.GetProjectInfo(projectId);

            Assert.NotNull(info);

            Stream? stream = await API.DownloadProjectImage(info);


            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }
    }
}
