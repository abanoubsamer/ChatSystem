namespace AppGateway
{
    public sealed class MigrationFlagGrain : Grain, IMigrationFlagGrain
    {
        private readonly IPersistentState<MigrationState> _state;

        public MigrationFlagGrain(
            [PersistentState("migration", "ChatStore")]
            IPersistentState<MigrationState> state)
            => _state = state;

        public Task<bool> IsDoneAsync()
            => Task.FromResult(_state.State.IsDone);

        public async Task SetDoneAsync()
        {
            _state.State.IsDone = true;
            await _state.WriteStateAsync();
        }
    }

    [GenerateSerializer]
    public sealed class MigrationState
    {
        [Id(0)] public bool IsDone { get; set; }
    }
}
