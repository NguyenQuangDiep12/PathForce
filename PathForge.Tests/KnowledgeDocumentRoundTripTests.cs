using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PathForge.Application.KnowledgeDocuments;
using PathForge.Desktop.Services;
using PathForge.Domain.Entities;
using PathForge.Infrastructure.FileStorage;
using PathForge.Infrastructure.Persistence;
using PathForge.Infrastructure.Repositories;
using Xunit;

namespace PathForge.Tests;

public sealed class KnowledgeDocumentRoundTripTests
{
    [Fact]
    public async Task KnowledgeDocumentCanBeSavedSearchedAndRestoredWithFormattingAndImage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PathForgeDbContext>().UseSqlite(connection).Options;
        await using (var context = new PathForgeDbContext(options))
            await context.Database.MigrateAsync();

        var repository = new EfKnowledgeDocumentRepository(new TestContextFactory(options));
        var service = new KnowledgeDocumentService(repository);
        var serialized = OnSta(() => KnowledgeDocumentCodec.Serialize(CreateFormattedDocument()));

        var save = await service.SaveAsync(null, "Ghi chú về SQLite", serialized);
        Assert.True(save.Succeeded);
        Assert.NotNull(save.Id);

        var search = await service.SearchAsync("sqlite");
        Assert.Single(search);
        Assert.Equal("Ghi chú về SQLite", search[0].Title);

        var fetched = await service.GetAsync(save.Id!.Value);
        Assert.NotNull(fetched);
        Assert.Equal(serialized, fetched!.Content);
        var updatedPackage = OnSta(() =>
        {
            var restored = KnowledgeDocumentCodec.Deserialize(fetched.Content);
            var plainText = new TextRange(restored.ContentStart, restored.ContentEnd).Text;
            Assert.Contains("Giới thiệu", plainText);
            Assert.Contains("Định dạng mẫu", plainText);
            Assert.Contains("Heading cấp một", plainText);
            Assert.Contains("Heading cấp hai", plainText);
            Assert.Contains("Heading cấp ba", plainText);
            Assert.Contains("mục bullet", plainText);
            Assert.Contains("mục đánh số", plainText);
            Assert.Contains("sau ảnh", plainText);

            var heading = restored.Blocks.OfType<Paragraph>().Single(paragraph =>
                new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Contains("Heading cấp một"));
            Assert.Equal(22d, heading.FontSize);
            Assert.Equal(FontWeights.Bold, heading.FontWeight);
            Assert.Equal(2, FindImages(restored.Blocks).Count);

            var headingTwo = restored.Blocks.OfType<Paragraph>().Single(paragraph =>
                new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Contains("Heading cấp hai"));
            var headingThree = restored.Blocks.OfType<Paragraph>().Single(paragraph =>
                new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Contains("Heading cấp ba"));
            Assert.Equal(18d, headingTwo.FontSize);
            Assert.Equal(15d, headingThree.FontSize);
            var styledRun = restored.Blocks.OfType<Paragraph>().SelectMany(paragraph => paragraph.Inlines)
                .OfType<Run>().Single(run => run.Text == "Định dạng mẫu");
            Assert.Equal(FontWeights.Bold, styledRun.FontWeight);
            Assert.Equal(FontStyles.Italic, styledRun.FontStyle);
            Assert.True(styledRun.TextDecorations?.Count > 0);
            Assert.Equal(Colors.Yellow, ((SolidColorBrush)styledRun.Background).Color);
            Assert.Equal(Colors.DarkBlue, ((SolidColorBrush)styledRun.Foreground).Color);

            var bullet = restored.Blocks.OfType<List>().Single(list => list.MarkerStyle == TextMarkerStyle.Disc);
            Assert.Single(bullet.ListItems);
            var numbered = restored.Blocks.OfType<List>().Single(list => list.MarkerStyle == TextMarkerStyle.Decimal);
            Assert.Single(numbered.ListItems);

            var middleItem = bullet.ListItems.Single().Blocks.OfType<Paragraph>().Single().Inlines.OfType<Run>().Single();
            middleItem.Text = "mục bullet đã chỉnh";
            var serializedAgain = KnowledgeDocumentCodec.Serialize(restored);
            var restoredAgain = KnowledgeDocumentCodec.Deserialize(serializedAgain);
            Assert.Equal(2, FindImages(restoredAgain.Blocks).Count);
            var textAfterEdit = new TextRange(restoredAgain.ContentStart, restoredAgain.ContentEnd).Text;
            Assert.Contains("mục bullet đã chỉnh", textAfterEdit);
            Assert.Contains("Heading cấp một", textAfterEdit);
            Assert.Contains("sau ảnh", textAfterEdit);
            return serializedAgain;
        });

        var secondSave = await service.SaveAsync(save.Id, "Ghi chú về SQLite", updatedPackage);
        Assert.True(secondSave.Succeeded);
        Assert.Equal(save.Id, secondSave.Id);
        Assert.Single(await service.SearchAsync("GHI CHÚ"));
        var savedAgain = await service.GetAsync(save.Id.Value);
        Assert.NotNull(savedAgain);
        Assert.Equal(fetched.CreatedAt, savedAgain!.CreatedAt);
        Assert.Equal(updatedPackage, savedAgain.Content);
    }

    [Fact]
    public async Task KnowledgeDocumentTableHasNoForeignKeysToBusinessTables()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PathForgeDbContext>().UseSqlite(connection).Options;
        await using var context = new PathForgeDbContext(options);
        await context.Database.MigrateAsync();

        var foreignKeys = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_list('KnowledgeDocuments');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            foreignKeys.Add(reader.GetString(2));

        Assert.Empty(foreignKeys);
        Assert.Equal(0, await context.KnowledgeDocuments.CountAsync());
    }

    [Fact]
    public async Task ManagedImageIsDeduplicatedAndSurvivesSourceRemoval()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PathForge.KnowledgeImages.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var source = Path.Combine(folder, "source.png");
            var secondSource = Path.Combine(folder, "second-copy.png");
            var bytes = OnSta(CreatePngBytes);
            await File.WriteAllBytesAsync(source, bytes);
            await File.WriteAllBytesAsync(secondSource, bytes);
            var storage = new LocalKnowledgeImageStorage(Path.Combine(folder, "managed"));

            var first = await storage.StoreAsync(source);
            File.Delete(source);
            var second = await storage.StoreAsync(secondSource);

            Assert.Equal(first.RelativePath, second.RelativePath);
            Assert.True(File.Exists(first.FullPath));
            Assert.Equal(bytes.Length, new FileInfo(first.FullPath).Length);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task ImageLoadedFromManagedCopySurvivesOriginalSourceDeletionAndDatabaseReload()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PathForge.KnowledgeImageRoundTrip.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var source = Path.Combine(folder, "original.png");
            var managedDirectory = Path.Combine(folder, "PathForgeImages");
            await File.WriteAllBytesAsync(source, OnSta(CreatePngBytes));
            var storage = new LocalKnowledgeImageStorage(managedDirectory);
            var managed = await storage.StoreAsync(source);

            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<PathForgeDbContext>().UseSqlite(connection).Options;
            await using (var context = new PathForgeDbContext(options))
                await context.Database.MigrateAsync();
            var service = new KnowledgeDocumentService(new EfKnowledgeDocumentRepository(new TestContextFactory(options)));

            var package = OnSta(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(managed.FullPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                var document = KnowledgeDocumentCodec.CreateEmptyDocument();
                var paragraph = (Paragraph)document.Blocks.FirstBlock!;
                paragraph.Inlines.Add(new Run("Trước ảnh"));
                paragraph.Inlines.Add(new InlineUIContainer(new Image { Source = bitmap, Width = 24, Height = 24 }));
                document.Blocks.Add(new Paragraph(new Run("Sau ảnh")));
                return KnowledgeDocumentCodec.Serialize(document);
            });

            var saved = await service.SaveAsync(null, "Ảnh minh họa", package);
            Assert.True(saved.Succeeded);
            File.Delete(source);
            Assert.True(File.Exists(managed.FullPath));
            var reopened = await service.GetAsync(saved.Id!.Value);
            Assert.NotNull(reopened);
            OnSta(() =>
            {
                var restored = KnowledgeDocumentCodec.Deserialize(reopened!.Content);
                var images = FindImages(restored.Blocks);
                Assert.Single(images);
                Assert.Contains("Trước ảnh", new TextRange(restored.ContentStart, restored.ContentEnd).Text);
                Assert.Contains("Sau ảnh", new TextRange(restored.ContentStart, restored.ContentEnd).Text);
                return true;
            });
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    private static FlowDocument CreateFormattedDocument()
    {
        var document = KnowledgeDocumentCodec.CreateEmptyDocument();
        var introduction = (Paragraph)document.Blocks.FirstBlock!;
        introduction.Inlines.Add(new Run("Giới thiệu"));
        introduction.Inlines.Add(new Run(" · "));
        introduction.Inlines.Add(new Run("Định dạng mẫu")
        {
            FontWeight = FontWeights.Bold,
            FontStyle = FontStyles.Italic,
            TextDecorations = System.Windows.TextDecorations.Underline,
            Foreground = Brushes.DarkBlue,
            Background = Brushes.Yellow
        });

        var heading = new Paragraph(new Run("Heading cấp một"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(49, 94, 82))
        };
        document.Blocks.Add(heading);

        document.Blocks.Add(new Paragraph(new Run("Heading cấp hai"))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        });
        document.Blocks.Add(new Paragraph(new Run("Heading cấp ba"))
        {
            FontSize = 15,
            FontWeight = FontWeights.SemiBold
        });

        var list = new List { MarkerStyle = TextMarkerStyle.Disc };
        list.ListItems.Add(new ListItem(new Paragraph(new Run("mục bullet"))));
        document.Blocks.Add(list);

        var numbered = new List { MarkerStyle = TextMarkerStyle.Decimal };
        numbered.ListItems.Add(new ListItem(new Paragraph(new Run("mục đánh số"))));
        document.Blocks.Add(numbered);

        var image = new Image { Source = CreateBitmap(), Width = 18, Height = 18, Stretch = System.Windows.Media.Stretch.Uniform };
        document.Blocks.Add(new Paragraph(new InlineUIContainer(image)));
        document.Blocks.Add(new Paragraph(new Run("sau ảnh")));
        var secondImage = new Image { Source = CreateBitmap(), Width = 18, Height = 18, Stretch = System.Windows.Media.Stretch.Uniform };
        document.Blocks.Add(new Paragraph(new InlineUIContainer(secondImage)));
        return document;
    }

    private static IReadOnlyList<Image> FindImages(BlockCollection blocks)
    {
        var images = new List<Image>();
        foreach (var block in blocks)
        {
            if (block is Paragraph paragraph)
            {
                images.AddRange(paragraph.Inlines.OfType<InlineUIContainer>()
                    .Select(container => container.Child).OfType<Image>());
            }
            else if (block is List list)
            {
                foreach (var item in list.ListItems)
                    images.AddRange(FindImages(item.Blocks));
            }
            else if (block is Section section)
            {
                images.AddRange(FindImages(section.Blocks));
            }
        }
        return images;
    }

    private static BitmapSource CreateBitmap()
    {
        var pixels = new byte[]
        {
            255, 0, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 255, 255, 255, 255
        };
        var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte[] CreatePngBytes()
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(CreateBitmap()));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static T OnSta<T>(Func<T> action)
    {
        T? result = default;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { result = action(); }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        return result!;
    }

    private sealed class TestContextFactory : IDbContextFactory<PathForgeDbContext>
    {
        private readonly DbContextOptions<PathForgeDbContext> _options;
        public TestContextFactory(DbContextOptions<PathForgeDbContext> options) => _options = options;
        public PathForgeDbContext CreateDbContext() => new(_options);
    }
}
