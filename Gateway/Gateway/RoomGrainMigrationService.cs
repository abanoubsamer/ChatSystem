using Application.Abstractions.Connection.Grains;
using Domain.Models;
using MongoDB.Driver;

namespace AppGateway
{
    public sealed class RoomGrainMigrationService : IHostedService
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IMongoCollection<ChatMember> _chatMembers;
        private readonly ILogger<RoomGrainMigrationService> _logger;

        public RoomGrainMigrationService(
            IGrainFactory grainFactory,
            IMongoDatabase database,
            ILogger<RoomGrainMigrationService> logger)
        {
            _grainFactory = grainFactory;
            _chatMembers = database.GetCollection<ChatMember>("ChatMember");
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            // اتأكد إن الـ migration ما اتعملتش قبل كده
            var migrationFlag = _grainFactory
                .GetGrain<IMigrationFlagGrain>("room_migration");

            if (await migrationFlag.IsDoneAsync())
            {
                _logger.LogInformation("RoomGrain migration already done — skipping");
                return;
            }

            _logger.LogInformation("Starting RoomGrain migration...");

            // جيب كل الـ ChatMembers اللي مش left
            // ChatMember بيحتوي على UserId + ChatId — ده كل اللي محتاجه
            var members = await _chatMembers
                .Find(m => m.LeftAt == null)   // بس الأعضاء الحاليين
                .Project(m => new { m.ChatId, m.UserId })
                .ToListAsync(ct);

            _logger.LogInformation(
                "Found {Count} active memberships to migrate", members.Count);

            // جمّع الـ members grouped by ChatId
            var grouped = members
                .GroupBy(m => m.ChatId)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(m => m.UserId.ToString()).ToList());

            _logger.LogInformation(
                "Migrating {ChatCount} chats...", grouped.Count);

            // لكل chat — ضيف الـ members في RoomGrain
            var tasks = grouped.Select(kvp =>
                MigrateChatAsync(kvp.Key, kvp.Value, ct));

            await Task.WhenAll(tasks);

            await migrationFlag.SetDoneAsync();

            _logger.LogInformation("RoomGrain migration completed ✅");
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

        // ─── Private ──────────────────────────────────────────────────────────

        private async Task MigrateChatAsync(
            string chatId,
            List<string> userIds,
            CancellationToken ct)
        {
            try
            {
                var roomGrain = _grainFactory.GetGrain<IRoomGrain>(chatId);
                var existing = await roomGrain.GetMembersAsync();

                // بس الـ members اللي مش موجودين في الـ Grain
                var toAdd = userIds
                    .Where(id => !existing.Contains(id))
                    .ToList();

                if (toAdd.Count == 0) return;

                await Task.WhenAll(toAdd.Select(userId =>
                    roomGrain.JoinAsync(userId)));

                _logger.LogDebug(
                    "Migrated chat {ChatId} | added {Count} members",
                    chatId, toAdd.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to migrate chat {ChatId}", chatId);
            }
        }
    }

}
