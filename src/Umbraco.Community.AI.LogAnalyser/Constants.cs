namespace Umbraco.Community.AI.LogAnalyser
{
    public class Constants
    {
        public const string ApiName = "ailoganalyser";

        /// <summary>
        /// Alias for the Umbraco.AI inline chat used to analyse log entries. Must be URL-safe:
        /// Umbraco.AI derives a deterministic chat ID from it for auditing and telemetry, so
        /// changing it starts a new identity in the audit log.
        /// </summary>
        public const string ChatAlias = "ai-log-analyser";
    }
}
