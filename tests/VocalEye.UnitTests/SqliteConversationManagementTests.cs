using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Infrastructure;
using VocalEye.Infrastructure.Persistence;
using Xunit;

namespace VocalEye.UnitTests;

public class SqliteConversationManagementTests
{
    [Fact]
    public async Task RenameThenDelete_OnSqlite_ChangesTheListAndRemovesTheMessages()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<VocalEyeDbContext> options = new DbContextOptionsBuilder<VocalEyeDbContext>().UseSqlite(connection).Options;
        await using (VocalEyeDbContext setup = new VocalEyeDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        Guid id = Guid.NewGuid();
        await using (VocalEyeDbContext writeContext = new VocalEyeDbContext(options))
        {
            ConversationStore store = new ConversationStore(new ConversationRepository(writeContext));
            ConversationState state = new ConversationState { Id = id };
            state.Messages.Add(new ChatMessage(ChatRole.User, "first message of the chat"));
            state.Messages.Add(new ChatMessage(ChatRole.Assistant, "answer"));
            await store.SaveAsync(state, CancellationToken.None);
        }

        await using VocalEyeDbContext context = new VocalEyeDbContext(options);
        ConversationManager manager = new ConversationManager(new ConversationRepository(context));
        ConversationQueries queries = new ConversationQueries(context);

        IReadOnlyList<ConversationSummaryDto> before = await queries.ListAsync(10, CancellationToken.None);
        bool renamed = await manager.RenameAsync(id, "Renamed chat", CancellationToken.None);
        IReadOnlyList<ConversationSummaryDto> afterRename = await queries.ListAsync(10, CancellationToken.None);
        bool deleted = await manager.DeleteAsync(id, CancellationToken.None);
        IReadOnlyList<ConversationSummaryDto> afterDelete = await queries.ListAsync(10, CancellationToken.None);
        int leftoverMessages = await context.Messages.CountAsync();

        before.Single().Title.Should().Be("first message of the chat");
        renamed.Should().BeTrue();
        afterRename.Single().Title.Should().Be("Renamed chat");
        deleted.Should().BeTrue();
        afterDelete.Should().BeEmpty();
        leftoverMessages.Should().Be(0);
    }

    [Fact]
    public async Task Initialize_DatabaseCreatedBeforeTitlesExisted_AddsTheColumnAndKeepsTheData()
    {
        string folder = Path.Combine(Path.GetTempPath(), "vocaleye-tests-" + Guid.NewGuid().ToString("N"));
        string file = Path.Combine(folder, "vocaleye.db");

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
            Guid id = Guid.NewGuid();
            using (IServiceScope scope = provider.CreateScope())
            {
                IConversationStore store = scope.ServiceProvider.GetRequiredService<IConversationStore>();
                ConversationState state = new ConversationState { Id = id };
                state.Messages.Add(new ChatMessage(ChatRole.User, "old chat"));
                await store.SaveAsync(state, CancellationToken.None);

                // Make the file look like one created by the previous version.
                VocalEyeDbContext context = scope.ServiceProvider.GetRequiredService<VocalEyeDbContext>();
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Conversations\" DROP COLUMN \"Title\";");
            }

            await provider.InitializePersistenceAsync();

            using IServiceScope check = provider.CreateScope();
            IConversationQueries queries = check.ServiceProvider.GetRequiredService<IConversationQueries>();
            IReadOnlyList<ConversationSummaryDto> list = await queries.ListAsync(10, CancellationToken.None);

            list.Should().ContainSingle().Which.Id.Should().Be(id);
            list[0].Title.Should().BeNull();
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
}
