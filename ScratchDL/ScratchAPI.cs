using ScratchDL;
using ScratchDL.Featured;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ScratchDL
{
    record class LoginRequest(
        string username,
        string password,
        string csrftoken,
        string csrfmiddlewaretoken,
        string captcha_challenge = "",
        string captcha_response = "",
        bool useMessages = true,
        string timezone = "America/New_York"
    );

    /// <summary>
    /// Used for downloading and accessing various data from the Scratch website
    /// </summary>
    public sealed class ScratchAPI : IDisposable
    {
        static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions() { WriteIndented = true };

        static byte[]? projectTemplate;

        HttpClient client;
        string? csrfToken;
        Dictionary<string, string> cookies = new Dictionary<string, string>();
        StringBuilder cookieString = new StringBuilder();

        /// <summary>
        /// Is the API currently logged into a user?
        /// </summary>
        public bool LoggedIn => ProfileLoginInfo != null;

        /// <summary>
        /// The currently logged in user information, or null if the API is currently not logged in
        /// </summary>
        public LoginInfo? ProfileLoginInfo { get; private set; }

        private ScratchAPI()
        {
            client = new HttpClient(new HttpClientHandler { UseCookies = false });
            client.DefaultRequestHeaders.Referrer = new Uri("https://scratch.mit.edu");
        }

        /// <summary>
        /// Creates a new Scratch API Accessor
        /// </summary>
        public static ScratchAPI Create()
        {
            return new ScratchAPI();
        }

        /// <summary>
        /// Creates a new Scratch API Accessor and Logs into an existing account
        /// </summary>
        /// <param name="username">The username of the account to login to</param>
        /// <param name="password">The password of the account to login to</param>
        /// <exception cref="NoInternetException">Throws if there is no internet connection</exception>
        /// <exception cref="LoginException">Throws if there was a failure logging in</exception>
        public static async Task<ScratchAPI> Create(string username, string password)
        {
            var accessor = new ScratchAPI();

            await accessor.Login(username, password);

            return accessor;
        }

        /// <summary>
        /// Logs into an existing account
        /// </summary>
        /// <param name="username">The username of the account to login to</param>
        /// <param name="password">The password of the account to login to</param>
        /// <returns></returns>
        /// <exception cref="NoInternetException">Throws if there is no internet connection</exception>
        /// <exception cref="LoginException">Throws if there was a failure logging in</exception>
        public async Task Login(string username, string password)
        {
            if (client == null)
            {
                return;
            }
            if (LoggedIn)
            {
                return;
            }
            using (var csrfContent = await DownloadData("https://scratch.mit.edu/csrf_token/"))
            {
                if (csrfContent == null || !csrfContent.IsSuccessStatusCode)
                {
                    throw new NoInternetException();
                }

                if (csrfContent.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    foreach (var cookie in SplitCookies(cookies))
                    {
                        if (cookie.Key == "scratchcsrftoken")
                        {
                            csrfToken = cookie.Value;
                        }
                    }
                }
            }

            try
            {
                AddCookie("scratchlanguage", "en");
                RemoveCookie("scratchsessionsid");

                var login = new LoginRequest(username, password, csrfToken!, csrfToken!);

                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("X-CSRFToken", csrfToken);

                using (var loginContent = await SendPostMessage("https://scratch.mit.edu/login/", JsonSerializer.Serialize(login, DefaultOptions)))
                {
                    if (!loginContent.IsSuccessStatusCode)
                    {
                        throw new LoginException(loginContent.StatusCode, await loginContent.Content.ReadAsStringAsync());
                    }
                    foreach (var cookie in SplitCookies(loginContent.Headers.GetValues("Set-Cookie")))
                    {
                        if (cookie.Key == "scratchsessionsid")
                        {
                            AddCookie(cookie.Key, cookie.Value);
                        }
                        if (cookie.Key == "scratchcsrftoken")
                        {
                            RemoveCookie(cookie.Key);
                            csrfToken = cookie.Value;
                            AddCookie(cookie.Key, cookie.Value);
                        }
                    }
                }

                client.DefaultRequestHeaders.Remove("X-CSRFToken");
                client.DefaultRequestHeaders.Add("X-CSRFToken", csrfToken);

                ProfileLoginInfo = await DownloadData<LoginInfo>("https://scratch.mit.edu/session/");

                if (ProfileLoginInfo == null)
                {
                    throw new NoInternetException();
                }

                client.DefaultRequestHeaders.Add("X-Token", ProfileLoginInfo.user.token);
                AddCookie("permissions", JsonSerializer.Serialize(ProfileLoginInfo.permissions, DefaultOptions));
            }
            catch (Exception)
            {
                client.DefaultRequestHeaders.Remove("X-CSRFToken");
                client.DefaultRequestHeaders.Remove("X-Token");
                client.DefaultRequestHeaders.Remove("X-Requested-With");
                ClearAllCookies();
                throw;
            }
        }

        /// <summary>
        /// Logs out of the currently logged in user
        /// </summary>
        public void Logout()
        {
            if (!LoggedIn)
            {
                return;
            }
            ProfileLoginInfo = null;
            client.DefaultRequestHeaders.Remove("X-CSRFToken");
            client.DefaultRequestHeaders.Remove("X-Token");
            client.DefaultRequestHeaders.Remove("X-Requested-With");
            ClearAllCookies();
        }

        /// <summary>
        /// Downloads data from the url
        /// </summary>
        /// <param name="url">The url to download from</param>
        /// <returns>A response message containing the downloaded data, or null if the data couldn't be downloaded</returns>
        async Task<HttpResponseMessage?> DownloadData(string url)
        {
            if (client == null)
            {
                return null;
            }
            const int RETRY_COUNT = 3;
            HttpResponseMessage? response = null;
            for (int i = 0; i < RETRY_COUNT; i++)
            {
                try
                {
                    response = await client.GetAsync(url);
                    break;
                }
                catch (Exception)
                {
                    if (i == RETRY_COUNT - 1)
                    {
                        throw;
                    }
                }
            }
            if (response == null || !response.IsSuccessStatusCode)
            {
                return null;
            }

            ProcessNewCookies(response);

            return response;
        }

        /// <summary>
        /// Downloads data from the URL
        /// </summary>
        /// <typeparam name="T">The type of the data to download</typeparam>
        /// <param name="url">The url to download from</param>
        /// <returns>Returns the downloaded data, or null if the data could not be downloaded</returns>
        async Task<T?> DownloadData<T>(string url) where T : class
        {
            using (var response = await DownloadData(url))
            {
                if (response != null && response.IsSuccessStatusCode == true)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<T>(str, DefaultOptions);
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Sends a post message to a URL
        /// </summary>
        /// <param name="url">The url to send to</param>
        /// <param name="data">The data to send</param>
        /// <returns>Returns an HTTP response from the post</returns>
        async Task<HttpResponseMessage> SendPostMessage(string url, string data)
        {
            var response = await client!.PostAsync(url, new StringContent(data));

            ProcessNewCookies(response);

            return response;
        }

        /// <summary>
        /// Adds a new cookie that has been received from an HTTP response
        /// </summary>
        /// <param name="response">The response to look over</param>
        void ProcessNewCookies(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                foreach (var cookie in values)
                {
                    var (key, value) = SplitCookie(cookie);
                    AddCookie(key, value);
                }
            }
        }


        #region LOGGED IN USER ONLY DOWNLOADERS

        /// <summary>
        /// Iterates over all the projects in the logged in user's gallery
        /// </summary>
        public IAsyncEnumerable<GalleryProject> GetAllProjectsByCurrentUser()
        {
            if (!LoggedIn)
            {
                throw new Exception($"Can't call {nameof(GetAllProjectsByCurrentUser)} without logging in first");
            }
            return DownloadList<GalleryProject>("https://scratch.mit.edu/site-api/projects/all/", 40, true);
        }

        /// <summary>
        /// Iterates over all the studios made by the logged in user
        /// </summary>
        public IAsyncEnumerable<UserStudio> GetAllStudiosByCurrentUser()
        {
            if (!LoggedIn)
            {
                throw new Exception($"Can't call {nameof(GetAllStudiosByCurrentUser)} without logging in first");
            }
            return DownloadList<UserStudio>("https://scratch.mit.edu/site-api/galleries/all/", 40, true);
        }

        /// <summary>
        /// Iterates over all the projects in a studio made by the logged in user
        /// </summary>
        /// <param name="studio">The studio to search for projects</param>
        public IAsyncEnumerable<StudioProject> GetProjectsInStudioByCurrentUser(UserStudio studio)
        {
            if (!LoggedIn)
            {
                throw new Exception($"Can't call {nameof(GetProjectsInStudioByCurrentUser)} without logging in first");
            }
            return GetProjectsInStudio(studio.id);
        }

        /// <summary>
        /// Gets information about a project made by the logged in user. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="project">The project to get more information about</param>
        public Task<Project?> GetInfoOfProjectByCurrentUser(GalleryProject project)
        {
            if (!LoggedIn)
            {
                throw new Exception($"Can't call {nameof(GetInfoOfProjectByCurrentUser)} without logging in first");
            }
            return GetProjectInfo(project.id);
        }

        #endregion

        #region GENERAL WEBSITE DOWNLOADERS

        /// <summary>
        /// Gets health information about the website. Returns null if the data couldn't be downloaded
        /// </summary>
        public Task<Health?> GetWebsiteHealth()
        {
            return DownloadData<Health>("https://api.scratch.mit.edu/health");
        }

        /// <summary>
        /// Gets the latest website news. Returns null if the data couldn't be downloaded
        /// </summary>
        public IAsyncEnumerable<News> GetNews()
        {
            return DownloadList<News>("https://api.scratch.mit.edu/news", 40);
        }

        /// <summary>
        /// Gets all information about featured projects. Returns null if the data couldn't be retrieved
        /// </summary>
        public Task<FeaturedData?> DownloadFeatured()
        {
            return DownloadData<FeaturedData>("https://api.scratch.mit.edu/proxy/featured");
        }

        /// <summary>
        /// Checks if a username is available. Returns null if the data couldn't be downloaded
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns></returns>
        public async Task<CheckUsernameResponse?> CheckUsername(string username)
        {
            var result = await DownloadData($"https://api.scratch.mit.edu/accounts/checkusername/{username}");
            if (result != null)
            {
                //var obj = JObject.Parse(await result.Content.ReadAsStringAsync());
                using var obj = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());
                //obj.RootElement.GetProperty("Students")
                if (obj.RootElement.TryGetProperty("msg", out var msgObj))
                {
                    var message = msgObj.GetString();
                    //var message = obj["msg"]?.Value<string>();
                    if (message == "valid username")
                    {
                        return CheckUsernameResponse.Valid;
                    }
                    else if (message == "username exists")
                    {
                        return CheckUsernameResponse.AlreadyExists;
                    }
                    else if (message == "bad username")
                    {
                        return CheckUsernameResponse.BadUsername;
                    }
                    else
                    {
                        return CheckUsernameResponse.Invalid;
                    }
                }
                return CheckUsernameResponse.Invalid;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Uses scratch's "Explore" feature to explore for certain types of projects
        /// </summary>
        /// <param name="keyword">The keyword to search for. Valid keywords are "all, animations, art, games, music, stories, tutorials"</param>
        /// <param name="searchMode">How the keywords should be searched. Valid modes are "trending, popular, recent"</param>
        /// <param name="language">The language the results should be in</param>
        public IAsyncEnumerable<Project> ExploreProjects(string keyword = "all", SearchMode searchMode = SearchMode.Popular, string language = "en")
        {
            if (keyword == "all")
            {
                keyword = "*";
            }

            return DownloadList<Project>($"https://api.scratch.mit.edu/explore/projects?language={language.ToLower()}&mode={searchMode.ToString().ToLower()}&q={keyword.ToLower()}", 40);
        }

        /// <summary>
        /// Uses scratch's "Explore" feature to explore for certain types of studios
        /// </summary>
        /// <param name="keyword">The keyword to search for. Valid keywords are "all, animations, art, games, music, stories, tutorials"</param>
        /// <param name="searchMode">How the keywords should be searched. Valid modes are "trending, popular, recent"</param>
        /// <param name="language">The language the results should be in</param>
        public IAsyncEnumerable<Studio> ExploreStudios(string keyword = "all", SearchMode searchMode = SearchMode.Popular, string language = "en")
        {
            if (keyword == "all")
            {
                keyword = "*";
            }

            return DownloadList<Studio>($"https://api.scratch.mit.edu/explore/studios?language={language.ToLower()}&mode={searchMode.ToString().ToLower()}&q={keyword.ToLower()}", 40);
        }

        /// <summary>
        /// Uses scratch's "Search" feature to search for projects
        /// </summary>
        /// <param name="searchTerm">The keyword to search for</param>
        /// <param name="searchMode">How the keywords should be searched. Valid modes are "trending, popular, recent"</param>
        /// <param name="language">The language the results should be in</param>
        public IAsyncEnumerable<Project> SearchProjects(string searchTerm, SearchMode searchMode = SearchMode.Popular, string language = "en")
        {
            return DownloadList<Project>($"https://api.scratch.mit.edu/search/projects?language={language.ToLower()}&mode={searchMode.ToString().ToLower()}&q={searchTerm}", 40);
        }

        /// <summary>
        /// Uses scratch's "Search" feature to search for certain types of studios
        /// </summary>
        /// <param name="searchTerm">The keyword to search for</param>
        /// <param name="searchMode">How the keywords should be searched. Valid modes are "trending, popular, recent"</param>
        /// <param name="language">The language the results should be in</param>
        public IAsyncEnumerable<Studio> SearchStudios(string searchTerm, SearchMode searchMode = SearchMode.Popular, string language = "en")
        {
            return DownloadList<Studio>($"https://api.scratch.mit.edu/search/studios?language={language.ToLower()}&mode={searchMode.ToString().ToLower()}&q={searchTerm}", 40);
        }

        /// <summary>
        /// Gets the total amount of projects that have been uploaded to scratch. Returns null if the data couldn't be retrieved
        /// </summary>
        public async Task<long?> GetEntireSiteProjectCount()
        {
            var result = await DownloadData("https://api.scratch.mit.edu/projects/count/all");
            if (result == null)
            {
                return null;
            }
            using var obj = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());

            if (obj.RootElement.TryGetProperty("count", out var countObj) && countObj.TryGetInt64(out var count))
            {
                return count;
            }
            return null;

            //var obj = JObject.Parse(await result.Content.ReadAsStringAsync());
            //return obj["count"]?.Value<long>();
        }

        #endregion

        #region PROJECT DOWNLOADERS

        /// <summary>
        /// Gets information about a project. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="project_id">The id of the project to get information about</param>
        public Task<Project?> GetProjectInfo(long project_id)
        {
            return DownloadData<Project>($"https://api.scratch.mit.edu/projects/{project_id}");
        }

        /// <summary>
        /// Gets a list of studios a certain project is added to
        /// </summary>
        /// <param name="username">The user who made the project</param>
        /// <param name="project_id">The project to check for</param>
        public IAsyncEnumerable<Studio> GetProjectStudios(string username, long project_id)
        {
            return DownloadList<Studio>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/studios", 40);
        }

        /// <summary>
        /// Gets all remixes for a certain project
        /// </summary>
        /// <param name="project_id">The project to check for</param>
        public IAsyncEnumerable<Project> GetProjectRemixes(long project_id)
        {
            return DownloadList<Project>($"https://api.scratch.mit.edu/projects/{project_id}/remixes", 40);
        }

        /// <summary>
        /// Iterates over all the comments on a project
        /// </summary>
        /// <param name="username">The username who made the project</param>
        /// <param name="project_id">The project to check for</param>
        public IAsyncEnumerable<Comment> GetProjectComments(string username, long project_id)
        {
            return DownloadList<Comment>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/comments", 40);
        }

        /// <summary>
        /// Iterates over all the replies of a certain project comment
        /// </summary>
        /// <param name="username">The username who made the project</param>
        /// <param name="project_id">The project the comment is a part of</param>
        /// <param name="comment">The comment to check for</param>
        public IAsyncEnumerable<Comment> GetRepliesToComment(string username, long project_id, Comment comment)
        {
            return GetRepliesToComment(username, project_id, comment.id);
        }

        /// <summary>
        /// Iterates over all the replies of a certain project comment
        /// </summary>
        /// <param name="username">The username who made the project</param>
        /// <param name="project_id">The project the comment is a part of</param>
        /// <param name="comment_id">The comment to check for</param>
        public IAsyncEnumerable<Comment> GetRepliesToComment(string username, long project_id, long comment_id)
        {
            return DownloadList<Comment>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/comments/{comment_id}/replies", 40);
        }

        /// <summary>
        /// Gets more information about a project comment. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="username">The username who made the project</param>
        /// <param name="project_id">The project the comment is a part of</param>
        /// <param name="comment_id">The comment to get more information about</param>
        /// <returns></returns>
        public Task<Comment?> GetProjectCommentInfo(string username, long project_id, long comment_id)
        {
            return DownloadData<Comment>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/comments/{comment_id}");
        }

        /// <summary>
        /// Downloads how many projects the user has shared, or null if the count couldn't be retrieved
        /// </summary>
        /// <param name="username">The username to get the shared project count from</param>
        /// <returns>Returns how many projects the user has shared, or null if the count couldn't be retrieved</returns>
        public async Task<long?> DownloadProjectCount(string username)
        {
            var html = await DownloadUserSite(username);
            if (html == null)
            {
                return null;
            }

            var match = RegexObjects.FindSharedProjectCount.Match(html);

            if (match.Success && long.TryParse(match.Groups[1].Value, out var result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of ALL projects made by the logged in user, or null if the number couldn't be retrieved
        /// </summary>
        /// <returns>Returns the count of ALL projects made by the logged in user, or null if the count couldn't be retrieved</returns>
        public async Task<long?> GetAllProjectCount()
        {
            var html = await DownloadMyStuff();
            if (html == null)
            {
                return null;
            }

            var match = RegexObjects.FindAllProjectCount.Match(html);

            if (match.Success && long.TryParse(match.Groups[1].Value, out var result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }

        Task<string?> DownloadMyStuff()
        {
            if (!LoggedIn)
            {
                return Task.FromResult<string?>(null);
            }
            try
            {
                return DownloadWebSite("https://scratch.mit.edu/mystuff/");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to download My Stuff page, {e}");
                return Task.FromResult<string?>(null);
            }
        }

        Task<string?> DownloadUserSite(string username)
        {
            try
            {
                return DownloadWebSite($"https://scratch.mit.edu/users/{username}/");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to download site data for user {username}, {e}");
                return Task.FromResult<string?>(null);
            }
            /*try
            {
                using (var siteStream = await DownloadFromURL($"https://scratch.mit.edu/users/{username}/"))
                {
                    using (var reader = new StreamReader(siteStream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to download site data for user {username}, {e}");
                return null;
            }*/
        }

        async Task<string?> DownloadWebSite(string url)
        {
            using (var siteStream = await DownloadFromURL(url))
            {
                using (var reader = new StreamReader(siteStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        #endregion

        #region STUDIO DOWNLOADERS

        /// <summary>
        /// Gets more information about a studio. Returns null if unable to retrieve the info
        /// </summary>
        /// <param name="studio_id">The id of the studio to download</param>
        public Task<Studio?> GetStudioInfo(long studio_id)
        {
            return DownloadData<Studio>($"https://api.scratch.mit.edu/studios/{studio_id}");
        }

        /// <summary>
        /// Iterates over all the projects in a studio
        /// </summary>
        /// <param name="studio_id">The studio to search for projects</param>
        public IAsyncEnumerable<StudioProject> GetProjectsInStudio(long studio_id)
        {
            return DownloadList<StudioProject>($"https://api.scratch.mit.edu/studios/{studio_id}/projects/", 24);
        }

        /// <summary>
        /// Gets more information about a studio. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        public Task<Studio?> DownloadStudioInfo(long studio_id)
        {
            return DownloadData<Studio>($"https://api.scratch.mit.edu/studios/{studio_id}");
        }

        /// <summary>
        /// Gets all the managers for a certain studio
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        public IAsyncEnumerable<User> GetStudioManagers(long studio_id)
        {
            return DownloadList<User>($"https://api.scratch.mit.edu/studios/{studio_id}/managers", 40);
        }

        /// <summary>
        /// Gets all the curators for a certain studio
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        /// <returns></returns>
        public IAsyncEnumerable<User> GetStudioCurators(long studio_id)
        {
            return DownloadList<User>($"https://api.scratch.mit.edu/studios/{studio_id}/curators", 40);
        }

        /// <summary>
        /// Gets all the comments for a certain studio
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        /// <returns></returns>
        public IAsyncEnumerable<Comment> GetStudioComments(long studio_id)
        {
            return DownloadList<Comment>($"https://api.scratch.mit.edu/studios/{studio_id}/comments", 40);
        }

        /// <summary>
        /// Gets more info about a studio comment. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        /// <param name="comment_id">The id of the comment to check for</param>
        /// <returns></returns>
        public Task<Comment?> DownloadStudioComment(long studio_id, long comment_id)
        {
            return DownloadData<Comment>($"https://api.scratch.mit.edu/studios/{studio_id}/comments/{comment_id}");
        }

        /// <summary>
        /// Gets the replies to a certain studio comment
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        /// <param name="comment_id">The id of the comment to check for</param>
        /// <returns></returns>
        public IAsyncEnumerable<Comment> DownloadStudioCommentReplies(long studio_id, long comment_id)
        {
            return DownloadList<Comment>($"https://api.scratch.mit.edu/studios/{studio_id}/comments/{comment_id}/replies", 40);
        }

        /// <summary>
        /// Gets the activity for a certain studio
        /// </summary>
        /// <param name="studio_id">The studio to check for</param>
        /// <param name="limit">If specified, will filter the results so that only activity older than the specified date will be retrieved</param>
        /// <returns></returns>
        public IAsyncEnumerable<StudioStatus> GetStudioActivity(long studio_id, DateTime? limit)
        {
            string dateTimeStr = "";

            if (limit != null)
            {
                dateTimeStr = "?dateLimit=" + limit.Value.ToString("u");
                dateTimeStr = dateTimeStr!.Remove(21).Insert(21, "T");
            }

            return DownloadList<StudioStatus>($"https://api.scratch.mit.edu/studios/{studio_id}/activity{dateTimeStr}", 40);
        }

        #endregion

        #region USER DOWNLOADERS

        /// <summary>
        /// Gets more information about a user. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="username">The username to get more information about</param>
        public Task<User?> GetUserInfo(string username)
        {
            return DownloadData<User>($"https://api.scratch.mit.edu/users/{username}");
        }

        /// <summary>
        /// Gets all published projects by a user
        /// </summary>
        /// <param name="username">The username to search</param>
        public IAsyncEnumerable<Project> GetPublishedProjects(string username)
        {
            return DownloadList<Project>($"https://api.scratch.mit.edu/users/{username}/projects", 40);
        }

        /// <summary>
        /// Gets all curated studios by a user
        /// </summary>
        /// <param name="username">The username to search</param>
        public IAsyncEnumerable<Studio> GetCuratedStudios(string username)
        {
            return DownloadList<Studio>($"https://api.scratch.mit.edu/users/{username}/studios/curate", 40);
        }

        /// <summary>
        /// Gets all of a user's favorite projects
        /// </summary>
        /// <param name="username">The username to search</param>
        /// <returns></returns>
        public IAsyncEnumerable<Project> GetFavoriteProjects(string username)
        {
            return DownloadList<Project>($"https://api.scratch.mit.edu/users/{username}/favorites", 40);
        }

        /// <summary>
        /// Gets how many messages a user has. Returns null if the data couldn't be retrieved
        /// </summary>
        /// <param name="username">The username to check</param>
        public async Task<long?> GetUserMessageCount(string username)
        {
            var result = await DownloadData($"https://api.scratch.mit.edu/users/{username}/messages/count");
            if (result != null)
            {
                using var obj = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());
                if (obj.RootElement.TryGetProperty("count", out var countObj) && countObj.TryGetInt64(out var count))
                {
                    return count;
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        public async Task<Stream?> DownloadProfileImage(string username)
        {
            var userInfo = await GetUserInfo(username);
            if (userInfo == null)
            {
                return null;
            }
            else
            {
                return await DownloadProfileImage(userInfo.id);
            }
        }

        public Task<Stream> DownloadProfileImage(long userID)
        {
            return DownloadFromURL($"https://uploads.scratch.mit.edu/get_image/user/{userID}_512x512.png");
        }

        /// <summary>
        /// Gets all the users that are following a certain user
        /// </summary>
        /// <param name="username">The username to check</param>
        public IAsyncEnumerable<User> GetFollowers(string username)
        {
            return DownloadList<User>($"https://api.scratch.mit.edu/users/{username}/followers", 40);
        }

        /// <summary>
        /// Gets all the users this a certain user is following
        /// </summary>
        /// <param name="username">The username to check</param>
        public IAsyncEnumerable<User> GetFollowingUsers(string username)
        {
            return DownloadList<User>($"https://api.scratch.mit.edu/users/{username}/following", 40);
        }

        #endregion

        /// <summary>
        /// Downloads a list of items from a Scratch URL
        /// </summary>
        /// <typeparam name="T">The type of data to download</typeparam>
        /// <param name="url">The url to download from</param>
        /// <param name="limit">The limit of how many items to download at once</param>
        /// <param name="pageFormat">If set to true, then the items will be downloaded using "page" format. Otherwise they will be downloaded using "offset" format</param>
        /// <param name="maxParallelSpeed">How much the download operation can be parallelized</param>
        /// <returns>Returns the downloaded list</returns>
        async IAsyncEnumerable<T> DownloadList<T>(string url, int limit, bool pageFormat = false, int maxParallelSpeed = 2048)
        {
            int offset = -limit;
            int currentSpeed = 1;
            int speedCounter = 0;

            string BuildURL()
            {
                if (pageFormat)
                {
                    if (offset == 0)
                    {
                        return url;
                    }
                    else
                    {
                        int pageNumber = offset / limit;
                        return $"{url}?page={pageNumber + 1}&ascsort=&descsort=";
                    }
                }
                else
                {
                    return $"{url}?offset={offset}&limit={limit}";
                }
            }

            void StartDownloading(List<Task<List<T>?>> taskList)
            {
                speedCounter++;

                if (speedCounter >= 3)
                {
                    speedCounter = 0;
                    currentSpeed *= 2;
                    if (currentSpeed > maxParallelSpeed)
                    {
                        currentSpeed = maxParallelSpeed;
                    }
                }

                for (int i = 0; i < currentSpeed; i++)
                {
                    offset += limit;
                    var finalUrl = BuildURL();
                    taskList.Add(DownloadData<List<T>>(finalUrl));
                }
            }

            List<Task<List<T>?>> downloadTasks = new List<Task<List<T>?>>();
            StartDownloading(downloadTasks);

            if (pageFormat)
            {
                limit = 0;
            }

            while (true)
            {
                for (int i = 0; i < downloadTasks.Count - 1; i++)
                {
                    var result = await downloadTasks[i];
                    if (result == null || result.Count == 0)
                    {
                        yield break;
                    }

                    if (pageFormat && offset == 0 && limit == 0)
                    {
                        limit = result.Count;
                    }

                    foreach (var item in result)
                    {
                        yield return item;
                    }

                    if (result.Count < limit)
                    {
                        yield break;
                    }
                }

                var lastContent = await downloadTasks[downloadTasks.Count - 1];

                if (lastContent != null)
                {
                    if (pageFormat && offset == 0 && limit == 0)
                    {
                        limit = lastContent.Count;
                    }

                    if (lastContent.Count != 0 && lastContent.Count == limit)
                    {
                        downloadTasks.Clear();
                        StartDownloading(downloadTasks);
                    }

                    foreach (var item in lastContent)
                    {
                        yield return item;
                    }

                    if (lastContent.Count != limit)
                    {
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
            }
        }

        static Regex cookieSplitter = new Regex(@"^(.+?)=(.*?);", RegexOptions.Compiled);

        /// <summary>
        /// Splits cookies into their key and value pairs
        /// </summary>
        /// <param name="cookies">The cookies to split</param>
        /// <returns>Returns the key and value pairs of the cookies</returns>
        static IEnumerable<(string Key, string Value)> SplitCookies(IEnumerable<string> cookies)
        {
            foreach (var cookieString in cookies)
            {
                yield return SplitCookie(cookieString);
            }
        }

        /// <summary>
        /// Splits a cookie into it's key and value
        /// </summary>
        /// <param name="cookie">The cookie to split</param>
        /// <returns>Returns the key and value of the cookie</returns>
        static (string Key, string Value) SplitCookie(string cookie)
        {
            var match = cookieSplitter.Match(cookie);
            return (match.Groups[1].Value, match.Groups[2].Value);
        }

        /// <summary>
        /// Removes all cookies
        /// </summary>
        void ClearAllCookies()
        {
            cookieString.Clear();
        }

        /// <summary>
        /// Adds a cookie to the HTTP client
        /// </summary>
        /// <param name="cookieKey">The key of the cookie to add</param>
        /// <param name="cookieValue">The key of the cookie to remove</param>
        void AddCookie(string cookieKey, string cookieValue)
        {
            if (client == null)
            {
                return;
            }
            RemoveCookie(cookieKey);

            if (cookieString.Length <= 1)
            {
                cookieString.Clear();
                cookieString.Append($"{cookieKey}={cookieValue}");
            }
            else
            {
                cookieString.Append($";{cookieKey}={cookieValue}");
            }
            cookies.Add(cookieKey, cookieValue);

            cookieString.Replace("\r", "");
            cookieString.Replace("\n", "");

            client.DefaultRequestHeaders.Remove("Cookie");

            client.DefaultRequestHeaders.Add("Cookie", cookieString.ToString());
        }

        /// <summary>
        /// Removes a cookie from the HTTP client
        /// </summary>
        /// <param name="cookieKey">THe cookie to remove</param>
        /// <returns>Returns true if the cookie was removed</returns>
        bool RemoveCookie(string cookieKey)
        {
            if (client == null)
            {
                return false;
            }
            bool removed = false;
            if (cookies.ContainsKey(cookieKey))
            {
                cookies.Remove(cookieKey);
                removed = true;

                cookieString.Clear();

                foreach (var (key, value) in cookies)
                {
                    cookieString.Append($"{key}={value};");
                }

                cookieString.Remove(cookieString.Length - 1, 1);

                client.DefaultRequestHeaders.Remove("Cookie");
                client.DefaultRequestHeaders.Add("Cookie", cookieString.ToString());
            }

            return removed;
        }

        /// <summary>
        /// Disposes the HTTP Client of the Scratch API
        /// </summary>
        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null!;
            }
            GC.SuppressFinalize(this);
        }

        ~ScratchAPI() => Dispose();










        /*  -----PROJECT DOWNLOADING FUNCTIONS
		 * 
		 * 
		 * 
		 * 
		 */

        #region Project Downloading Functions

        /// <summary>
        /// Downloads a project and places the files into a directory
        /// </summary>
        /// <param name="projectID">The id of the project to download</param>
        /// <param name="outputDirectory">The output location of the project files</param>
        /// <exception cref="ProjectDownloadException">Throws if the project couldn't be downloaded. Either the project doesn't exist or the project is private</exception>
        /// <returns>Returns the output directory the files where placed in</returns>
        public async Task<DirectoryInfo> DownloadAndExportProject(long projectID, DirectoryInfo outputDirectory)
        {
            var projectInfo = await GetProjectInfo(projectID);

            if (projectInfo == null)
            {
                throw new ProjectDownloadException(projectID, $"Unable to download project files for {projectID}. Either the project isn't public, or the project ID is invalid");
            }

            var dir = await DownloadAndExportProject(projectID, projectInfo, outputDirectory);
            return dir;
        }

        /// <summary>
        /// Downloads a project and places the files into a directory
        /// </summary>
        /// <param name="info">The id of the project to download</param>
        /// <param name="outputDirectory">The output location of the project files</param>
        /// <exception cref="ProjectDownloadException">Throws if the project couldn't be downloaded. Either the project doesn't exist or the project is private</exception>
        /// <returns>Returns the output directory the files where placed in</returns>
        public Task<DirectoryInfo> DownloadAndExportProject(Project info, DirectoryInfo outputDirectory)
        {
            if (string.IsNullOrEmpty(info.project_token))
            {
                return DownloadAndExportProject(info.id, outputDirectory);
            }
            else
            {
                return DownloadAndExportProject(info.id, info, outputDirectory);
            }
        }

        /// <summary>
        /// Downloads a project and places the files into a directory
        /// </summary>
        /// <param name="id">The id of the project to download</param>
        /// <param name="info">The project information</param>
        /// <param name="outputDirectory">The output location of the project files</param>
        /// <returns>Returns the directory the project was exported to</returns>
        /// <exception cref="ProjectDownloadException">Throws if the project could not be downloaded and exported</exception>
        async Task<DirectoryInfo> DownloadAndExportProject(long id, Project info, DirectoryInfo outputDirectory)
        {
            var dir = await DownloadAndExportProjectInternal(id, info, outputDirectory);

            if (dir == null)
            {
                throw new ProjectDownloadException(id, $"Unable to download project files for {info.title ?? id.ToString()}. Either the project isn't public, or the project ID is invalid");
            }

            return dir;
        }

        /// <summary>
        /// Downloads a project from the scratch website
        /// </summary>
        /// <param name="info">The project to download</param>
        /// <returns>Returns the downloaded project information</returns>
		/// <exception cref="ProjectDownloadException">Throws if the project could not be downloaded. Either the project doesn't exist or the project is private</exception>
        public Task<DownloadedProject> DownloadProject(Project info)
        {
            if (string.IsNullOrEmpty(info.project_token))
            {
                return DownloadProject(info.id);
            }
            else
            {
                return DownloadProject(info.id, info.author.username, info.project_token);
            }
        }

        /// <summary>
        /// Downloads a project from the scratch website
        /// </summary>
        /// <param name="projectID">The project to download</param>
        /// <returns>Returns the downloaded project information</returns>
        /// <exception cref="ProjectDownloadException">Throws if the project could not be downloaded. Either the project doesn't exist or the project is private</exception>
        public async Task<DownloadedProject> DownloadProject(long projectID)
        {
            var info = await GetProjectInfo(projectID);

            if (info == null)
            {
                throw new ProjectDownloadException(projectID, $"Unable to download project files for {projectID}. Either the project isn't public, or the project ID is invalid");
            }
            else
            {
                return await DownloadProject(info);
            }
        }

        /// <summary>
        /// Downloads a project
        /// </summary>
        /// <param name="projectID">The ID of the project to download</param>
        /// <param name="project_token">The unique token of the project to download</param>
        /// <param name="author">The author of the project</param>
        /// <returns>Returns the downloaded project</returns>
        /// <exception cref="ProjectDownloadException">Throws if the project could not be downloaded</exception>
        async Task<DownloadedProject> DownloadProject(long projectID, string project_token, string? author)
        {
            var projectData = await DownloadData($"https://projects.scratch.mit.edu/{projectID}?token={project_token}");
            if (projectData == null)
            {
                projectData = await DownloadData($"https://projects.scratch.mit.edu/internalapi/project/{projectID}/get/?token={project_token}");
            }

            if (projectData == null)
            {
                throw new ProjectDownloadException(projectID, $"Unable to download project files for {projectID}. Either the project isn't public, or the project ID is invalid");
            }

            using (projectData)
            {
                using (var content = projectData.Content)
                {
                    var data = await content.ReadAsByteArrayAsync();

                    var p1 = DownloadSB1Project(projectID, data, author);
                    var p2Task = DownloadSB2Project(projectID, data, author);
                    var p3Task = DownloadSB3Project(projectID);

                    await Task.WhenAll(p2Task, p3Task);

                    DownloadedProject? project = null;

                    if (p1 != null)
                    {
                        project = p1;
                    }

                    if (p3Task.Result != null)
                    {
                        project = p3Task.Result;
                    }

                    if (p2Task.Result != null)
                    {
                        project = p2Task.Result;
                    }

                    if (project != null && project.Author == null)
                    {
                        project.Author = author;
                    }

                    if (project == null)
                    {
                        throw new ProjectDownloadException(projectID, $"Unable to download project files for {projectID}. Either the project isn't public, or the project ID is invalid");
                    }

                    return project;
                }
            }
        }

        /// <summary>
        /// Downloads and exports a project
        /// </summary>
        /// <param name="id">The ID of the project</param>
        /// <param name="info">The project info</param>
        /// <param name="outputDirectory">The directory to export the project to</param>
        /// <returns>Returns the directory the project was exported to, or null if the project could not be downloaded or exported</returns>
        async Task<DirectoryInfo?> DownloadAndExportProjectInternal(long id, Project info, DirectoryInfo outputDirectory)
        {
            var filteredName = Helpers.RemoveIllegalCharacters(info.title ?? id.ToString());
            if (string.IsNullOrEmpty(filteredName) || string.IsNullOrWhiteSpace(filteredName))
            {
                filteredName = "UNNAMED_PROJECT_" + Guid.NewGuid().ToString();
            }

            var projectSubDir = new DirectoryInfo(Helpers.PathAddBackslash(outputDirectory.FullName) + filteredName);

            if (projectSubDir.Exists && File.Exists(Helpers.PathAddBackslash(projectSubDir.FullName) + "Info.md"))
            {
                return projectSubDir;
            }

            var downloadedProject = await DownloadProject(id, info.project_token, info.author.username);
            if (downloadedProject == null)
            {
                return null;
            }
            if (!projectSubDir.Exists)
            {
                projectSubDir.Create();
            }


            await downloadedProject.ExportProject(projectSubDir, filteredName);

            if (projectTemplate == null)
            {
                using (var stream = typeof(ScratchAPI).Assembly.GetManifestResourceStream("ScratchDL.Resources.ProjectTemplate.md"))
                {
                    projectTemplate = new byte[stream!.Length];
                    await stream.ReadAsync(projectTemplate, 0, (int)stream.Length);
                }
            }

            var template = new StringBuilder(Encoding.UTF8.GetString(projectTemplate));

            var matches = Regex.Matches(template.ToString(), @"%(.+?)%");

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                if (match.Success)
                {
                    var group = match.Groups[1];

                    if (group.Value == "author.username")
                    {
                        template.Remove(match.Index, match.Length);
                        template.Insert(match.Index, downloadedProject.Author);
                        continue;
                    }

                    var placeholderPaths = group.Value.Split('.');

                    object? currentValue = info;

                    if (currentValue != null)
                    {
                        foreach (var path in placeholderPaths)
                        {
                            currentValue = Helpers.GetFieldValue(currentValue, path);
                            if (currentValue == null)
                            {
                                break;
                            }
                        }
                    }

                    template.Remove(match.Index, match.Length);
                    template.Insert(match.Index, currentValue?.ToString() ?? "");
                }
            }

            Stream? image;

            if (info != null)
            {
                image = await DownloadProjectImage(info);
            }
            else
            {
                image = await DownloadProjectImage(id);
            }
            if (image != null)
            {
                var imageFileName = Regex.Match(info?.image ?? id.ToString(), @"(\d{3,}_\d{2,}x\d{2,}\..*$)").Groups[1].Value;

                var imagePath = Helpers.PathAddBackslash(projectSubDir.FullName) + imageFileName;
                using var file = File.Open(imagePath, FileMode.Create, FileAccess.Write);

                var imageWriteTask = image.CopyToAsync(file);
                await imageWriteTask;
                image.Close();

                template.Replace("%thumbnail%", imageFileName);
            }
            else
            {
                template.Replace("%thumbnail%", "");
            }

            using (var infoFileStream = await Helpers.WaitTillFileAvailable(Helpers.PathAddBackslash(projectSubDir.FullName) + "Info.md", FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(infoFileStream))
                {
                    await writer.WriteAsync(template.ToString());
                }
            }

            return projectSubDir;
        }

        /// <summary>
        /// Downloads data from a URL
        /// </summary>
        /// <param name="url">The url to download the data from</param>
        /// <returns></returns>
        /// <exception cref="Exception">Throws if the file couldn't be downloaded</exception>
        public async Task<Stream> DownloadFromURL(string url)
        {
            int counter = 0;
            while (true)
            {
                try
                {
                    var response = await client.GetAsync(url);

                    return await response.Content.ReadAsStreamAsync();
                }
                catch (Exception)
                {
                    counter++;

                    if (counter >= 3)
                    {
                        throw;
                    }
                    else
                    {
                        Console.WriteLine("Failed to download from " + url);
                        Console.WriteLine("Retrying");
                    }
                }
            }
        }

        /// <summary>
        /// Downloads the project's thumbnail image
        /// </summary>
        /// <param name="projectID">The project ID to get the image of</param>
        /// <returns>Returns a stream to the image data, or null if the image couldn't be downloaded</returns>
        public async Task<Stream?> DownloadProjectImage(long projectID)
        {
            try
            {
                return await DownloadFromURL($"https://cdn2.scratch.mit.edu/get_image/project/{projectID}_480x360.png");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to download image of project {projectID} : {e}");
                return null;
            }
        }

        /// <summary>
        /// Downloads the project's thumbnail image
        /// </summary>
        /// <param name="info">The project to get the image of</param>
        /// <returns>Returns a stream to the image data, or null if the image couldn't be downloaded</returns>
        public async Task<Stream?> DownloadProjectImage(Project info)
        {
            Stream? stream = null;
            try
            {
                stream = await DownloadFromURL(info.image);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to download image of project {info.title} : {e}");
            }
            if (stream == null && info.images.Count > 0)
            {
                try
                {
                    stream = await DownloadFromURL(info.images.MaxBy(i => int.Parse(i.Key.Split("x")[0])).Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to download image of project {info.title} : {e}");
                }
            }
            return stream;
        }

        /// <summary>
        /// Downloads a project as an SB1 project
        /// </summary>
        /// <param name="projectID">The ID of the project</param>
        /// <param name="data">The downloaded project data</param>
        /// <param name="author">The author of the project</param>
        /// <returns>Returns the SB1 project, or null if the data could not be interpreted as an SB1 project</returns>
        SB1Project? DownloadSB1Project(long projectID, byte[] data, string? author)
        {
            var magic = "ScratchV0".ToCharArray();
            if (Helpers.IsSpanSameAs(data.AsSpan(0, magic.GetLength(0)), magic))
            {
                return new SB1Project(author ?? FindAuthorInSB1(data), data);
            }
            return null;
        }

        /// <summary>
        /// Searches over the SB1 binary data for the author of the project
        /// </summary>
        /// <param name="data">The SB1 binary project data</param>
        /// <returns>Returns the author, or null if no author was found</returns>
        string? FindAuthorInSB1(byte[] data)
        {
            int pos = Helpers.FindPositionInSpan(data, "author".ToCharArray());
            if (pos > -1)
            {
                pos += 11;

                StringBuilder author = new StringBuilder();
                while (data[pos] >= 33)
                {
                    author.Append((char)data[pos]);
                    pos++;
                }
                return author.ToString();
            }
            return null;
        }


        /// <summary>
        /// Attempts to download a project as an SB2 Project
        /// </summary>
        /// <param name="projectID">The id of the project</param>
        /// <param name="data">The downloaded project data</param>
        /// <param name="author">The author of the project</param>
        /// <returns>Returns the project as an SB2 project, or null if an SB2 project isn't possible</returns>
        async Task<SB2Project?> DownloadSB2Project(long projectID, byte[] data, string? author)
        {
            try
            {
                string s = Encoding.UTF8.GetString(data, 0, data.Length);
                if (!string.IsNullOrWhiteSpace(s))
                {
                    var obj = JsonNode.Parse(s, documentOptions: new JsonDocumentOptions() { MaxDepth = 2000 });
                    if (obj == null)
                    {
                        return SB2BinaryProject(data);
                    }
                    return await SB2JsonProject(projectID, obj, author);
                }
                return null;
            }
            catch (Exception e)
            {
                return SB2BinaryProject(data);
            }
        }

        /// <summary>
        /// Attempts to download a project as an SB3 Project
        /// </summary>
        /// <param name="projectID">The id of the project</param>
        /// <returns>Returns the project as an SB3 project, or null if an SB3 project isn't possible</returns>
        async Task<SB3Project?> DownloadSB3Project(long projectID)
        {
            string PROJECTS_API = $"https://projects.scratch.mit.edu/{projectID}";
            string ASSETS_API = "https://assets.scratch.mit.edu/internalapi/asset/$path/get/";


            List<SB3Project.SB3File> files = new List<SB3Project.SB3File>();

            using (var projectResponse = await DownloadData(PROJECTS_API))
            {
                if (projectResponse == null)
                {
                    return null;
                }
                var response = await projectResponse.Content.ReadAsStringAsync();

                if (response.StartsWith("<"))
                {
                    return null;
                }

                JsonDocument json;

                try
                {
                    json = JsonDocument.Parse(response, new JsonDocumentOptions() { MaxDepth = 20000 });
                }
                catch (Exception e)
                {
                    return null;
                }

                if (json == null)
                {
                    return null;
                }

                using (json)
                {

                    if (!json.RootElement.TryGetProperty("objName", out _))
                    {
                        return null;
                    }
                    files.Add(new SB3Project.SB3File("project.json", Encoding.UTF8.GetBytes(response)));

                    object filesLock = new object();

                    List<JsonElement> costumes = new List<JsonElement>();
                    List<JsonElement> sounds = new List<JsonElement>();
                    List<JsonElement> assets = new List<JsonElement>();

                    async Task addFile(JsonElement data)
                    {
                        var path = "";

                        if (data.TryGetProperty("md5ext", out var md5Obj))
                        {
                            path = md5Obj.GetString()!;
                        }
                        else
                        {
                            var idObj = data.GetProperty("assetId");
                            var formatObj = data.GetProperty("dataFormat");

                            path = $"{idObj.GetString()}.{formatObj.GetString()}";
                        }

                        using (var response = await DownloadData(ASSETS_API.Replace("$path", path)))
                        {
                            if (response != null)
                            {
                                var assetData = await response.Content.ReadAsByteArrayAsync();
                                lock (filesLock)
                                {
                                    files.Add(new SB3Project.SB3File(path, assetData));
                                }
                            }
                        }
                    }
                    if (json.RootElement.TryGetProperty("targets", out var targetsObj))
                    {
                        foreach (var target in targetsObj.EnumerateArray())
                        {
                            if (target.TryGetProperty("costumes", out var costumesObj))
                            {
                                foreach (var costume in costumesObj.EnumerateArray())
                                {
                                    costumes.Add(costume);
                                    assets.Add(costume);
                                }
                            }
                            if (target.TryGetProperty("sounds", out var soundsObj))
                            {
                                foreach (var sound in soundsObj.EnumerateArray())
                                {
                                    sounds.Add(sound);
                                    assets.Add(sound);
                                }
                            }
                        }
                    }
                    else if (json.RootElement.TryGetProperty("costumes", out var costumesObj) && json.RootElement.TryGetProperty("sounds", out var soundsObj))
                    {
                        foreach (var costume in costumesObj.EnumerateArray())
                        {
                            costumes.Add(costume);
                            assets.Add(costume);
                        }

                        foreach (var sound in soundsObj.EnumerateArray())
                        {
                            sounds.Add(sound);
                            assets.Add(sound);
                        }
                    }
                    else
                    {
                        return null;
                    }

                    assets = assets.Distinct(new SB3Project.SB3JsonElementDistict()).ToList();

                    Task[] tasks = new Task[assets.Count];

                    for (int i = 0; i < assets.Count; i++)
                    {
                        tasks[i] = addFile(assets[i]);
                    }

                    await Task.WhenAll(tasks);

                    files.Sort(SB3Project.SB3File.Comparer.Default);

                    return new SB3Project(null, files);

                }
            }
        }

        /// <summary>
        /// Scans a json object for any sound, pen or music assets
        /// </summary>
        /// <param name="json">The json object to scan</param>
        /// <returns>Returns a list of assets found</returns>
        List<MetaAsset> ScanForAssets(JsonNode json)
        {
            List<MetaAsset> Assets = new List<MetaAsset>();
            if (json["penLayerMD5"] != null && json["penLayerID"] != null)
            {
                try
                {
                    Assets.Add(new PenMetaAsset(
                            (string)json["penLayerMD5"].AsValue(),
                            json,
                            (int)json["penLayerID"].AsValue()
                        ));
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to download pen layer");
                }
            }
            if (json["sounds"] != null)
            {
                int index = -1;
                foreach (var sound in json["sounds"].AsArray())
                {
                    index++;
                    try
                    {
                        Assets.Add(new SoundMetaAsset(
                            (string?)sound["md5"].AsValue(),
                            sound,
                            (string?)sound["soundName"].AsValue(),
                            (int)sound["soundID"].AsValue(),
                            (int)sound["sampleCount"].AsValue(),
                            (int)sound["rate"].AsValue(),
                            (string?)sound["format"].AsValue()
                        ));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Failed to download sound : " + json["sounds"][index]!.ToString());
                    }
                }
            }
            if (json["costumes"] != null)
            {
                int index = -1;
                foreach (var costume in json["costumes"].AsArray())
                {
                    index++;
                    try
                    {
                        Assets.Add(new CostumeMetaAsset(
                            (string?)costume["baseLayerMD5"].AsValue(),
                            costume,
                            (string?)costume["costumeName"].AsValue(),
                            (int)costume["baseLayerID"].AsValue(),
                            (int?)costume["bitmapResolution"]?.AsValue(),
                            (int)costume["rotationCenterX"].AsValue(),
                            (int)costume["rotationCenterY"].AsValue()
                        ));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Failed to download costume : " + json["costumes"][index]!.ToString());
                        throw;
                    }
                }
            }
            if (json["children"] != null)
            {
                foreach (var child in json["children"].AsArray())
                {
                    Assets.AddRange(ScanForAssets(child));
                }
            }
            return Assets;
        }

        /// <summary>
        /// Removes duplicates from the list. The duplicates are stored as lists in a dictionary.
        /// </summary>
        /// <param name="assets">The assets to deduplicate</param>
        /// <returns>Returns a dictionary where the duplicates are stored as lists</returns>
        Dictionary<MetaAsset, List<MetaAsset>> DeduplicateAssets(List<MetaAsset> assets)
        {
            var deduplicatedAssets = new Dictionary<MetaAsset, List<MetaAsset>>(MetaAsset.Comparer.Default);
            foreach (var asset in assets)
            {
                if (deduplicatedAssets.ContainsKey(asset))
                {
                    deduplicatedAssets[asset].Add(asset);
                }
                else
                {
                    deduplicatedAssets.Add(asset, new List<MetaAsset>());
                }
            }
            return deduplicatedAssets;
        }

        /// <summary>
        /// Interprets a json object as an SB2 Json Project
        /// </summary>
        /// <param name="projectID">The ID of the project</param>
        /// <param name="json">The json object to interpret</param>
        /// <param name="author">The author of the project</param>
        /// <returns>Returns an SB2 Json Project</returns>
        /// <exception cref="Exception">Throws if the json object could not be interpreted</exception>
        async Task<SB2Project> SB2JsonProject(long projectID, JsonNode json, string? author)
        {
            if (string.IsNullOrEmpty(author))
            {
                if (json["info"] != null)
                {
                    var info = json["info"];
                    if (info!["author"] != null)
                    {
                        author = (string)info!["author"]!.AsValue()!;
                    }
                }
            }

            List<string> ImageExtensions = new List<string>
            {
                "svg",
                "png",
                "jpg"
            };

            List<string> SoundExtensions = new List<string>
            {
                "wav",
                "mp3"
            };

            int soundAccumulator = 0;
            int imageAccumulator = 0;

            int GetAccumulator(MetaAsset asset)
            {
                var ext = asset.GetExtension();
                if (ImageExtensions.Contains(ext))
                {
                    return imageAccumulator++;
                }
                else if (SoundExtensions.Contains(ext))
                {
                    return soundAccumulator++;
                }
                throw new Exception("Unknown Extension: " + ext);
            }

            var assets = ScanForAssets(json);

            var dedupedAssets = DeduplicateAssets(assets);

            List<SB2File> Files = new List<SB2File>();

            foreach (var asset in dedupedAssets)
            {
                var accumulator = GetAccumulator(asset.Key);

                foreach (var reference in asset.Value.Append(asset.Key))
                {
                    if (reference is CostumeMetaAsset costumeAsset)
                    {
                        costumeAsset.BaseLayerID = accumulator;
                        costumeAsset.Parent["baseLayerID"] = accumulator;
                    }
                    else if (reference is SoundMetaAsset soundAsset)
                    {
                        soundAsset.SoundID = accumulator;
                        soundAsset.Parent["soundID"] = accumulator;
                    }
                    else if (reference is PenMetaAsset penAsset)
                    {
                        penAsset.PenLayerID = accumulator;
                        penAsset.Parent["penLayerID"] = accumulator;
                    }
                }


                var assetResult = await DownloadData($"https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/");

                if (assetResult == null)
                {
                    assetResult = await DownloadData($"https://assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}.png");
                }

                if (assetResult == null && !(asset.Key is PenMetaAsset))
                {
                    throw new Exception($"{projectID} - Failed to download asset = https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/");
                }
                if (assetResult != null)
                {
                    using (assetResult)
                    {
                        var data = await assetResult.Content.ReadAsByteArrayAsync();
                        Files.Add(new SB2File(
                            accumulator + "." + asset.Key.GetExtension(),
                            data
                        ));
                    }
                }
            }

            Files.Insert(0, new SB2File(
                "project.json",
                Encoding.UTF8.GetBytes(json.ToJsonString(new JsonSerializerOptions() { MaxDepth = 20000 }))
            ));

            Files.Sort(SB2File.Comparer.Default);

            return new SB2Project(author, Files);

        }

        SB2Project? SB2BinaryProject(byte[] data)
        {
            var zipMagic = "PK".ToCharArray();

            if (!Helpers.IsSpanSameAs(data.AsSpan().Slice(0, 2), zipMagic))
            {
                return null;
            }

            List<SB2File> Files = new List<SB2File>
            {
                new SB2File
                (
                    "BINARY.sb2",
                    data
                )
            };
            return new SB2Project(null, Files);
        }

        #endregion
    }
}
