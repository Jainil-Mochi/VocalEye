using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Infrastructure;
using VocalEye.Infrastructure.Persistence;
using Xunit;

namespace VocalEye.UnitTests;

public class SqlitePersistenceTests
{
    [Fact]
    public async Task SaveThenList_UsesSqlite_NewestConversationFirstAndMessagesInOrder()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<VocalEyeDbContext> options = new DbContextOptionsBuilder<VocalEyeDbContext>().UseSqlite(connection).Options;

        await using (VocalEyeDbContext setup = new VocalEyeDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid older = Guid.NewGuid();
        Guid newer = Guid.NewGuid();
        await SaveAsync(options, older, "first chat");
        await Task.Delay(20);
        await SaveAsync(options, newer, "second chat");

        await using VocalEyeDbContext readContext = new VocalEyeDbContext(options);
        ConversationQueries queries = new ConversationQueries(readContext);

        IReadOnlyList<ConversationSummaryDto> list = await queries.ListAsync(10, CancellationToken.None);
        IReadOnlyList<StoredMessageDto>? messages = await queries.GetMessagesAsync(older, CancellationToken.None);

        list.Select(item => item.Id).Should().Equal(newer, older);
        list[0].MessageCount.Should().Be(2);
        list[0].ModelProvider.Should().Be("fake");
        messages.Should().NotBeNull();
        messages!.Select(message => message.Sequence).Should().Equal(0, 1);
    }

    [Fact]
    public async Task GetAsync_AfterSave_ReturnsTheStoredHistoryAndModel()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<VocalEyeDbContext> options = new DbContextOptionsBuilder<VocalEyeDbContext>().UseSqlite(connection).Options;
        await using (VocalEyeDbContext setup = new VocalEyeDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid id = Guid.NewGuid();
        await SaveAsync(options, id, "hello");

        await using VocalEyeDbContext context = new VocalEyeDbContext(options);
        ConversationStore store = new ConversationStore(new ConversationRepository(context));
        ConversationState state = await store.GetAsync(id, CancellationToken.None);

        state.Messages.Select(message => message.Text).Should().Equal("hello", "reply to hello");
        state.Model.Should().Be(new ModelSelection("fake", "m1"));
    }

    [Fact]
    public async Task AddPersistence_WithSqliteFile_CreatesFolderAndDatabaseAndServesTheStore()
    {
        string folder = Path.Combine(Path.GetTempPath(), "vocaleye-tests-" + Guid.NewGuid().ToString("N"));
        string file = Path.Combine(folder, "nested", "vocaleye.db");

        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Provider"] = "Sqlite",
                    ["ConnectionStrings:Sqlite"] = $"Data Source={file};Pooling=False",
                })
                .Build();

            ServiceCollection services = new ServiceCollection();
            services.AddPersistence(configuration);
            await using ServiceProvider provider = services.BuildServiceProvider();

            await provider.InitializePersistenceAsync();

            using IServiceScope scope = provider.CreateScope();
            IConversationStore store = scope.ServiceProvider.GetRequiredService<IConversationStore>();
            Guid id = Guid.NewGuid();
            ConversationState state = new ConversationState { Id = id };
            state.Messages.Add(new ChatMessage(ChatRole.User, "saved to a file"));
            await store.SaveAsync(state, CancellationToken.None);

            ConversationState loaded = await store.GetAsync(id, CancellationToken.None);

            File.Exists(file).Should().BeTrue();
            loaded.Messages.Should().ContainSingle().Which.Text.Should().Be("saved to a file");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    [Fact]
    public void AddPersistence_UnknownProvider_ThrowsAndNamesTheValidOnes()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Persistence:Provider"] = "Mongo" })
            .Build();

        Action act = () => new ServiceCollection().AddPersistence(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Postgres*Sqlite*InMemory*");
    }

    [Fact]
    public void CreateDefault_PointsAtAVocalEyeFolder()
    {
        string connection = SqliteConnectionStrings.CreateDefault();

        connection.Should().StartWith("Data Source=").And.EndWith("vocaleye.db");
        connection.Should().Contain("VocalEye");
    }

    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=file:memdb1?mode=memory&cache=shared")]
    public void EnsureDirectoryExists_NonFileSources_DoNothing(string connection)
    {
        Action act = () => SqliteConnectionStrings.EnsureDirectoryExists(connection);

        act.Should().NotThrow();
    }

    private static async Task SaveAsync(DbContextOptions<VocalEyeDbContext> options, Guid id, string text)
    {
        await using VocalEyeDbContext context = new VocalEyeDbContext(options);
        ConversationStore store = new ConversationStore(new ConversationRepository(context));
        ConversationState state = new ConversationState { Id = id, Model = new ModelSelection("fake", "m1") };
        state.Messages.Add(new ChatMessage(ChatRole.User, text));
        state.Messages.Add(new ChatMessage(ChatRole.Assistant, "reply to " + text.Split(' ')[0]));
        await store.SaveAsync(state, CancellationToken.None);
    }
}
