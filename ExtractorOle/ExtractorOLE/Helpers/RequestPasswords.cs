namespace ExtractorOLE.Helpers
{
    // Null and empty request passwords both mean "no password" ([ydk:entity:extraction/ExtractionRequest]).
    internal static class RequestPasswords
    {
        internal static bool HasPassword(string? password) => !string.IsNullOrEmpty(password);
    }
}
