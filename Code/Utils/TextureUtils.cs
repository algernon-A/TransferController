using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	internal static class TextureUtils
	{
		// Dictionary to cache texture atlas lookups.
		private readonly static Dictionary<string, UITextureAtlas> textureCache = new Dictionary<string, UITextureAtlas>();

		// Dictionary to cache texture file lookups.
		private readonly static Dictionary<string, UITextureAtlas> fileCache = new Dictionary<string, UITextureAtlas>();


		/// <summary>
		/// Loads a cursor texture.
		/// </summary>
		/// <param name="cursorName">Cursor texture file name</param>
		/// <returns>New cursor</returns>
		internal static CursorInfo LoadCursor(string cursorName)
		{
			CursorInfo cursor = ScriptableObject.CreateInstance<CursorInfo>();

			cursor.m_texture = LoadTexture(cursorName);
			cursor.m_hotspot = new Vector2(5f, 0f);

			return cursor;
		}


		/// <summary>
		/// Loads a one-sprite texture from a given .png file.
		/// </summary>
		/// <param name="fileName">Atlas file name (".png" will be appended fto make the filename)</param>
		/// <returns>New texture atlas</returns>
		internal static UITextureAtlas LoadSprite(string fileName)
		{
			// Check if we've already cached this file.
			if (fileCache.ContainsKey(fileName))
			{
				// Cached - return cached result.
				return fileCache[fileName];
			}

			// Create new texture atlas for button.
			UITextureAtlas newAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
			newAtlas.name = fileName;
			newAtlas.material = Object.Instantiate(UIView.GetAView().defaultAtlas.material);

			// Load texture from file.
			Texture2D newTexture = LoadTexture(fileName + ".png");
			newAtlas.material.mainTexture = newTexture;

			// Setup sprite.
			newAtlas.AddSprite(new UITextureAtlas.SpriteInfo
			{
				name = "normal",
				texture = newTexture,
				region = new Rect(0f, 0f, 1f, 1f)
			});

			// Add atlas to cache and return.
			fileCache.Add(fileName, newAtlas);
			return newAtlas;
		}


		/// <summary>
		/// Loads a four-sprite texture atlas from a given .png file.
		/// </summary>
		/// <param name="fileName">Atlas file name (".png" will be appended fto make the filename)</param>
		/// <returns>New texture atlas</returns>
		internal static UITextureAtlas LoadSpriteAtlas(string fileName)
		{
			// Check if we've already cached this file.
			if (fileCache.ContainsKey(fileName))
			{
				// Cached - return cached result.
				return fileCache[fileName];
			}

			// Create new texture atlas for button.
			UITextureAtlas newAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
			newAtlas.name = fileName;
			newAtlas.material = Object.Instantiate(UIView.GetAView().defaultAtlas.material);

			// Load texture from file.
			Texture2D newTexture = LoadTexture(fileName + ".png");
			newAtlas.material.mainTexture = newTexture;

			// Setup sprites.
			string[] spriteNames = new string[] { "disabled", "normal", "pressed", "hovered" };
			int numSprites = spriteNames.Length;
			float spriteWidth = 1f / spriteNames.Length;

			// Iterate through each sprite (counter increment is in region setup).
			for (int i = 0; i < numSprites; ++i)
			{
				UITextureAtlas.SpriteInfo sprite = new UITextureAtlas.SpriteInfo
				{
					name = spriteNames[i],
					texture = newTexture,
					// Sprite regions are horizontally arranged, evenly spaced.
					region = new Rect(i * spriteWidth, 0f, spriteWidth, 1f)
				};
				newAtlas.AddSprite(sprite);
			}

			// Add atlas to cache and return.
			fileCache.Add(fileName, newAtlas);
			return newAtlas;
		}


		/// <summary>
		/// Returns the "ingame" atlas.
		/// </summary>
		internal static UITextureAtlas InGameAtlas => GetTextureAtlas("ingame");


		/// <summary>
		/// Returns a reference to the specified named atlas.
		/// </summary>
		/// <param name="atlasName">Atlas name</param>
		/// <returns>Atlas reference (null if not found)</returns>
		internal static UITextureAtlas GetTextureAtlas(string atlasName)
		{
			// Check if we've already cached this atlas.
			if (textureCache.ContainsKey(atlasName))
			{
				// Cached - return cached result.
				return textureCache[atlasName];
			}

			// No cache entry - get game atlases and iterate through, looking for a name match.
			UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
			for (int i = 0; i < atlases.Length; ++i)
			{
				if (atlases[i].name.Equals(atlasName))
				{
					// Got it - add to cache and return.
					textureCache.Add(atlasName, atlases[i]);
					return atlases[i];
				}
			}

			// IF we got here, we couldn't find the specified atlas.
			return null;
		}


		/// <summary>
		/// Loads a 2D texture from file.
		/// </summary>
		/// <param name="fileName">Texture file to load</param>
		/// <returns>New 2D texture</returns>
		private static Texture2D LoadTexture(string fileName)
		{
			using (Stream stream = OpenResourceFile(fileName))
			{
				// New texture.
				Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
				{
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp
				};

				// Read texture as byte stream from file.
				byte[] array = new byte[stream.Length];
				stream.Read(array, 0, array.Length);
				texture.LoadImage(array);
				texture.Apply();

				return texture;
			}
		}


		/// <summary>
		/// Opens the named resource file for reading.
		/// </summary>
		/// <param name="fileName">File to open</param>
		/// <returns>Read-only file stream</returns>
		private static Stream OpenResourceFile(string fileName)
		{
			string path = Path.Combine(ModUtils.GetAssemblyPath(), "Resources");
			return File.OpenRead(Path.Combine(path, fileName));
		}
	}
}
