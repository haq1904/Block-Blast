namespace BlockBlast.Features.PlayerCloud.Gateway
{
    public sealed class ProtectedPlayerDataReadResult
    {
        public bool Found { get; }
        public string JsonValue { get; }

        private ProtectedPlayerDataReadResult(bool found, string jsonValue)
        {
            Found = found;
            JsonValue = jsonValue;
        }

        public static ProtectedPlayerDataReadResult NotFound()
        {
            return new ProtectedPlayerDataReadResult(false, null);
        }

        public static ProtectedPlayerDataReadResult FoundResult(string jsonValue)
        {
            return new ProtectedPlayerDataReadResult(true, jsonValue);
        }
    }
}
