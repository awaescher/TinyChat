using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace TinyChat;

/// <summary>Retrieves Windows file-type icons without opening or creating attachment files.</summary>
internal static class WindowsFileIconFactory
{
	private static readonly ConcurrentDictionary<string, Lazy<Task<byte[]>>> _icons = new(StringComparer.OrdinalIgnoreCase);

	internal static async Task<Image> CreateAsync(string fileName)
	{
		var extension = Path.GetExtension(fileName);
		var data = await _icons.GetOrAdd(extension, static type => new Lazy<Task<byte[]>>(() => Task.Run(() => Load(type)))).Value.ConfigureAwait(false);
		using var stream = new MemoryStream(data);
		using var image = Image.FromStream(stream);
		return new Bitmap(image);
	}

	private static byte[] Load(string extension)
	{
		// Shell APIs require COM initialization, including on background pool threads.
		var initialized = CoInitializeEx(0, 0) >= 0;
		var info = new ShellFileInfo();
		try
		{
			const uint FILE_ATTRIBUTE_NORMAL = 0x80;
			const uint SHGFI_ICON = 0x100;
			const uint SHGFI_USEFILEATTRIBUTES = 0x10;
			SHGetFileInfo(string.IsNullOrEmpty(extension) ? "file" : extension, FILE_ATTRIBUTE_NORMAL, out info, (uint)Marshal.SizeOf<ShellFileInfo>(), SHGFI_ICON | SHGFI_USEFILEATTRIBUTES);
			using var icon = info.Icon == 0 ? (Icon)SystemIcons.Application.Clone() : Icon.FromHandle(info.Icon);
			using var bitmap = icon.ToBitmap();
			using var stream = new MemoryStream();
			bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
			return stream.ToArray();
		}
		finally
		{
			if (info.Icon != 0)
				DestroyIcon(info.Icon);
			if (initialized)
				CoUninitialize();
		}
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct ShellFileInfo
	{
		internal nint Icon;
		internal int Index;
		internal uint Attributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		internal string DisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		internal string TypeName;
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
	private static extern nuint SHGetFileInfo(string path, uint attributes, out ShellFileInfo info, uint size, uint flags);
	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DestroyIcon(nint icon);
	[DllImport("ole32.dll")]
	private static extern int CoInitializeEx(nint reserved, uint mode);
	[DllImport("ole32.dll")]
	private static extern void CoUninitialize();
}
