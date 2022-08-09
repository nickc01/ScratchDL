/*using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
	public class ScratchAccessor
	{
		Dictionary<string, string> headerValues;
		Dictionary<string, string> cookies;

		public int UserID { get; private set; }
		public bool Banned { get; private set; }
		public string Username { get; private set; }
		public string Token { get; private set; }
		public string ThumbnailURL { get; private set; }
		public string DateJoined { get; private set; }
		public string Email { get; private set; }



		class LoginInfo
		{
			public string username;
			public string password;
			public string csrftoken;
			public string csrfmiddlewaretoken;
			public string captcha_challenge = "";
			public string captcha_response = "";
			public bool useMessages = true;
			public string timezone = "America/New_York";
		}

		HttpClientHandler handler;
		HttpClient client;

		private ScratchAccessor()
		{

		}

		public async Task<ProjectInfoOLD> GetProject(string username, int projectID)
		{
			//
			try
			{
				headerValues.Add("X-Token", "8f14fcc5484943bf957a0e2adf8a682a:rtM12oXok6o61mJcWlAfCIgk_bc");
				//var content = await GetAsync($"https://api.scratch.mit.edu/users/{username}/projects/{projectID}", "https://scratch.mit.edu/");
				var content = await GetAsync($"http://api.scratch.mit.edu/projects/{projectID}", $"https://scratch.mit.edu/projects/{projectID}/");
				using (content)
				{
					var result = await content.Content.ReadAsStringAsync();
					Console.WriteLine("PROJECT GET RESULT = " + result);
					var json = JObject.Parse("{ Project: " + result + "}");

					var p = ((dynamic)json).Project;
					var pNormal = json["Project"];
					var info = new ProjectInfoOLD
					{

					};

					info.ID = projectID;
					info.Title = p.title;
					info.Description = p.description;
					info.Instructions = p.instructions;
					info.Visibility = p.visibility;
					info.Public = pNormal["public"].Value<bool>();
					info.CommentsAllowed = p.comments_allowed;
					info.IsPublished = p.is_published;
					info.AuthorID = p.author.id;
					info.username = username;
					info.ProjectImageURL = p.image;
					info.DateCreated = p.history.created;
					info.DateModified = p.history.modified;
					info.DateShared = p.history.shared;
					info.Views = p.stats.views;
					info.Loves = p.stats.loves;
					info.Favorites = p.stats.favorites;
					info.Remixes = p.stats.remixes;
					info.RemixParentProjectID = pNormal["remix"]["parent"].Type != JTokenType.Null ? p.remix.parent : -1;
					info.RemixRootProjectID = pNormal["remix"]["root"].Type != JTokenType.Null ? p.remix.root : -1;
					return info;
				}
			}
			finally
			{
				headerValues.Remove("X-Token");
			}
		}

		public async Task<byte[]> DownloadFromURL(long projectID)
		{
			using (var webClient = new WebClient())
			{
				return webClient.DownloadData(new Uri($"https://cdn2.scratch.mit.edu/get_image/project/{projectID}_480x360.png"));
			}
		}

		public async IAsyncEnumerable<GalleryProjectInfoOLD> GetProjects()
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

				var content = await GetAsync(url, "https://scratch.mit.edu/mystuff/");
				using (content)
				{
					if (!content.IsSuccessStatusCode)
					{
						yield break;
					}
					else
					{
						var result = await content.Content.ReadAsStringAsync();
						//Console.WriteLine("Result = " + result);
						var json = JObject.Parse("{ Projects: " + result + "}");
						foreach (var pNormal in json["Projects"])
						{
							var g = new GalleryProjectInfoOLD();
							try
							{
								dynamic p = pNormal;

								g.ID = p.pk;
								g.ViewCount = p.fields.view_count;
								g.FavoriteCount = p.fields.favorite_count;
								g.RemixCount = p.fields.remixers_count;
								g.Creator = new User
								{
									Username = p.fields.creator.username,
									ID = p.fields.creator.pk,
									ThumbnailURL = p.fields.creator.thumbnail_url,
									Admin = p.fields.creator.admin
								};
								g.Title = p.fields.title;
								g.IsPublished = p.fields.isPublished;
								g.DateCreated = p.fields.datetime_created;
								g.ThumbnailURL = p.fields.thumbnail_url;
								g.Visibility = p.fields.visibility;
								g.LoveCount = p.fields.love_count;
								g.DateModified = p.fields.datetime_modified;
								g.UncachedThumbnailURL = p.fields.uncached_thumbnail_url;
								g.DateShared = pNormal["fields"]["datetime_shared"].Type != JTokenType.Null ? p.fields.datetime_shared : "null";//p.fields.datetime_shared != null ? p.fields.datetime_shared : "null";
								g.CommentCount = p.fields.commenters_count;
							}
							catch (Exception e)
							{
								Console.WriteLine("ERROR : " + e);
							}
							yield return g;
						}
						pageNumber++;
					}
				}
			}
		}

		public static async Task<ScratchAccessor> Create(string username, string password)
		{
			var accessor = new ScratchAccessor();
			accessor.handler = new HttpClientHandler { UseCookies = false };
			accessor.client = new HttpClient(accessor.handler);

			//CSRF TOKEN REQUEST
			var csrfContent = await accessor.GetAsync("https://scratch.mit.edu/csrf_token/", "https://scratch.mit.edu");
			var csrfToken = "";

			using (csrfContent)
			{
				accessor.cookies = GetCookies(csrfContent);
				foreach (var cookie in accessor.cookies)
				{
					if (cookie.Key == "scratchcsrftoken")
					{
						csrfToken = cookie.Value;
					}
				}
			}

			accessor.cookies.Add("scratchlanguage","en");


			//LOGIN REQUEST
			var login = new LoginInfo()
			{
				username = username,
				password = password,
				csrftoken = csrfToken,
				csrfmiddlewaretoken = csrfToken,
			};

			accessor.headerValues = new Dictionary<string, string>
			{
				{"X-Requested-With", "XMLHttpRequest" },
				{"X-CSRFToken", csrfToken }
			};

			var loginContent = await accessor.PostAsync("https://scratch.mit.edu/login/", "https://scratch.mit.edu", new StringContent(JsonConvert.SerializeObject(login)));
			using (loginContent)
			{
				var loginCookies = GetCookies(loginContent);
				foreach (var cookie in loginCookies)
				{
					if (cookie.Key == "scratchcsrftoken" || cookie.Key == "scratchsessionsid")
					{
						accessor.cookies.Remove(cookie.Key);
						accessor.cookies.Add(cookie.Key, cookie.Value);
						csrfToken = cookie.Value;
					}
				}
			}

			accessor.headerValues.Remove("X-CSRFToken");
			accessor.headerValues.Add("X-CSRFToken", csrfToken);

			//Session Request
			var sessionContent = await accessor.GetAsync("https://scratch.mit.edu/session/", "https://scratch.mit.edu");
			using (sessionContent)
			{
				var result = await sessionContent.Content.ReadAsStringAsync();
				dynamic json = JObject.Parse(result);
				accessor.UserID = json.user.id;
				accessor.Banned = json.user.banned;
				accessor.Username = json.user.username;
				accessor.Token = json.user.token;
				accessor.ThumbnailURL = json.user.thumbnailUrl;
				accessor.DateJoined = json.user.dateJoined;
				accessor.Email = json.user.email;
			}

			return accessor;
		}



		Task<HttpResponseMessage> PostAsync(string url, string referer, HttpContent contents)
		{
			PrepareClient(referer);
			try
			{
				return client.PostAsync(url, contents);
			}
			finally
			{
				ResetClient();
			}
		}

		Task<HttpResponseMessage> GetAsync(string url, string referer)
		{
			PrepareClient(referer);
			try
			{
				return client.GetAsync(url);
			}
			finally
			{
				ResetClient();
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

		public async Task<DownloadedProject> DownloadProject(long projectID)
		{
			using (var projectData = await GetAsync($"https://projects.scratch.mit.edu/internalapi/project/{projectID}/get/", $"https://scratch.mit.edu/projects/{projectID}/"))
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

					//TODO Get Scratch 3.0 working
					return null;
				}
			}
				//var result = await DownloadSB1Project(projectID);

			//return result;
		}

		public SB1Project DownloadSB1Project(long projectID, byte[] data)
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

		public async Task<SB2Project> DownloadSB2Project(long projectID, byte[] data)
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

		public List<MetaAsset> ScanForAssets(JToken json)
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
						BitmapResolution = costume["bitmapResolution"].Value<int>(),
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

		public async Task<SB2Project> SB2JsonProject(long projectID, JObject json)
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

				Console.WriteLine("Adding Asset: " + asset.Key.MD5 + " - ID: " + accumulator);



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


				var assetResult = await GetAsync($"https://cdn.assets.scratch.mit.edu/internalapi/asset/{asset.Key.MD5}/get/", $"https://scratch.mit.edu/projects/{projectID}/");
				using (assetResult)
				{
					var data = await assetResult.Content.ReadAsByteArrayAsync();
					//project.Files.Add(accumulator + "." + asset.Key.GetExtension(),data);
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

			//assets.Distinct(MetaAsset.Comparer.Default);

			return project;

		}

		public SB2Project SB2BinaryProject(long projectID, byte[] data)
		{
			var zipMagic = "PK".ToCharArray();

			if (!CheckSpan(data.AsSpan().Slice(0,2),zipMagic))
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






		private void ResetClient()
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

		private void PrepareClient(string referer)
		{
			//Console.WriteLine("Preparing Client");
			string cookieString = "";
			if (cookies != null)
			{
				foreach (var cookie in cookies)
				{
					//Console.WriteLine("Cookie = " + cookie.Key + "=" + cookie.Value);
					cookieString += cookie.Key + "=" + cookie.Value + ";";
				}
				cookieString.Remove(cookieString.Length - 1);
				client.DefaultRequestHeaders.Add("Cookie", cookieString);
			}

			if (headerValues != null)
			{
				foreach (var pair in headerValues)
				{
					//Console.WriteLine("Header: " + pair.Key + " = " + pair.Value); 
					client.DefaultRequestHeaders.Add(pair.Key, pair.Value);
				}
			}
			client.DefaultRequestHeaders.Add("Referer", referer);
		}

		static Dictionary<string,string> GetCookies(HttpResponseMessage response)
		{
			var cookies = new Dictionary<string, string>();
			foreach (var value in response.Headers.GetValues("Set-Cookie"))
			{
				var match = Regex.Match(value, @"^(.+?)=(.*?);");
				cookies.Add(match.Groups[1].Value, match.Groups[2].Value);
			}
			return cookies;
		}
	}
}
*/