using System.Drawing;
using System.Drawing.Imaging;

var argsPaths = args.Length == 0
    ? Array.Empty<string>()
    : args;

if (argsPaths.Length == 0)
{
    Console.WriteLine("画像を含むフォルダをドラッグ＆ドロップしてください。\n" +
                      "例: ImageSplitter.exe C:\\Images");
    return;
}

var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"
};

var imageFiles = new List<string>();
foreach (var path in argsPaths)
{
    if (Directory.Exists(path))
    {
        var files = Directory.EnumerateFiles(path)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file)));
        imageFiles.AddRange(files);
    }
    else if (File.Exists(path) && supportedExtensions.Contains(Path.GetExtension(path)))
    {
        imageFiles.Add(path);
    }
    else
    {
        Console.WriteLine($"対象外: {path}");
    }
}

if (imageFiles.Count == 0)
{
    Console.WriteLine("処理対象の画像が見つかりませんでした。");
    return;
}

foreach (var file in imageFiles)
{
    try
    {
        SplitImageVertically(file);
        Console.WriteLine($"完了: {file}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"失敗: {file} ({ex.Message})");
    }
}

static void SplitImageVertically(string filePath)
{
    using var image = Image.FromFile(filePath);
    var width = image.Width;
    var height = image.Height;

    if (width < 2)
    {
        throw new InvalidOperationException("画像の幅が小さすぎます。");
    }

    var leftWidth = width / 2;
    var rightWidth = width - leftWidth;

    using var leftBitmap = new Bitmap(leftWidth, height);
    using var rightBitmap = new Bitmap(rightWidth, height);

    using (var graphicsLeft = Graphics.FromImage(leftBitmap))
    using (var graphicsRight = Graphics.FromImage(rightBitmap))
    {
        graphicsLeft.DrawImage(image,
            new Rectangle(0, 0, leftWidth, height),
            new Rectangle(0, 0, leftWidth, height),
            GraphicsUnit.Pixel);

        graphicsRight.DrawImage(image,
            new Rectangle(0, 0, rightWidth, height),
            new Rectangle(leftWidth, 0, rightWidth, height),
            GraphicsUnit.Pixel);
    }

    var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
    var extension = Path.GetExtension(filePath);
    var baseName = Path.GetFileNameWithoutExtension(filePath);
    var leftPath = GetAvailablePath(directory, $"{baseName}_left", extension);
    var rightPath = GetAvailablePath(directory, $"{baseName}_right", extension);

    var format = GetImageFormat(extension);
    leftBitmap.Save(leftPath, format);
    rightBitmap.Save(rightPath, format);
}

static string GetAvailablePath(string directory, string baseName, string extension)
{
    var candidate = Path.Combine(directory, baseName + extension);
    if (!File.Exists(candidate))
    {
        return candidate;
    }

    var index = 1;
    while (true)
    {
        var numbered = Path.Combine(directory, $"{baseName}_{index}{extension}");
        if (!File.Exists(numbered))
        {
            return numbered;
        }
        index++;
    }
}

static ImageFormat GetImageFormat(string extension)
{
    return extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => ImageFormat.Jpeg,
        ".png" => ImageFormat.Png,
        ".bmp" => ImageFormat.Bmp,
        ".gif" => ImageFormat.Gif,
        ".tif" or ".tiff" => ImageFormat.Tiff,
        _ => ImageFormat.Png
    };
}
