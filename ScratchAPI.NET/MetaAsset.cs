
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ScratchDL
{
	public abstract class MetaAsset
	{
		Regex fileNameMatcher = new Regex(@"^(.+?)\.", RegexOptions.Singleline | RegexOptions.Compiled);
		Regex fileExtensionMatcher = new Regex(@"\.(.*?)$", RegexOptions.Singleline | RegexOptions.Compiled);

		public class Comparer : IEqualityComparer<MetaAsset>
		{
			public static Comparer Default = new Comparer();
			public bool Equals([AllowNull] MetaAsset x, [AllowNull] MetaAsset y)
			{
				return x?.MD5 == y?.MD5;
			}

			public int GetHashCode([DisallowNull] MetaAsset obj)
			{
				return obj.MD5.GetHashCode();
			}
		}

		public string MD5;

		public JsonNode Parent;

		public MetaAsset(string md5,JsonNode parent)
        {
			MD5 = md5;
			Parent = parent;
        }

		public string GetFileName()
		{
			return fileNameMatcher.Match(MD5).Groups[1].Value;
		}

		public string GetExtension()
		{
			return fileExtensionMatcher.Match(MD5).Groups[1].Value;
		}

		public override bool Equals(object? obj)
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

        public SoundMetaAsset(string md5, JsonNode parent, string soundName, int soundID, int sampleCount, int sampleRate, string format) : base(md5, parent)
        {
			SoundName = soundName;
			SoundID = soundID;
			SampleCount = sampleCount;
			SampleRate = sampleRate;
			Format = format;
        }
    }

	public class CostumeMetaAsset : MetaAsset
	{
		public string CostumeName;
		public int BaseLayerID;
		public int? BitmapResolution;
		public int RotationCenterX;
		public int RotationCenterY;

        public CostumeMetaAsset(string md5, JsonNode parent, string costumeName, int baseLayerID, int? bitmapResolution, int rotationCenterX, int rotationCenterY) : base(md5, parent)
        {
			CostumeName = costumeName;
			BaseLayerID = baseLayerID;
			BitmapResolution = bitmapResolution;
			RotationCenterX = rotationCenterX;
			RotationCenterY = rotationCenterY;
        }
    }

	public class PenMetaAsset : MetaAsset
	{
		public int PenLayerID;

        public PenMetaAsset(string md5, JsonNode parent, int penLayerID) : base(md5, parent)
        {
			PenLayerID = penLayerID;
        }
    }



}
