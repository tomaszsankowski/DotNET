namespace Contracts
{
    public class DataSubmittedEvent(string data)
    {
        public string Data { get; } = data;
    }
}