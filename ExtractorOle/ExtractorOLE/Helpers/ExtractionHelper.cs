using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using System;

namespace ExtractorOLE.Helpers
{
    /// <summary>
    /// Backward-compatible facade over the three focused components that own the work:
    /// <see cref="IMimeTypeMapper"/> (MIME &lt;-&gt; enum mapping), <see cref="IOfficeMimeTypeDetector"/>
    /// (MIME detection) and <see cref="IEmbeddedFileExtractor"/> (first-layer embedded extraction).
    /// Contains no logic of its own.
    /// </summary>
    public class ExtractionHelper : IExtractionHelper
    {
        private readonly IMimeTypeMapper _mapper;
        private readonly IOfficeMimeTypeDetector _detector;
        private readonly IEmbeddedFileExtractor _embedded;

        public ExtractionHelper() : this(new MimeDetectionRegistry(new IMimeTypeDetector[]
        {
            new OoxmlMimeDetector(),
            new CfbMimeDetector(),
        }))
        {
        }

        public ExtractionHelper(IMimeDetectionRegistry detectionRegistry)
            : this(new MimeTypeMapper(), new OfficeMimeTypeDetector(detectionRegistry))
        {
        }

        private ExtractionHelper(IMimeTypeMapper mapper, IOfficeMimeTypeDetector detector)
            : this(mapper, detector, new EmbeddedFileExtractor(mapper))
        {
        }

        public ExtractionHelper(IMimeTypeMapper mapper, IOfficeMimeTypeDetector detector, IEmbeddedFileExtractor embedded)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _embedded = embedded ?? throw new ArgumentNullException(nameof(embedded));
        }

        public string MimeFor(OfficeMimeTypeEnum type) => _mapper.MimeFor(type);

        public OfficeMimeTypeEnum ParseOfficeMimeType(string? mime) => _mapper.ParseOfficeMimeType(mime);

        public string GetExtensionFromContentType(string contentType) => _mapper.GetExtensionFromContentType(contentType);

        public void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result) =>
            _embedded.ExtractMetadataAndEmbedded(package, rootPart, result);

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart) =>
            _embedded.ExtractFirstLayerEmbedded(result, rootPart);

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook) =>
            _embedded.ExtractFirstLayerEmbedded(result, workbook);

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, NPOIFSFileSystem fs) =>
            _embedded.ExtractFirstLayerEmbedded(result, fs);

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, DirectoryEntry root) =>
            _embedded.ExtractFirstLayerEmbedded(result, root);

        public OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes) => _detector.DetectMimeTypeFromBytes(fileBytes);

        public MimeDetectionResult DetectMimeType(MimeDetectionRequest request) => _detector.DetectMimeType(request);

        public string DetectMimeFromMagicBytes(byte[] bytes) => _detector.DetectMimeFromMagicBytes(bytes);
    }
}
