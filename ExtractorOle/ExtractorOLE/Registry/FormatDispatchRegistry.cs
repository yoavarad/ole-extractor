using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.Collections.Generic;

namespace ExtractorOLE.Registry
{
    /// <summary>
    /// Default <see cref="IFormatDispatchRegistry"/> implementation: two
    /// format-keyed lookup tables built once at DI-registration time (see
    /// ServiceRegistration) and never mutated afterward, so it is safe to
    /// share as a singleton across concurrent extraction calls.
    /// </summary>
    public sealed class FormatDispatchRegistry : IFormatDispatchRegistry
    {
        private readonly IDictionary<OfficeMimeTypeEnum, IOpenStrategy> _openStrategies;
        private readonly IDictionary<OfficeMimeTypeEnum, ITextExtractor> _textExtractors;

        public FormatDispatchRegistry(
            IDictionary<OfficeMimeTypeEnum, IOpenStrategy> openStrategies,
            IDictionary<OfficeMimeTypeEnum, ITextExtractor> textExtractors)
        {
            _openStrategies = openStrategies ?? throw new ArgumentNullException(nameof(openStrategies));
            _textExtractors = textExtractors ?? throw new ArgumentNullException(nameof(textExtractors));
        }

        public IOpenStrategy? GetOpenStrategy(OfficeMimeTypeEnum format) =>
            _openStrategies.TryGetValue(format, out var strategy) ? strategy : null;

        public ITextExtractor? GetTextExtractor(OfficeMimeTypeEnum format) =>
            _textExtractors.TryGetValue(format, out var extractor) ? extractor : null;
    }
}
