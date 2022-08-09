using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
	public class ScratchAPIObsolete
	{
		Dictionary<string, string> headerValues = new Dictionary<string, string>();
		Dictionary<string, string> cookies = new Dictionary<string, string>();

		public string CsrfToken { get; private set; }

		public LoginInfo ProfileLoginInfo;

		ScratchAPIObsolete() { }

		/// <summary>
		/// Creates a Scratch API object and logs into an account
		/// </summary>
		/// <param name="username">The username to login to</param>
		/// <param name="password">The password to login with</param>
		/// <exception cref="NoInternetException">Called when there's no internet connection</exception>
		/// <exception cref="LoginException">Called when invalid credentials were entered</exception>
		public static async Task<ScratchAPIObsolete> Create(string username, string password)
		{
			static Dictionary<string, string> GetCookies(HttpResponseMessage response)
			{
				var cookies = new Dictionary<string, string>();
				foreach (var value in response.Headers.GetValues("Set-Cookie"))
				{
					var match = Regex.Match(value, @"^(.+?)=(.*?);");
					cookies.Add(match.Groups[1].Value, match.Groups[2].Value);
				}
				return cookies;
			}


			var accessor = new ScratchAPIObsolete();

			using (var csrfContent = await accessor.DownloadData("https://scratch.mit.edu/csrf_token/", "https://scratch.mit.edu"))
			{
				if (!csrfContent.IsSuccessStatusCode)
				{
					throw new NoInternetException();
				}
				accessor.cookies = GetCookies(csrfContent);
				foreach (var cookie in accessor.cookies)
				{
					if (cookie.Key == "scratchcsrftoken")
					{
						accessor.CsrfToken = cookie.Value;
					}
				}
			}

			accessor.cookies.Add("scratchlanguage", "en");


			//LOGIN REQUEST
			var login = new LoginRequest()
			{
				username = username,
				password = password,
				csrftoken = accessor.CsrfToken,
				csrfmiddlewaretoken = accessor.CsrfToken,
			};

			accessor.headerValues = new Dictionary<string, string>
			{
				{"X-Requested-With", "XMLHttpRequest" },
				{"X-CSRFToken", accessor.CsrfToken }
			};

			using (var loginContent = await accessor.SendPostMessageRaw("https://scratch.mit.edu/login/", "https://scratch.mit.edu", JsonConvert.SerializeObject(login)))
			{
				if (!loginContent.IsSuccessStatusCode)
				{
					throw new LoginException();
				}
				var loginCookies = GetCookies(loginContent);
				foreach (var cookie in loginCookies)
				{
					if (cookie.Key == "scratchcsrftoken" || cookie.Key == "scratchsessionsid")
					{
						accessor.cookies.Remove(cookie.Key);
						accessor.cookies.Add(cookie.Key, cookie.Value);
					}
					if (cookie.Key == "scratchcsrftoken")
					{
						accessor.CsrfToken = cookie.Value;
					}
				}
			}

			accessor.headerValues.Remove("X-CSRFToken");
			accessor.headerValues.Add("X-CSRFToken", accessor.CsrfToken);

			accessor.ProfileLoginInfo = await accessor.DownloadData<LoginInfo>("https://scratch.mit.edu/session/", "https://scratch.mit.edu");

			accessor.cookies.Add("permissions",JsonConvert.SerializeObject(accessor.ProfileLoginInfo.permissions));

            foreach (var cookie in accessor.cookies)
            {
				Console.WriteLine($"COOKIE = {cookie.Key}={cookie.Value}");
            }

			return accessor;
		}

		/// <summary>
		/// Iterates over all the projects in the player's gallery
		/// </summary>
		/// <returns></returns>
		public async IAsyncEnumerable<GalleryProjectInfo> GetGalleryProjects()
		{
			int pageNumber = 1;
			//40 projects per request
			while (true)
			{
				var url = "https://scratch.mit.edu/site-api/projects/all/";
				if (pageNumber > 1)
				{
					url += "?page=" + pageNumber + "&ascsort=&descsort=";
				}

				var content = await DownloadData<List<GalleryProjectInfo>>(url, "https://scratch.mit.edu/mystuff/");

				if (content != null)
				{
					foreach (var project in content)
					{
						yield return project;
					}
					pageNumber++;
				}
				else
				{
					yield break;
				}
			}
		}

		/// <summary>
		/// Gets the information about a project from the current user
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public async Task<ProjectInfo> GetProjectInfo(long project_id)
		{
			return await DownloadData<ProjectInfo>($"https://api.scratch.mit.edu/projects/{project_id}", $"https://scratch.mit.edu/projects/{project_id}");
		}

		public Task<ProjectInfo> GetProjectInfo(GalleryProjectInfo project)
		{
			return GetProjectInfo(project.pk);
		}

		/// <summary>
		/// Gets a list of top-level comments created on the project
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectComment> GetProjectComments(long project_id)
		{
			return DownloadList<ProjectComment>($"https://api.scratch.mit.edu/comments/project/{project_id}", "https://scratch.mit.edu/projects/{project_id}");
		}

		/// <summary>
		/// Gets a list of replies to the comment created on the project
		/// </summary>
		/// <param name="project_id"></param>
		/// <param name="comment_id"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectComment> GetProjectCommentReplies(long project_id, long comment_id)
		{
			return DownloadList<ProjectComment>($"https://api.scratch.mit.edu/comments/project/{project_id}/{comment_id}", "https://scratch.mit.edu/projects/{project_id}");
		}

		public async Task<UserInfo> GetUserInfo(string username)
		{
			return await DownloadData<UserInfo>($"https://api.scratch.mit.edu/users/{username}", "https://scratch.mit.edu");
		}

		public IAsyncEnumerable<ProjectInfo> GetPublishedProjects(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/projects", "https://scratch.mit.edu");
		}

		public async Task<ProjectInfo> GetProjectInfo(string username, long project_id)
		{
			return await DownloadData<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/projects/{project_id}", $"https://scratch.mit.edu");
		}

		public IAsyncEnumerable<StudioInfo> GetCuratedStudios(string username)
		{
			return DownloadList<StudioInfo>($"https://api.scratch.mit.edu/users/{username}/studios/curate", "https://scratch.mit.edu");
		}

		public IAsyncEnumerable<ProjectInfo> GetFavoriteProjects(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/favorites", "https://scratch.mit.edu");
		}

		/// <summary>
		/// Gets all the users that are following this <paramref name="username"/>
		/// </summary>
		/// <param name="username">The username to check for followers</param>
		/// <returns></returns>
		public IAsyncEnumerable<UserInfo> GetFollowers(string username)
		{
			return DownloadList<UserInfo>($"https://api.scratch.mit.edu/users/{username}/followers", "https://scratch.mit.edu");
		}

		/// <summary>
		/// Gets all the users this <paramref name="username"/> is following
		/// </summary>
		/// <param name="username">The username to check</param>
		/// <returns></returns>
		public IAsyncEnumerable<UserInfo> GetFollowingUsers(string username)
		{
			return DownloadList<UserInfo>($"https://api.scratch.mit.edu/users/{username}/following", "https://scratch.mit.edu");
		}

		public async Task<FeaturedProjects> GetFeaturedProjects()
		{
			return await DownloadData<FeaturedProjects>($"https://api.scratch.mit.edu/proxy/featured", $"https://scratch.mit.edu");
		}

		/*/// <summary>
		/// Gets a list of projects that have recently been added to studios that the given user is following
		/// </summary>
		/// <param name="username">The username to check</param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectInfo> GetRecentlyAddedStudioProjects(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/following/studios/projects", "https://scratch.mit.edu");
		}*/

		/*/// <summary>
		/// Gets a list of projects that have recently been loved by users that the given user is following. Shows up as “Projects Loved by Scratchers I’m Following” on the front page
		/// </summary>
		/// <param name="username">The username to check</param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectInfo> GetProjectsLovedByUsersList(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/following/users/loves", "https://scratch.mit.edu");
		}*/

		/*/// <summary>
		/// Gets a list of projects that have recently been shared by users that the given user is following
		/// </summary>
		/// <param name="username"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectInfo> GetProjectsRecentlySharedByFollowersList(string username)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/users/{username}/following/users/projects", "https://scratch.mit.edu");
		}*/

		/*public Task<HttpResponseMessage> GetRecentActivity(string username)
		{
			return DownloadData($"https://api.scratch.mit.edu/users/{username}/following/users/activity","https://scratch.mit.edu");
		}*/

		/// <summary>
		/// Gets a list of projects that remix this <paramref name="project_id"/>
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public IAsyncEnumerable<ProjectInfo> GetProjectRemixes(string project_id)
		{
			return DownloadList<ProjectInfo>($"https://api.scratch.mit.edu/projects/{project_id}/remixes", "https://scratch.mit.edu");
		}

		/// <summary>
		/// Gets a list of studios the project is added to.
		/// </summary>
		/// <param name="project_id"></param>
		/// <returns></returns>
		public IAsyncEnumerable<StudioInfo> GetProjectStudios(string project_id)
		{
			return DownloadList<StudioInfo>($"https://api.scratch.mit.edu/projects/{project_id}/studios", "https://scratch.mit.edu");
		}



		async IAsyncEnumerable<T> DownloadList<T>(string url, string referrer)
		{
			int offset = 0;
			//40 comments per request
			while (true)
			{
				var finalUrl = $"{url}?offset={offset}&limit={40}";

				var content = await DownloadData<List<T>>(url, referrer);

				if (content != null)
				{
					foreach (var item in content)
					{
						yield return item;
					}

					if (content.Count == 40)
					{
						offset += 40;
					}
					else
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


		/// <summary>
		/// Builds an http client
		/// </summary>
		HttpClient BuildClient(string referer)
		{
			var client = new HttpClient(new HttpClientHandler { UseCookies = false });

			string cookieString = "";
			if (cookies != null)
			{
				foreach (var cookie in cookies)
				{
					cookieString += cookie.Key + "=" + cookie.Value + ";";
				}
				if (cookieString.Length > 0)
				{
					cookieString.Remove(cookieString.Length - 1);
				}
				client.DefaultRequestHeaders.Add("Cookie", cookieString);
			}

			if (headerValues != null)
			{
				foreach (var pair in headerValues)
				{
					client.DefaultRequestHeaders.Add(pair.Key, pair.Value);
				}
			}
			client.DefaultRequestHeaders.Add("Referer", referer);

			//Console.WriteLine("Cookies = " + cookieString);
			//Console.WriteLine("Referer = " + referer);
			foreach (var pair in client.DefaultRequestHeaders)
			{
				//Console.WriteLine($"Header {pair.Key} = {pair.Value}");
				//Console.WriteLine($"Printing Keys = {pair.Key}");
				foreach (var val in pair.Value)
				{
					//Console.WriteLine(val);
				}
			}

			return client;
		}

		async Task<HttpResponseMessage> DownloadData(string url, string referer)
		{
			using (var client = BuildClient(referer))
			{
				try
				{
					var response = await client.GetAsync(url);
					return response;
				}
				finally
				{
					if (headerValues != null)
					{
						foreach (var pair in headerValues)
						{
							client.DefaultRequestHeaders.Remove(pair.Key);
						}
					}
					if (cookies != null)
					{
						client.DefaultRequestHeaders.Remove("Cookie");
					}
					client.DefaultRequestHeaders.Remove("Referer");
				}
			}
		}

		async Task<T> DownloadData<T>(string url, string referer) where T : class
		{
			using (var response = await DownloadData(url, referer))
			{
				if (response.IsSuccessStatusCode)
				{
					var data = await response.Content.ReadAsStringAsync();
					return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
				}
				else
				{
					return null;
				}
			}
		}

		async Task<bool> SendPostMessage(string url, string referer, string data)
		{
			var response = await SendPostMessageRaw(url, referer, data);
			return response.IsSuccessStatusCode;
		}

		async Task<HttpResponseMessage> SendPostMessageRaw(string url, string referer, string data)
		{
			using (var client = BuildClient(referer))
			{
				return await client.PostAsync(url, new StringContent(data));
			}
		}

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

		public async Task<DirectoryInfo> DownloadAndExportProject(long projectID, DirectoryInfo outputDirectory)
		{
			var projectInfoTask = GetProjectInfo(projectID);
			var projectDataTask = DownloadProject(projectID);

			await Task.WhenAll(projectInfoTask, projectDataTask);


			var dir = await ExportProject(projectInfoTask.Result, projectDataTask.Result, outputDirectory);
			return dir;
		}

		public async Task<DownloadedProject> DownloadProject(long projectID)
		{
			using (var projectData = await DownloadData($"https://projects.scratch.mit.edu/internalapi/project/{projectID}/get/", $"https://scratch.mit.edu/projects/{projectID}/"))
			{
				using (var content = projectData.Content)
				{
					var data = await content.ReadAsByteArrayAsync();


					DownloadedProject result = DownloadSB1Project(projectID, data);
					if (result != null)
					{
						return result;
					}

					result = await DownloadSB2Project(projectID, data);
					if (result != null)
					{
						return result;
					}

					return await DownloadSB3Project(projectID);
				}
			}
		}

		public async Task<DirectoryInfo> ExportProject(ProjectInfo info, DownloadedProject project, DirectoryInfo outputDirectory)
		{
			Console.WriteLine("Downloading: " + info.title);

			var downloadedProject = await DownloadProject(info.id);

			if (downloadedProject == null)
			{
				return null;
			}

			Console.WriteLine("Exporting: " + info.title);

			string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

			var filteredName = info.title;

			foreach (char c in invalidChars)
			{
				filteredName = filteredName.Replace(c.ToString(), "");
			}

			var projectSubDir = outputDirectory.CreateSubdirectory(filteredName);

			if (!projectSubDir.Exists)
			{
				projectSubDir.Create();
			}

			downloadedProject.ExportProject(projectSubDir, filteredName);

			Console.WriteLine("Completed: " + info.title);

			var template = new StringBuilder(Encoding.UTF8.GetString(Resources.Resource.Project_Template));

			//var projectInfoType = typeof(ProjectInfo);

			/*foreach (var field in projectInfoType.GetFields())
			{
				var value = field.GetValue(project);
				template = template.Replace("%" + field.Name.ToLower() + "%", value == null ? "N/A" : value.ToString());
			}*/

			var projectInfoJson = JObject.FromObject(info);

			var matches = Regex.Matches(template.ToString(), @"%(.+?)%");

			for (int i = matches.Count - 1; i >= 0; i--)
			{
				var match = matches[i];
				if (match.Success)
				{
					var group = match.Groups[1];
					var placeholderPaths = group.Value.Split('.');

					JToken currentToken = projectInfoJson;

					foreach (var path in placeholderPaths)
					{
						currentToken = currentToken[path];
						if (currentToken == null)
						{
							continue;
						}
					}
					template.Remove(group.Index,group.Length);
					template.Insert(group.Index, currentToken.Value<string>());
				}
			}

			var image = await DownloadProjectImage(info.id);

			var imageFileName = Regex.Match(info.image, @"(\d{3,}_\d{2,}x\d{2,}\..*$)").Groups[1].Value;

			var imagePath = Utilities.PathAddBackslash(projectSubDir.FullName) + imageFileName;

			await File.WriteAllBytesAsync(imagePath, image);

			template.Replace("%thumbnail%", imageFileName);

			await File.WriteAllTextAsync(Utilities.PathAddBackslash(projectSubDir.FullName) + "Info.md", template.ToString());

			return outputDirectory;
		}

		static async Task<byte[]> DownloadFromURL(string url)
		{
			using (var webClient = new WebClient())
			{
				return await webClient.DownloadDataTaskAsync(new Uri(url));
			}
		}

		static Task<byte[]> DownloadProjectImage(long projectID)
		{
			return DownloadFromURL($"https://cdn2.scratch.mit.edu/get_image/project/{projectID}_480x360.png");
		}

		SB1Project DownloadSB1Project(long projectID, byte[] data)
		{
			var magic = "ScratchV0".ToCharArray();
			if (CheckSpan(data.AsSpan(0, magic.GetLength(0)), magic))
			{
				return new SB1Project
				{
					Data = data
				};
			}
			return null;
		}

		async Task<SB2Project> DownloadSB2Project(long projectID, byte[] data)
		{
			try
			{
				string s = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
				var json = JObject.Parse(s);
				return await SB2JsonProject(projectID, json);
			}
			catch (Exception)
			{
				return SB2BinaryProject(projectID, data);
			}
		}


		async Task<SB3Project> DownloadSB3Project(long projectID)
		{
			string PROJECTS_API = $"https://projects.scratch.mit.edu/{projectID}";
			string REFERER = $"https://scratch.mit.edu/projects/{projectID}/";
			string ASSETS_API = "https://assets.scratch.mit.edu/internalapi/asset/$path/get/";


			List<SB3Project.SB3File> files = new List<SB3Project.SB3File>();

			using (var projectResponse = await DownloadData(PROJECTS_API, REFERER))
			{
				var json = JObject.Parse(await projectResponse.Content.ReadAsStringAsync());

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

					using (var response = await DownloadData(ASSETS_API.Replace("$path",path),REFERER))
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
				foreach (var costume in json["costumes"])
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

		async Task<SB2Project> SB2JsonProject(long projectID, JObject json)
		{
			SB2Project project = new SB2Project();

			List<string> ImageExtensions = new List<string>
			{
				"svg",
				"png"
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

				foreach (var reference in asset.Value.Append(asset.Key))
				{
					if (reference is CostumeMetaAsset costumeAsset)
					{
						costumeAsset.BaseLayerID = accumulator;
						costumeAsset.Parent["baseLayerID"] = accumulator;
						Console.WriteLine("Setting Costume \"" + costumeAsset.CostumeName + "\" ID: " + accumulator);
					}
					else if (reference is SoundMetaAsset soundAsset)
					{
						soundAsset.SoundID = accumulator;
						soundAsset.Parent["soundID"] = accumulator;
						Console.WriteLine("Setting Sound \"" + soundAsset.SoundName + "\" ID: " + accumulator);
					}
					else if (reference is PenMetaAsset penAsset)
					{
						penAsset.PenLayerID = accumulator;
						penAsset.Parent["penLayerID"] = accumulator;
						Console.WriteLine("Setting Pen \"" + penAsset.MD5 + "\" ID: " + accumulator);
					}
				}


				var assetResult = await DownloadData($"https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/", $"https://scratch.mit.edu/projects/{projectID}/");
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
	}
}
