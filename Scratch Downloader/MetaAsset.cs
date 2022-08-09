using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Scratch_Downloader
{
	public abstract class MetaAsset
	{
		Regex fileNameMatcher = new Regex(@"^(.+?)\.", RegexOptions.Singleline | RegexOptions.Compiled);
		Regex fileExtensionMatcher = new Regex(@"\.(.*?)$", RegexOptions.Singleline | RegexOptions.Compiled);

		static MetaAsset()
		{

		}

		public class Comparer : IEqualityComparer<MetaAsset>
		{
			public static Comparer Default = new Comparer();
			public bool Equals([AllowNull] MetaAsset x, [AllowNull] MetaAsset y)
			{
				return x.MD5 == y.MD5;
			}

			public int GetHashCode([DisallowNull] MetaAsset obj)
			{
				return obj.MD5.GetHashCode();
			}
		}

		public string MD5;

		public JToken Parent;

		public string GetFileName()
		{
			//return Regex.Match(MD5, @"^(.+?)\.").Groups[1].Value;
			return fileNameMatcher.Match(MD5).Groups[1].Value;
		}

		public string GetExtension()
		{
			//return Regex.Match(MD5, @"\.(.*?)$").Groups[1].Value;
			return fileExtensionMatcher.Match(MD5).Groups[1].Value;
		}

		public override bool Equals(object obj)
		{
			return obj is MetaAsset asset && asset.MD5 == MD5;
		}

		public override int GetHashCode()
		{
			return MD5.GetHashCode();
		}

		public override string ToString()
		{
			return MD5;
		}
	}

	public class SoundMetaAsset : MetaAsset
	{
		public string SoundName;
		public int SoundID;
		public int SampleCount;
		public int SampleRate;
		public string Format;
	}

	public class CostumeMetaAsset : MetaAsset
	{
		public string CostumeName;
		public int BaseLayerID;
		public int? BitmapResolution;
		public int RotationCenterX;
		public int RotationCenterY;
	}

	public class PenMetaAsset : MetaAsset
	{
		public int PenLayerID;
	}



}
