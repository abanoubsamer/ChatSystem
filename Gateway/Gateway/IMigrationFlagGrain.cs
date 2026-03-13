namespace AppGateway
{
    public interface IMigrationFlagGrain : IGrainWithStringKey
    {
        Task<bool> IsDoneAsync();
        Task SetDoneAsync();
    }
}
