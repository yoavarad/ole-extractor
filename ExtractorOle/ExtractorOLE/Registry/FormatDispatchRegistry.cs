using ExtractorOLE.Abstractions;
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
        private readonly IDictionary<OfficeMimeType, IOpenStrategy> _openStrategies;
        private readonly IDictionary<OfficeMimeType, ITextExtractor> _textExtractors;

        public FormatDispatchRegistry(
            IDictionary<OfficeMimeType, IOpenStrategy> openStrategies,
            IDictionary<OfficeMimeType, ITextExtractor> textExtractors)
        {
            _openStrategies = openStrategies ?? throw new ArgumentNullException(nameof(openStrategies));
            _textExtractors = textExtractors ?? throw new ArgumentNullException(nameof(textExtractors));
        }

        public IOpenStrategy? GetOpenStrategy(OfficeMimeType format) =>
            _openStrategies.TryGetValue(format, out var strategy) ? strategy : null;

        public ITextExtractor? GetTextExtractor(OfficeMimeType format) =>
            _textExtractors.TryGetValue(format, out var extractor) ? extractor : null;
    }
}
