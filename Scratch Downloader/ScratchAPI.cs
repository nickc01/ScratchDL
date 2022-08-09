using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
	public sealed class ScratchAPI : IDisposable
	{
		HttpClient client;
		string csrfToken;
		Dictionary<string, string> cookies = new Dictionary<string, string>();
		StringBuilder cookieString = new StringBuilder();

		public LoginInfo ProfileLoginInfo { get; private set; }

		public ScratchAPI()
		{
			client = new HttpClient(new HttpClientHandler { UseCookies = false });
			//client.DefaultRequestHeaders.Add("Referer", "https://scratch.mit.edu");
			client.DefaultRequestHeaders.Referrer = new Uri("https://scratch.mit.edu");
		}

		public static async Task<ScratchAPI> Create(string username, string password)
		{
			var accessor = new ScratchAPI();

			using (var csrfContent = await accessor.DownloadData("https://scratch.mit.edu/csrf_token/"))
			{
				if (!csrfContent.IsSuccessStatusCode)
				{
					throw new NoInternetException();
				}

				if (csrfContent.Headers.TryGetValues("Set-Cookie", out var cookies))
				{
                    foreach (var cookie in SplitCookies(cookies))
                    {
                        if (cookie.Key == "scratchcsrftoken")
                        {
							//Console.WriteLine("CSRF = " + cookie.Value);
							accessor.csrfToken = cookie.Value;
						}
						//var csrf = cookies.First(s => s.Contains("scratchcsrftoken"));
					}
				}
			}

			accessor.AddCookie("scratchlanguage", "en");
			accessor.RemoveCookie("scratchsessionsid");


			//LOGIN REQUEST
			var login = new LoginRequest()
			{
				username = username,
				password = password,
				csrftoken = accessor.csrfToken,
				csrfmiddlewaretoken = accessor.csrfToken,
			};

			accessor.client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
			accessor.client.DefaultRequestHeaders.Add("X-CSRFToken", accessor.csrfToken);

			using (var loginContent = await accessor.SendPostMessage("https://scratch.mit.edu/login/", JsonConvert.SerializeObject(login)))
			{
				if (!loginContent.IsSuccessStatusCode)
				{
					throw new LoginException();
				}
                foreach (var cookie in SplitCookies(loginContent.Headers.GetValues("Set-Cookie")))
                {
					if (cookie.Key == "scratchsessionsid")
					{
						//Console.WriteLine("SESSION ID = " + cookie.Value);
						accessor.AddCookie(cookie.Key, cookie.Value);
					}
					if (cookie.Key == "scratchcsrftoken")
					{
						accessor.RemoveCookie(cookie.Key);
						accessor.csrfToken = cookie.Value;
						accessor.AddCookie(cookie.Key, cookie.Value);
					}
				}
			}

			accessor.client.DefaultRequestHeaders.Remove("X-CSRFToken");
			accessor.client.DefaultRequestHeaders.Add("X-CSRFToken", accessor.csrfToken);

			accessor.ProfileLoginInfo = await accessor.DownloadData<LoginInfo>("https://scratch.mit.edu/session/");

			//Console.WriteLine("TOKEN = " + accessor.ProfileLoginInfo.user.token);
			
			accessor.client.DefaultRequestHeaders.Add("X-Token", accessor.ProfileLoginInfo.user.token);
			//Console.WriteLine("PROFILE INFO = " + JsonConvert.SerializeObject(accessor.ProfileLoginInfo,Formatting.Indented));

			accessor.AddCookie("permissions", JsonConvert.SerializeObject(accessor.ProfileLoginInfo.permissions));

            /*foreach (var cookie in accessor.GetCookies())
            {
				Console.WriteLine("Cookie = " + cookie);
            }*/
			
			return accessor;
		}

		async Task<HttpResponseMessage> DownloadData(string url, bool print = false)
		{
            if (print)
            {
				Console.WriteLine("DOWNLOADING FROM : " + url);
			}

            //var cookies = client.DefaultRequestHeaders.GetValues("Cookie");
            /*if (client.DefaultRequestHeaders.TryGetValues("Cookie",out var cookies))
            {
				Console.WriteLine("DOWNLOAD Cookies = " + (cookies.FirstOrDefault() ?? ""));
			}*/

            if (print)
            {
                if (client.DefaultRequestHeaders.TryGetValues("Cookie",out var cookies))
                {
					Console.WriteLine("Cookies = ");
					foreach (var cookie in cookies)
                    {
						Console.WriteLine(cookie);
                    }
                }
			}
			HttpResponseMessage response = null;
            for (int i = 0; i < 3; i++)
            {
				try
				{
					response = await client.GetAsync(url);
					break;
				}
				catch (Exception)
				{

				}
			}
            if (response == null || !response.IsSuccessStatusCode)
            {
				return null;
				//throw new Exception("Error downloading content : " + response.ReasonPhrase);
            }
            if (print)
            {
				Console.WriteLine("RESPONSE = " + await response.Content.ReadAsStringAsync());
			}

			ProcessNewCookies(response);

			return response;
		}

		async Task<T> DownloadData<T>(string url, bool print = false) where T : class
		{
			using (var response = await DownloadData(url, print))
			{
				if (response?.IsSuccessStatusCode == true)
				{
					return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
				}
				else
				{
					return null; 
				}
			}
		}

		async Task<HttpResponseMessage> SendPostMessage(string url, string data)
		{
			/*if (client.DefaultRequestHeaders.TryGetValues("Cookie", out var cookies))
			{
				Console.WriteLine("POST Cookies = " + (cookies.FirstOrDefault() ?? ""));
			}*/
			var response = await client.PostAsync(url, new StringContent(data));

			ProcessNewCookies(response);

			return response;

		}

		void ProcessNewCookies(HttpResponseMessage response)
        {
			if (response.Headers.TryGetValues("Set-Cookie", out var values))
			{
				foreach (var cookie in values)
                {
					var (key, value) = SplitCookie(cookie);
					AddCookie(key, value);
				}

				/*foreach (var cookies in values)
                {
                    foreach (var cookie in SplitCookie(cookies))
                    {
						AddCookie(cookie.Key, cookie.Value);
                    }
					//var splitCookies = cookies.Split(";");
                }*/
					//AddCookie();
					//client.DefaultRequestHeaders.Add("Cookie", values);
			}
		}

		/// <summary>
		/// Iterates over all the projects in the player's gallery
		/// </summary>
		/// <returns></returns>
		public IAsyncEnumerable<GalleryProjectInfo> GetGalleryProjects()
		{
			return DownloadList<GalleryProjectInfo>("https://scratch.mit.edu/site-api/projects/all/", 40, true);
		}

		/// <summary>
		/// Iterates over all the projects in the player's gallery
		/// </summary>
		/// <returns></returns>
		public IAsyncEnumerable<UserStudioInfo> GetStudios()
		{
			return DownloadList<UserStudioInfo>("https://scratch.mit.edu/site-api/galleries/all/", 40, true);
		}

		/// <summary>
		/// Iterates over all the projects in the player's gallery
		/// </summary>
		/// <returns></returns>
		public IAsyncEnumerable<StudioProjectInfo> GetProjectsInStudio(UserStudioInfo studio)
        {
			return GetProjectsInStudio(studio.pk);
		}


		public Task<StudioInfo> GetStudioInfo(long studio_id)
        {
			return DownloadData<StudioInfo>($"https://api.scratch.mit.edu/studios/{studio_id}");
        }


		/// <summary>
		/// Iterates over all the projects in a gallery
		/// </summary>
		/// <returns></returns>
		public IAsyncEnumerable<StudioProjectInfo> GetProjectsInStudio(long studio_id)
		{
			return DownloadList<StudioProjectInfo>($"https://api.scratch.mit.edu/studios/{studio_id}/projects/",24);
		}

		public Task<HealthInfo> GetWebsiteHealth()
        {
			return DownloadData<HealthInfo>("https://api.scratch.mit.edu/health");
        }

		public IAsyncEnumerable<NewsInfo> GetNews()
        {
			return DownloadList<NewsInfo>("https://api.scratch.mit.edu/news",40);
        }

		/// <summary>
		/// Gets the information about a project from the current user
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public Task<ProjectInfo> GetProjectInfo(long project_id)
		{
			return DownloadData<ProjectInfo>($"https://api.scratch.mit.edu/projects/{project_id}");
		}

		public Task<ProjectInfo> GetProjectInfo(GalleryProjectInfo project)
		{
			return GetProjectInfo(project.pk);
		}

		public Task<UserInfo> GetUserInfo(string username)
		{
			return DownloadData<UserInfo>($"https://api.scratch.mit.edu/users/{username}");
		}

		public IAsyncEnumerable<ProjectInfo> GetPublishedProjects(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/projects", 40);
		}

		public Task<ProjectInfo> GetProjectInfo(string username, long project_id)
		{
			return DownloadData<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}");
		}

		public IAsyncEnumerable<StudioInfo> GetCuratedStudios(string username)
		{
			return DownloadList<StudioInfo>($"https://api.scratch.mit.edu/users/{username}/studios/curate", 40);
		}

		public IAsyncEnumerable<ProjectInfo> GetFavoriteProjects(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/favorites", 40);
		}

		/// <summary>
		/// Gets all the users that are following this <paramref name="username"/>
		/// </summary>
		/// <param name="username">The username to check for followers</param>
		/// <returns></returns>
		public IAsyncEnumerable<UserInfo> GetFollowers(string username)
		{
			return DownloadList<UserInfo>($"https://api.scratch.mit.edu/users/{username}/followers", 40);
		}

		/// <summary>
		/// Gets all the users this <paramref name="username"/> is following
		/// </summary>
		/// <param name="username">The username to check</param>
		/// <returns></returns>
		public IAsyncEnumerable<UserInfo> GetFollowingUsers(string username)
		{
			return DownloadList<UserInfo>($"https://api.scratch.mit.edu/users/{username}/following", 40);
		}

		public Task<FeaturedProjects> GetFeaturedProjects()
		{
			return DownloadData<FeaturedProjects>($"https://api.scratch.mit.edu/proxy/featured");
		}

		/// <summary>
		/// Gets a list of projects that remix this <paramref name="project_id"/>
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectInfo> GetProjectRemixes(long project_id)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/projects/{project_id}/remixes", 40);
		}

		/// <summary>
		/// Gets a list of studios the project is added to.
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public IAsyncEnumerable<StudioInfo> GetProjectStudios(string username, long project_id)
		{
			return DownloadList<StudioInfo>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/studios", 40);
		}

		public IAsyncEnumerable<UserInfo> GetStudioManagers(long studio_id)
        {
			return DownloadList<UserInfo>($"https://api.scratch.mit.edu/studios/{studio_id}/managers",40);
		}

		public IAsyncEnumerable<UserInfo> GetStudioCurators(long studio_id)
		{
			return DownloadList<UserInfo>($"https://api.scratch.mit.edu/studios/{studio_id}/curators", 40);
		}

		public IAsyncEnumerable<CommentInfo> GetProjectComments(string username, long project_id)
		{
			return DownloadList<CommentInfo>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/comments", 40);
		}

		public IAsyncEnumerable<CommentInfo> GetRepliesToComment(string username, long project_id, CommentInfo comment)
        {
			return GetRepliesToComment(username, project_id, comment.id);
        }

		public IAsyncEnumerable<CommentInfo> GetRepliesToComment(string username, long project_id, long comment_id)
        {
			return DownloadList<CommentInfo>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/comments/{comment_id}/replies",40);
		}

		public Task<CommentInfo> GetComment(string username, long project_id, long comment_id)
        {
			return DownloadData<CommentInfo>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}/comments/{comment_id}");
        }

		public async Task<long> GetAllProjectCount()
        {
			var result = await DownloadData("https://api.scratch.mit.edu/projects/count/all");
			var obj = JObject.Parse(await result.Content.ReadAsStringAsync());
			return obj["count"].Value<long>();
        }

		async IAsyncEnumerable<T> DownloadList<T>(string url,int limit, bool pageFormat = false, int maxParallelSpeed = 2048)
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

			void StartDownloading(List<Task<List<T>>> taskList)
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

			//string finalUrl = BuildURL();

			//var downloadTask = DownloadData<List<T>>(finalUrl);
			List<Task<List<T>>> downloadTasks = new List<Task<List<T>>>();
			StartDownloading(downloadTasks);

			if (pageFormat)
			{
				limit = 0;
			}

			//int currentDownloadIndex = 0;

			//40 comments per request
			while (true)
			{
				//var result = await downloadTasks[currentDownloadIndex];
				//await Task.WhenAll(downloadTasks);


                for (int i = 0; i < downloadTasks.Count - 1; i++)
                {
					//var result = downloadTasks[i].Result;
					var result = await downloadTasks[i];
                    if (result == null || result.Count == 0)
                    {
						//Console.WriteLine("Early Break 1");
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
						//Console.WriteLine("Early Break 2");
						yield break;
                    }
				}

				//var lastContent = downloadTasks[downloadTasks.Count - 1].Result;
				var lastContent = await downloadTasks[downloadTasks.Count - 1];

				if (lastContent != null)
				{
					if (pageFormat && offset == 0 && limit == 0)
					{
						limit = lastContent.Count;
					}

					if (lastContent.Count != 0 && lastContent.Count == limit)
                    {
						//Console.WriteLine("Continuing");
						//offset += limit;
						//finalUrl = BuildURL();
						downloadTasks.Clear();
						StartDownloading(downloadTasks);
						//downloadTask = DownloadData<List<T>>(finalUrl);
					}

					foreach (var item in lastContent)
					{
						yield return item;
					}

					if (lastContent.Count != limit)
					{
						//Console.WriteLine("Early Break 3");
						yield break;
					}
				}
				else
				{
					yield break;
				}
			}
		}

		static Regex cookieSplitter;

		static IEnumerable<(string Key, string Value)> SplitCookies(IEnumerable<string> cookies)
		{
			foreach (var cookieString in cookies)
			{
				yield return SplitCookie(cookieString);
			}
		}

		static (string Key, string Value) SplitCookie(string cookie)
		{
			if (cookieSplitter == null)
			{
				cookieSplitter = new Regex(@"^(.+?)=(.*?);", RegexOptions.Compiled);
			}

			var match = cookieSplitter.Match(cookie);
			return (match.Groups[1].Value, match.Groups[2].Value);

			/*var cookieArray = cookies.Split(';');

            foreach (var cookie in cookieArray)
            {
				var match = cookieSplitter.Match(cookie);
                if (match.Success)
                {
					yield return (match.Groups[1].Value, match.Groups[2].Value);
				}
			}*/

			//var match = cookieSplitter.Match(cookie);
			//return (match.Groups[1].Value, match.Groups[2].Value);
		}

		void AddCookie(string key, string value)
		{
			//Console.WriteLine($"Adding Cookie = {key}={value}");
			RemoveCookie(key);

            if (cookieString.Length <= 1)
            {
				cookieString.Clear();
				cookieString.Append($"{key}={value}");
			}
			else
            {
				cookieString.Append($";{key}={value}");
			}
			//client.DefaultRequestHeaders.Add("Cookie", $"{key}={value}");
			cookies.Add(key, value);

			client.DefaultRequestHeaders.Remove("Cookie");
			client.DefaultRequestHeaders.Add("Cookie", cookieString.ToString());
		}

		bool RemoveCookie(string key)
        {
			bool removed = false;
            if (cookies.ContainsKey(key))
            {
				//Console.WriteLine($"Removing Cookie = {key}={cookies[key]}");
				cookies.Remove(key);
				removed = true;

				cookieString.Clear();

				foreach (var (cookieKey, cookieValue) in cookies)
				{
					cookieString.Append($"{cookieKey}={cookieValue};");
				}

				cookieString.Remove(cookieString.Length - 1, 1);

				client.DefaultRequestHeaders.Remove("Cookie");
				client.DefaultRequestHeaders.Add("Cookie", cookieString.ToString());
			}

			return removed;
        }

		IEnumerable<(string Key, string Value)> GetCookies()
        {
			if (cookieSplitter == null)
			{
				cookieSplitter = new Regex(@"^(.+?)=(.*?);", RegexOptions.Compiled);
			}

            if (client.DefaultRequestHeaders.TryGetValues("Cookie",out var values))
            {
				var cookies = values.FirstOrDefault() ?? "";

				var matches = cookieSplitter.Matches(cookies);

				foreach (Match match in matches)
				{
					yield return (match.Groups[1].Value, match.Groups[2].Value);
				}
			}
        }

		public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            GC.SuppressFinalize(this);
        }

        ~ScratchAPI()
        {
            Dispose();
        }










		/*  -----PROJECT DOWNLOADING FUNCTIONS
		 * 
		 * 
		 * 
		 * 
		 */

		static bool CheckSpan(ReadOnlySpan<byte> span, char[] characters)
		{
			for (int i = 0; i < characters.GetLength(0); i++)
			{
				if (span[i] != characters[i])
				{
					return false;
				}
			}
			return true;
		}

		static int FindPositionInSpan(ReadOnlySpan<byte> span, char[] characters)
		{
			int startPos = 0;
			int charactersFound = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == characters[charactersFound])
                {
                    if (charactersFound == 0)
                    {
						startPos = i;
					}
					charactersFound++;

                    if (charactersFound == characters.Length)
                    {
						return startPos;
                    }
				}
				else
                {
					startPos = 0;
					charactersFound = 0;
                }
            }

			return -1;
			/*for (int i = 0; i < characters.GetLength(0); i++)
			{ 
				if (span[i] != characters[i])
				{
					return false;
				}
			}
			return true;*/
		}

		public async Task<DirectoryInfo> DownloadAndExportProject(long projectID, DirectoryInfo outputDirectory)
		{
			var projectInfoTask = GetProjectInfo(projectID);

			var dir = await DownloadAndExportProject(await projectInfoTask, outputDirectory);
			return dir;
		}

		public async Task<DirectoryInfo> DownloadAndExportProject(ProjectInfo info, DirectoryInfo outputDirectory)
		{
            if (info == null)
            {
				return null;
            }
			var dir = await ExportProject(info, outputDirectory);

			if (dir == null)
			{
				Console.WriteLine("Failed to download: " + info.title);
			}

			return dir;
		}

		public Task<DownloadedProject> DownloadProject(ProjectInfo info)
        {
			return DownloadProject(info.id, info.author.username);
        }

		public async Task<DownloadedProject> DownloadProject(long projectID, string author)
		{
			//https://projects.scratch.mit.edu/1872191

			var projectData = await DownloadData($"https://projects.scratch.mit.edu/internalapi/project/{projectID}/get/");

            if (projectData == null)
            {
				projectData = await DownloadData($"https://projects.scratch.mit.edu/{projectID}");
			}

			Console.WriteLine($"Downloaded Data for {projectID} = {projectData != null}");


			using (projectData)
			{
                if (projectData == null)
                {
					Console.WriteLine($"A - {projectID}");
					return null;
                }
				using (var content = projectData.Content)
				{
					//List<DownloadedProject> projects = new List<DownloadedProject>();

					var data = await content.ReadAsByteArrayAsync();

					var p1 = DownloadSB1Project(projectID, data, author);
					var p2Task = DownloadSB2Project(projectID, data, author);
					var p3Task = DownloadSB3Project(projectID);

					await Task.WhenAll(p2Task, p3Task);

					DownloadedProject project = null;

                    if (p1 != null)
                    {
						//projects.Add(p1);
						project = p1;
                    }

                    if (p2Task.Result != null)
                    {
						//projects.Add(p2Task.Result);
						project = p2Task.Result;
                    }

                    if (p3Task.Result != null)
                    {
						//projects.Add(p3Task.Result);
						project = p3Task.Result;
                    }

                    if (project != null && project.Author == null)
                    {
						project.Author = author;
                    }

					return project;
				}
			}
		}

		public async Task<DirectoryInfo> ExportProject(ProjectInfo info, DirectoryInfo outputDirectory)
		{
			var filteredName = FilterName(info.title);

			if (string.IsNullOrEmpty(filteredName) || string.IsNullOrWhiteSpace(filteredName))
			{
				filteredName = "UNNAMED_PROJECT_" + Guid.NewGuid().ToString();
			}

			var projectSubDir = new DirectoryInfo(Utilities.PathAddBackslash(outputDirectory.FullName) + filteredName);

            if (projectSubDir.Exists && File.Exists(Utilities.PathAddBackslash(projectSubDir.FullName) + "Info.md"))
            {
				return projectSubDir;
            }

			Console.WriteLine("Downloading: " + info.title);

			var downloadedProject = await DownloadProject(info.id, info.author.username);

			if (downloadedProject == null)
			{
				return null;
			}

			//Console.WriteLine("Exporting: " + info.title);

			//var projectSubDir = outputDirectory.CreateSubdirectory(filteredName);

			if (!projectSubDir.Exists)
			{
				projectSubDir.Create();
			}


			downloadedProject.ExportProject(projectSubDir, filteredName);

			//Console.WriteLine("Completed Download of: " + info.title);

			var template = new StringBuilder(Encoding.UTF8.GetString(Resources.Resource.Project_Template));

			var projectInfoJson = JObject.FromObject(info);

			var matches = Regex.Matches(template.ToString(), @"%(.+?)%");

			for (int i = matches.Count - 1; i >= 0; i--)
			{
				var match = matches[i];
				if (match.Success)
				{
					var group = match.Groups[1];

                    if (group.Value == "author.username")
                    {
						//Console.WriteLine("123 = " + downloadedProject.Author);
						//Console.WriteLine("AUTHOR = " + downloadedProject.Author);
						template.Remove(match.Index, match.Length);
						template.Insert(match.Index, downloadedProject.Author);
						continue;
					}

					var placeholderPaths = group.Value.Split('.');

					JToken currentToken = projectInfoJson;

					foreach (var path in placeholderPaths)
					{
						currentToken = currentToken[path];
						if (currentToken == null)
						{
							break;
						}
					}

                    if (currentToken != null)
                    {
						template.Remove(match.Index, match.Length);
						template.Insert(match.Index, currentToken.Value<string>() ?? "null");
					}
				}
			}
			//Console.WriteLine("Downloading Image for: " + info.title);
			//var image = await DownloadProjectImage(info.id);
			var image = await DownloadProjectImage(info);

			var imageFileName = Regex.Match(info.image, @"(\d{3,}_\d{2,}x\d{2,}\..*$)").Groups[1].Value;

			var imagePath = Utilities.PathAddBackslash(projectSubDir.FullName) + imageFileName;

            //await File.WriteAllBytesAsync(imagePath, image);
            if (image != null)
            {
				using var file = File.OpenWrite(imagePath);

				var imageWriteTask = image.CopyToAsync(file);
				await imageWriteTask;
				image.Close();
			}

			template.Replace("%thumbnail%", imageFileName);

			//Console.WriteLine("Writing Image for: " + info.title);

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(projectSubDir.FullName) + "Info.md", template.ToString());

			Console.WriteLine("Done: " + info.title);


			return projectSubDir;
		}

		public async Task<Stream> DownloadFromURL(string url)
		{
			int counter = 0;
            while(true)
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

		Task<Stream> DownloadProjectImage(long projectID)
		{
			return DownloadFromURL($"https://cdn2.scratch.mit.edu/get_image/project/{projectID}_480x360.png");
		}

		async Task<Stream> DownloadProjectImage(ProjectInfo info)
		{
			//return DownloadFromURL($"https://cdn2.scratch.mit.edu/get_image/project/{projectID}_480x360.png");
			var stream = await DownloadFromURL(info.image);
            if (stream == null && info.images.Count > 0)
            {
				stream = await DownloadFromURL(info.images.MaxBy(i => int.Parse(i.Key.Split("x")[0])).Value);
            }
			return stream;
		}

		SB1Project DownloadSB1Project(long projectID, byte[] data, string author)
		{
			var magic = "ScratchV0".ToCharArray();
			if (CheckSpan(data.AsSpan(0, magic.GetLength(0)), magic))
			{
				return new SB1Project
				{
					Data = data,
					Author = author ?? FindAuthorInSB1(data)
				};
			}
			Console.WriteLine($"B - {projectID}");
			return null;
		}

		string FindAuthorInSB1(byte[] data)
        {
			int pos = FindPositionInSpan(data, "author".ToCharArray());
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

		async Task<SB2Project> DownloadSB2Project(long projectID, byte[] data, string author)
		{
			try
			{
				string s = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
                if (!string.IsNullOrWhiteSpace(s))
                {
					//var json = JObject.Parse(s);
					var json = JsonConvert.DeserializeObject<JObject>(s, new JsonSerializerSettings
                    {
						MaxDepth = 2000
                    });
					return await SB2JsonProject(projectID, json, author);
				}
				return null;
			}
			catch (Exception e)
			{
				Console.WriteLine($"C - {projectID} - {e}");
				return SB2BinaryProject(projectID, data);
			}
		}


		async Task<SB3Project> DownloadSB3Project(long projectID)
		{
			string PROJECTS_API = $"https://projects.scratch.mit.edu/{projectID}";
			//string REFERER = $"https://scratch.mit.edu/projects/{projectID}/";
			string ASSETS_API = "https://assets.scratch.mit.edu/internalapi/asset/$path/get/";


			List<SB3Project.SB3File> files = new List<SB3Project.SB3File>();

			using (var projectResponse = await DownloadData(PROJECTS_API))
			{

				var response = await projectResponse.Content.ReadAsStringAsync();

                if (response.StartsWith("<"))
                {
					Console.WriteLine($"D - {projectID}");
					return null;
                }

				JObject json = null;

				try
                {
					//json = JObject.Parse(response);
					json = JsonConvert.DeserializeObject<JObject>(response, new JsonSerializerSettings
					{
						MaxDepth = 2000
					});
				}
				catch (Exception)
                {
					Console.WriteLine($"E - {projectID}");
					return null;
                }

                if (json["objName"] != null)
                {
					Console.WriteLine($"F - {projectID}");
					return null;
                }

				files.Add(new SB3Project.SB3File
				{
					path = "project.json",
					data = Encoding.UTF8.GetBytes(json.ToString(Formatting.None))
				});

				object filesLock = new object();

				List<JToken> costumes = new List<JToken>();
				List<JToken> sounds = new List<JToken>();
				List<JToken> assets = new List<JToken>();

				async Task addFile(JToken data)
				{
					var path = "";

					if (data["md5ext"] != null)
					{
						path = data.Value<string>("md5ext");
					}
					else
					{
						var id = data.Value<string>("assetId");
						var format = data.Value<string>("dataFormat");

						path = $"{id}.{format}";
					}

					using (var response = await DownloadData(ASSETS_API.Replace("$path", path)))
					{
						var assetData = await response.Content.ReadAsByteArrayAsync();
						lock (filesLock)
						{
							files.Add(new SB3Project.SB3File
							{
								path = path,
								data = assetData
							});
						}
					}
				}

				if (json["targets"] != null)
				{
					foreach (var target in json["targets"])
					{
						if (target["costumes"] != null)
						{
							foreach (var costume in target["costumes"])
							{
								costumes.Add(costume);
								assets.Add(costume);
							}
						}
						if (target["sounds"] != null)
						{
							foreach (var sound in target["sounds"])
							{
								sounds.Add(sound);
								assets.Add(sound);
							}
						}
					}
				}
				else
                {
                    if (json["costumes"] != null && json["sounds"] != null)
                    {
						foreach (var costume in json["costumes"])
						{
							costumes.Add(costume);
							assets.Add(costume);
						}

						foreach (var sound in json["sounds"])
						{
							sounds.Add(sound);
							assets.Add(sound);
						}
					}
					else
                    {
						Console.WriteLine($"G - {projectID}");
						return null;
                    }
                }

				assets = assets.Distinct(new SB3Project.SB3JTokenDistict()).ToList();

				Task[] tasks = new Task[assets.Count];

				for (int i = 0; i < assets.Count; i++)
				{
					tasks[i] = addFile(assets[i]);
				}

				await Task.WhenAll(tasks);

				files.Sort(SB3Project.SB3File.Comparer.Default);

				return new SB3Project
				{
					Files = files
				};
			}
		}

		List<MetaAsset> ScanForAssets(JToken json)
		{
			List<MetaAsset> Assets = new List<MetaAsset>();
			if (json["penLayerMD5"] != null && json["penLayerID"] != null)
			{
				Assets.Add(new PenMetaAsset
				{
					MD5 = json["penLayerMD5"].Value<string>(),
					Parent = json,
					PenLayerID = json["penLayerID"].Value<int>()
				});
			}
			if (json["sounds"] != null)
			{
				foreach (var sound in json["sounds"])
				{
					Assets.Add(new SoundMetaAsset
					{
						MD5 = sound["md5"].Value<string>(),
						Parent = sound,
						Format = sound["format"].Value<string>(),
						SampleCount = sound["sampleCount"].Value<int>(),
						SampleRate = sound["rate"].Value<int>(),
						SoundID = sound["soundID"].Value<int>(),
						SoundName = sound["soundName"].Value<string>()
					});
				}
			}
			if (json["costumes"] != null)
			{
				int index = -1;
				foreach (var costume in json["costumes"])
				{
					index++;
					try
					{
						Assets.Add(new CostumeMetaAsset
						{
							MD5 = costume["baseLayerMD5"].Value<string>(),
							Parent = costume,
							BaseLayerID = costume["baseLayerID"].Value<int>(),
							BitmapResolution = costume["bitmapResolution"]?.Value<int>(),
							CostumeName = costume["costumeName"].Value<string>(),
							RotationCenterX = costume["rotationCenterX"].Value<int>(),
							RotationCenterY = costume["rotationCenterY"].Value<int>()
						});
					}
					catch (Exception e)
                    {
						Console.WriteLine($"Failed Costume Addition : " + json["costumes"][index].ToString());
						throw;
                    }
				}
			}
			if (json["children"] != null)
			{
				foreach (var child in json["children"])
				{
					Assets.AddRange(ScanForAssets(child));
				}
			}
			return Assets;
		}

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

		async Task<SB2Project> SB2JsonProject(long projectID, JObject json, string author)
		{
			SB2Project project = new SB2Project();

			project.Author = author;

            if (string.IsNullOrEmpty(author))
            {
                if (json["info"] != null)
                {
					var info = json["info"];
                    if (info["author"] != null)
                    {
						project.Author = info["author"].Value<string>();
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

			foreach (var asset in dedupedAssets)
			{
				var accumulator = GetAccumulator(asset.Key);

				//Console.WriteLine("Adding Asset: " + asset.Key.MD5 + " - ID: " + accumulator);



				foreach (var reference in asset.Value.Append(asset.Key))
				{
					if (reference is CostumeMetaAsset costumeAsset)
					{
						costumeAsset.BaseLayerID = accumulator;
						costumeAsset.Parent["baseLayerID"] = accumulator;
						//Console.WriteLine("Setting Costume \"" + costumeAsset.CostumeName + "\" ID: " + accumulator);
					}
					else if (reference is SoundMetaAsset soundAsset)
					{
						soundAsset.SoundID = accumulator;
						soundAsset.Parent["soundID"] = accumulator;
						//Console.WriteLine("Setting Sound \"" + soundAsset.SoundName + "\" ID: " + accumulator);
					}
					else if (reference is PenMetaAsset penAsset)
					{
						penAsset.PenLayerID = accumulator;
						penAsset.Parent["penLayerID"] = accumulator;
						//Console.WriteLine("Setting Pen \"" + penAsset.MD5 + "\" ID: " + accumulator);
					}
				}


				var assetResult = await DownloadData($"https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/");

                if (assetResult == null)
                {
					assetResult = await DownloadData($"https://assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}.png");
				}

                /*if (assetResult == null)
                {
					Console.WriteLine($"{projectID} - Failed to download asset = https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/");
                }*/

                if (assetResult == null && !(asset.Key is PenMetaAsset))
                {
					throw new Exception($"{projectID} - Failed to download asset = https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/");
				}
                if (assetResult != null)
                {
					using (assetResult)
					{
						var data = await assetResult.Content.ReadAsByteArrayAsync();
						project.Files.Add(new SB2File
						{
							Path = accumulator + "." + asset.Key.GetExtension(),
							Data = data
						});
					}
				}
			}

			project.Files.Insert(0, new SB2File
			{
				Path = "project.json",
				Data = Encoding.UTF8.GetBytes(json.ToString(Formatting.None))
			});

			project.Files.Sort(SB2File.Comparer.Default);

			return project;

		}

		SB2Project SB2BinaryProject(long projectID, byte[] data)
		{
			var zipMagic = "PK".ToCharArray();

			if (!CheckSpan(data.AsSpan().Slice(0, 2), zipMagic))
			{
				return null;
			}

			var project = new SB2Project();

			project.Files.Add(new SB2File
			{
				Path = "BINARY.sb2",
				Data = data
			});
			return project;
		}

		public static string FilterName(string name)
        {
			string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

			var filteredName = name;

			foreach (char c in invalidChars)
			{
				filteredName = filteredName.Replace(c.ToString(), "");
			}
			return filteredName;
		}
	}
}
