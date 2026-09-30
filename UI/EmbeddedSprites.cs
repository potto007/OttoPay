using System.IO;
using System.Reflection;

namespace OttoPay.UI;

/// Sprites built from images embedded in the mod DLL.
internal static class EmbeddedSprites
{
    /// UnityEngine.ImageConversionModule cannot be referenced from net48: its metadata names
    /// ReadOnlySpan<byte>, which lives in the game's Mono mscorlib and not in the net48
    /// reference assemblies. The byte[] overload of LoadImage still exists, so it is bound once
    /// at startup instead.
    private static readonly MethodInfo? LoadImageMethod = AccessTools.Method(
        "UnityEngine.ImageConversion:LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });

    internal static Sprite Load(string name)
    {
        Texture2D texture = LoadTexture(name);
        return texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero) : null!;
    }

    private static Texture2D LoadTexture(string name)
    {
        Texture2D texture = new(0, 0);
        if (LoadImageMethod == null)
        {
            Log.Error("UnityEngine.ImageConversion.LoadImage was not found. Textures will not load.");
            return texture;
        }

        LoadImageMethod.Invoke(null, new object[] { texture, ReadBytes("assets." + name) });
        return texture;
    }

    private static byte[] ReadBytes(string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using MemoryStream stream = new();
        assembly.GetManifestResourceStream(assembly.GetName().Name + "." + name)!.CopyTo(stream);
        return stream.ToArray();
    }
}
