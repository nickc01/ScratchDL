using System.Diagnostics;
using Xunit;

namespace ScratchDL.Tests
{
    public class ScratchAPITests
    {
        public ScratchAPI API = ScratchAPI.Create();

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
    }
}