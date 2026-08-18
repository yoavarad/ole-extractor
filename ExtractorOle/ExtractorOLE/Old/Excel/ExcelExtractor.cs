using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtractorOLE.Old.Excel
{
    internal class ExcelExtractor
    {
        public DocumentExtractionResult ExtractFromXlsxBinary(byte[] fileBytes)
        {
            var result = new DocumentExtractionResult();

            using (MemoryStream ms = new MemoryStream(fileBytes))
            {
                using (SpreadsheetDocument excelDoc = SpreadsheetDocument.Open(ms, false))
                {
                    WorkbookPart? workbookPart = excelDoc.WorkbookPart;
                    if (workbookPart == null) return result;

                    // Requirement #1: Warning-Free Metadata Parsing (Direct CoreFilePropertiesPart)
                    CoreFilePropertiesPart? corePart = excelDoc.CoreFilePropertiesPart;
                    if (corePart?.CoreFileProperties != null)
                    {
                        var props = corePart.CoreFileProperties;
                        result.Metadata.Title = props.Title;
                        result.Metadata.Creator = props.Creator;
                        result.Metadata.Created = props.Created;
                        result.Metadata.Modified = props.Modified;
                        result.Metadata.LastModifiedBy = props.LastModifiedBy;


                        // Requirement #2: Direct, static IANA MIME-type allocation
                        result.MimeType = corePart.ContentType;
                    }


                    // Pre-cache Shared String Table to prevent repetitive file scans
                    SharedStringTablePart? stringTablePart = workbookPart.SharedStringTablePart;
                    List<string> sharedStrings = new List<string>();
                    if (stringTablePart?.SharedStringTable != null)
                    {
                        sharedStrings = stringTablePart.SharedStringTable
                                                     .Elements<SharedStringItem>()
                                                     .Select(x => x.InnerText)
                                                     .ToList();
                    }

                    // Requirement #3: Flat Text Extraction (Iterating over rows and columns)
                    StringBuilder textBuilder = new StringBuilder();
                    foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
                    {
                        SheetData? sheetData = worksheetPart.Worksheet?.Elements<SheetData>().FirstOrDefault();
                        if (sheetData == null) continue;

                        foreach (Row row in sheetData.Elements<Row>())
                        {
                            foreach (Cell cell in row.Elements<Cell>())
                            {
                                string cellValue = GetCellText(cell, sharedStrings);
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    textBuilder.Append(cellValue).Append(' ');
                                }
                            }
                            textBuilder.AppendLine(); // Line break per row
                        }
                    }
                    result.ExtractedText = textBuilder.ToString().Trim();

                    // Requirement #4: Extract ONLY 1st Layer Embedded Files
                    // workbookPart.Parts scans immediate relational components attached to the main workspace container
                    GetFirstLayerEmbedded(result, workbookPart);
                }
            }

            return result;
        }

        private void GetFirstLayerEmbedded(DocumentExtractionResult result, WorkbookPart workbookPart)
        {
            int index = 1;
            foreach (var partPair in workbookPart.Parts)
            {
                var nestedPart = partPair.OpenXmlPart;

                // Identify embedded spreadsheet assets, OLE attachments, packages, or macro containers
                if (nestedPart is EmbeddedObjectPart || nestedPart is EmbeddedPackagePart || nestedPart is ImagePart)
                {
                    using (Stream partStream = nestedPart.GetStream())
                    using (MemoryStream binaryStream = new MemoryStream())
                    {
                        partStream.CopyTo(binaryStream);
                        byte[] extractedBytes = binaryStream.ToArray();

                        var item = new EmbeddedFileItem
                        {
                            BinaryData = extractedBytes,
                            PackagePath = nestedPart.Uri.ToString(), // Path inside zip container
                            SizeInBytes = extractedBytes.LongLength
                        };

                        // Look up corresponding extension from data type and format names
                        string extension = GetExtensionFromContentType(nestedPart.ContentType);
                        item.FileName = $"excel_embedded_object_{index}{extension}";

                        result.EmbeddedFiles.Add(item);
                        index++;
                    }
                }
            }
        }


        private string GetCellText(Cell cell, List<string> sharedStrings)
        {
            string rawValue = cell.CellValue?.InnerText ?? string.Empty;
            if (string.IsNullOrEmpty(rawValue)) return string.Empty;

            // If DataType flag marks cell as a shared index, fetch corresponding item from cache
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (int.TryParse(rawValue, out int index) && index >= 0 && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }
            }

            return rawValue; // Return default raw layout number or string value fallback
        }

        private string GetExtensionFromContentType(string contentType)
        {
            string cType = contentType.ToLower();
            if (cType.Contains("pdf")) return ".pdf";
            if (cType.Contains("spreadsheetml") || cType.Contains("excel")) return ".xlsx";
            if (cType.Contains("wordprocessingml")) return ".docx";
            if (cType.Contains("jpeg") || cType.Contains("jpg")) return ".jpg";
            if (cType.Contains("png")) return ".png";
            if (cType.Contains("presentationml")) return ".pptx";
            return ".bin"; // Generic binary data fallback
        }
    }
}


/*var extendedPropertiesPart = excelDoc.ExtendedFilePropertiesPart;

if (extendedPropertiesPart != null && extendedPropertiesPart.Properties != null)
{
    var extProps = extendedPropertiesPart.Properties;

}


var coreProps = excelDoc.PackageProperties;
result.Metadata.Title = coreProps.Title;
result.Metadata.Creator = coreProps.Creator;
result.Metadata.Created = coreProps.Created;
result.Metadata.Modified = coreProps.Modified;
result.Metadata.LastModifiedBy = coreProps.LastModifiedBy;*/