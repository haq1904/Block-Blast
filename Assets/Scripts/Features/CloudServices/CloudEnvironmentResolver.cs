namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Pure resolver determining the target UGS environment based on editor and build configuration.
    /// </summary>
    public static class CloudEnvironmentResolver
    {
        public const string DevelopmentEnvironment = "development";
        public const string ProductionEnvironment = "production";

        /// <summary>
        /// Returns 'development' for Unity Editor or Development Builds; otherwise returns 'production'.
        /// </summary>
        public static string Resolve(bool isEditor, bool isDebugBuild)
        {
            return (isEditor || isDebugBuild) ? DevelopmentEnvironment : ProductionEnvironment;
        }
    }
}
