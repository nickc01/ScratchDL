using System.Diagnostics;
using Xunit;

namespace ScratchDL.Tests
{
    public class ScratchAPITests
    {

        DirectoryInfo tempFolder;

        public ScratchAPI API = ScratchAPI.Create();

        public ScratchAPITests() {
            tempFolder = new DirectoryInfo(Path.GetTempPath() + "/Scratch_API_TESTS");
            if (tempFolder.Exists) {
                tempFolder.Delete(true);
            }
            tempFolder.Create();
        }


        async Task<int> CountAsync<T>(IAsyncEnumerable<T> values) {
            int counter = 0;
            const int limit = 40;
            await foreach (var item in values)
            {
                counter++;
                if (counter >= limit) {
                    break;
                }
            }
            return counter;
        }

        async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> values) {
            var list = new List<T>();
            const int limit = 40;
            await foreach (var item in values)
            {
                list.Add(item);
                if (list.Count >= limit) {
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
            await foreach (var newsItem in API.GetNews())
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
            Assert.Equal(20,newsRetrieved);
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
            Assert.Equal(CheckUsernameResponse.AlreadyExists,await API.CheckUsername("mres"));
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
            Assert.Equal(CheckUsernameResponse.BadUsername,await API.CheckUsername("kill"));
        }

        [Fact]
        public async Task ExploreProjects_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (var project in API.ExploreProjects("pacman"))
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
            await foreach (var studio in API.ExploreStudios("mario"))
            {
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
            await foreach (var project in API.SearchProjects("sonic"))
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
        public async Task SearchStudios_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (var studio in API.SearchStudios("fun"))
            {
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
            await foreach (var item in API.GetProjectStudios("Ricky-Jan", 795072056))
            {
                if (!string.IsNullOrEmpty(item.title))
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
            await foreach (var item in API.GetProjectStudios("Ricky-JanFake", 795072056))
            {
                if (!string.IsNullOrEmpty(item.title))
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
            // Arrange
            long projectId = 60917032;

            // Act
            var projectRemixes = API.GetProjectRemixes(projectId);

            // Assert
            await foreach (var remix in projectRemixes)
            {
                Assert.NotNull(remix);
            }
        }

        [Fact]
        public async Task GetProjectComments_ShouldNotBeNull()
        {
            // Arrange
            string username = "griffpatch";
            long projectId = 60917032;

            // Act
            var projectComments = API.GetProjectComments(username, projectId);

            // Assert
            await foreach (var comment in projectComments)
            {
                Assert.NotNull(comment);
            }
        }

        [Fact]
        public async Task GetRepliesToComment_ShouldNotBeNull()
        {
            // Arrange
            string username = "griffpatch";
            long projectId = 60917032;
            long commentId = 329901088;

            // Act
            var commentReplies = API.GetRepliesToComment(username, projectId, commentId);

            // Assert
            await foreach (var reply in commentReplies)
            {
                Assert.NotNull(reply);
            }
        }

        [Fact]
        public async Task GetProjectCommentInfo_ShouldNotBeNull()
        {
            var commentInfo = await API.GetProjectCommentInfo("griffpatch", 60917032, 329901088);
            Assert.NotNull(commentInfo);
        }

        /*[Fact]
        public async Task DownloadProjectCount_ShouldNotBeNull()
        {
            var projectCount = await API.DownloadProjectCount("griffpatch");
            Assert.NotNull(projectCount);
        }*/

        /*[Fact]
        public async Task GetAllProjectCount_ShouldNotBeNull()
        {
            var allProjectCount = await API.GetAllProjectCount();
            Assert.NotNull(allProjectCount);
        }*/

        [Fact]
        public async Task GetStudioInfo_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            var studio = await API.GetStudioInfo(studio_id);
            Assert.NotNull(studio);
        }

        [Fact]
        public async Task GetProjectsInStudio_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            var projects = API.GetProjectsInStudio(studio_id);
            await foreach (var project in projects)
            {
                Assert.NotNull(project);
            }
        }

        [Fact]
        public async Task DownloadStudioInfo_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            var studio = await API.DownloadStudioInfo(studio_id);
            Assert.NotNull(studio);
        }

        [Fact]
        public async Task GetStudioManagers_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            var managers = API.GetStudioManagers(studio_id);
            await foreach (var manager in managers)
            {
                Assert.NotNull(manager);
            }
        }

        [Fact]
        public async Task GetStudioCurators_ShouldNotBeNull()
        {
            long studio_id = 32774157;
            var curators = API.GetStudioCurators(studio_id);
            await foreach (var curator in curators)
            {
                Assert.NotNull(curator);
            }
        }

        [Fact]
        public async Task GetProjectRemixes_ShouldReturnSomeRemixes()
        {
            long project_id = 113321949;
            int remixCount = await CountAsync(API.GetProjectRemixes(project_id));

            Assert.True(remixCount > 0);
        }

        [Fact]
        public async Task GetProjectComments_ShouldReturnSomeComments()
        {
            string username = "griffpatch";
            long project_id = 60917032;
            int commentCount = await CountAsync(API.GetProjectComments(username, project_id));

            Assert.True(commentCount > 0);
        }

        [Fact]
        public async Task GetRepliesToComment_ShouldReturnSomeReplies()
        {
            string username = "griffpatch";
            long project_id = 60917032;
            long comment_id = 8865580;
            int replyCount = await CountAsync(API.GetRepliesToComment(username, project_id, comment_id));

            Assert.True(replyCount > 0);
        }

        [Fact]
        public async Task GetStudioComments_ShouldReturnSomeComments()
        {
            long studio_id = 15847014; // Game Builders Studio
            int commentCount = await CountAsync(API.GetStudioComments(studio_id));

            Assert.True(commentCount > 0);
        }

        [Fact]
        public async Task DownloadStudioComment_ShouldReturnSomeComment()
        {
            long studio_id = 15847014; // Game Builders Studio
            long comment_id = 6121906;
            Comment? comment = await API.DownloadStudioComment(studio_id, comment_id);

            Assert.NotNull(comment);
        }

        [Fact]
        public async Task DownloadStudioCommentReplies_ShouldReturnSomeReplies()
        {
            long studio_id = 15847014; // Game Builders Studio
            long comment_id = 6121906;
            int replyCount = await CountAsync(API.DownloadStudioCommentReplies(studio_id, comment_id));

            Assert.True(replyCount > 0);
        }

        [Fact]
        public async Task GetStudioActivity_ShouldReturnSomeActivities()
        {
            long studio_id = 15847014; // Game Builders Studio
            int activityCount = await CountAsync(API.GetStudioActivity(studio_id, null));

            Assert.True(activityCount > 0);
        }

        [Fact]
        public async Task GetStudioActivity_ShouldFilterActivitiesByDateLimit()
        {
            long studio_id = 15847014; // Game Builders Studio
            var limit = new System.DateTime(2022, 1, 1);
            List<StudioStatus> activities = new List<StudioStatus>();

            await foreach (var activity in API.GetStudioActivity(studio_id, limit)) {
                activities.Add(activity);
            }

            Assert.All(activities, a => Assert.True(a.datetime_created < limit));
        }

        [Fact]
        public async Task GetUserInfo_ShouldNotBeNull()
        {
            // Arrange
            string username = "someuser";

            // Act
            var result = await API.GetUserInfo(username);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPublishedProjects_ShouldNotBeNull()
        {
            // Arrange
            string username = "someuser";

            // Act
            var result = await ToListAsync(API.GetPublishedProjects(username));

            // Assert
            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetCuratedStudios_ShouldNotBeNull()
        {
            // Arrange
            string username = "someuser";

            // Act
            var result = await ToListAsync(API.GetCuratedStudios(username));

            // Assert
            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetFavoriteProjects_ShouldNotBeNull()
        {
            // Arrange
            string username = "someuser";

            // Act
            var result = await ToListAsync(API.GetFavoriteProjects(username));

            // Assert
            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetUserMessageCount_ShouldReturnValidData()
        {
            // Arrange
            const string username = "Griffpatch"; // Set a valid username to test with

            // Act
            var count = await API.GetUserMessageCount(username);

            // Assert
            Assert.NotNull(count);
            Assert.True(count.Value > 0);
        }

        [Fact]
        public async Task DownloadProfileImage_ShouldReturnValidData()
        {
            // Arrange
            const string username = "Griffpatch"; // Set a valid username to test with

            // Act
            var stream = await API.DownloadProfileImage(username);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream!.Length > 0);
        }

        [Fact]
        public async Task DownloadProfileImage_WithUserID_ShouldReturnValidData()
        {
            // Arrange
            const long userId = 1; // Set a valid user id to test with

            // Act
            var stream = await API.DownloadProfileImage(userId);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream!.Length > 0);
        }

        [Fact]
        public async Task GetFollowers_ShouldReturnValidData()
        {
            // Arrange
            const string username = "Griffpatch"; // Set a valid username to test with
            var followers = new List<User>();

            // Act
            await foreach (var follower in API.GetFollowers(username))
            {
                followers.Add(follower);
            }

            // Assert
            Assert.NotEmpty(followers);
        }

        [Fact]
        public async Task GetFollowingUsers_ShouldNotBeNull()
        {
            // Arrange
            string username = "ScratchUser";

            // Act
            var followingUsers = API.GetFollowingUsers(username);

            // Assert
            await foreach (var user in followingUsers)
            {
                Assert.NotNull(user);
            }
        }

        [Fact]
        public async Task DownloadAndExportProject_ByProjectId_ShouldReturnOutputDirectory()
        {
            // Arrange
            long projectId = 123456;
            DirectoryInfo outputDirectory = new DirectoryInfo("output");

            // Act
            var result = await API.DownloadAndExportProject(projectId, outputDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Exists);
            Assert.Equal(outputDirectory.FullName, result.FullName);
        }

        [Fact]
        public async Task DownloadAndExportProject_ByProjectInfo_ShouldReturnOutputDirectory()
        {
            // Arrange
            var projectInfo = await API.GetProjectInfo(123456);
            DirectoryInfo outputDirectory = new DirectoryInfo("output");

            // Act
            var result = await API.DownloadAndExportProject(projectInfo, outputDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Exists);
            Assert.Equal(outputDirectory.FullName, result.FullName);
        }

        [Fact]
        public async Task DownloadAndExportProject_NonExistentProject_ShouldThrowProjectDownloadException()
        {
            // Arrange
            long projectId = -1;
            DirectoryInfo outputDirectory = new DirectoryInfo("output");

            // Act & Assert
            await Assert.ThrowsAsync<ProjectDownloadException>(() => API.DownloadAndExportProject(projectId, outputDirectory));
        }

        [Fact]
        public async Task DownloadAndExportProject_PrivateProject_ShouldThrowProjectDownloadException()
        {
            // Arrange
            long projectId = 1111; // A private project ID, assuming that it remains private
            DirectoryInfo outputDirectory = new DirectoryInfo("output");

            // Act & Assert
            await Assert.ThrowsAsync<ProjectDownloadException>(() => API.DownloadAndExportProject(projectId, outputDirectory));
        }

        [Fact]
        public async Task DownloadProject_ByProjectInfo_ShouldNotBeNull()
        {
            var projectInfo = await API.GetProjectInfo(216338743);

            var downloadedProject = await API.DownloadProject(projectInfo);

            Assert.NotNull(downloadedProject);
        }

        [Fact]
        public async Task DownloadProject_ByProjectId_ShouldNotBeNull()
        {
            var downloadedProject = await API.DownloadProject(216338743);

            Assert.NotNull(downloadedProject);
        }

        [Fact]
        public async Task DownloadProject_ByProjectInfo_ShouldSaveToDisk()
        {
            var projectInfo = await API.GetProjectInfo(216338743);

            var downloadedProject = await API.DownloadProject(projectInfo);

            var filePath = Path.Combine(tempFolder.FullName, $"{projectInfo.title}.sb3");

            //File.WriteAllBytes(filePath, downloadedProject.Bytes);
            await downloadedProject.ExportProject(tempFolder,$"{projectInfo.title}.sb3");

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task DownloadProject_ByProjectId_ShouldSaveToDisk()
        {
            const int ID = 216338743;

            var downloadedProject = await API.DownloadProject(ID);

            var filePath = Path.Combine(tempFolder.FullName, $"{ID}.sb3");

            //File.WriteAllBytes(filePath, downloadedProject.Bytes);
            await downloadedProject.ExportProject(tempFolder,$"{ID}.sb3");

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
            await Assert.ThrowsAsync<ProjectDownloadException>(async () => await API.DownloadProject(145517216));
        }

        [Fact]
        public async Task DownloadFromURL_ReturnsStream()
        {
            // Arrange
            string url = "https://api.scratch.mit.edu/status";

            // Act
            var stream = await API.DownloadFromURL(url);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task DownloadProjectImage_WithProjectID_ReturnsStream()
        {
            // Arrange
            long projectId = 484870012; // Replace with an actual project ID that has an image

            // Act
            var stream = await API.DownloadProjectImage(projectId);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task DownloadProjectImage_WithProjectInfo_ReturnsStream()
        {
            // Arrange
            long projectId = 484870012; // Replace with an actual project ID that has an image
            var info = await API.GetProjectInfo(projectId);

            // Act
            var stream = await API.DownloadProjectImage(info);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }



        /*[Fact]
        public async Task GetProjectStudios_ShouldShowResults()
        {
            int retrieved = 0;
            int counter = 0;
            await foreach (var item in API.GetProjectStudios("Ricky-Jan", 795072056))
            {
                if (!string.IsNullOrEmpty(item.title))
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
            await foreach (var item in API.GetProjectStudios("Ricky-JanFake", 795072056))
            {
                if (!string.IsNullOrEmpty(item.title))
                {
                    retrieved++;
                }
                if (++counter >= 20)
                {
                    break;
                }
            }
            Assert.NotEqual(20, retrieved);
        }*/
    }
}