using System;
using System.Collections.Generic;
using System.Text;

namespace ExtractorOLE.Old
{
    internal class BackupLogic
    {
        /*
         * // Fallback: try to open as Word -> Spreadsheet -> Presentation (Randomly choosen by me, you can change it to be statistics based)
            if (!opened)
            {
                try
                {
                    Console.WriteLine("tring Word fallback");
                    using (var ms = new MemoryStream(fileBytes))
                    using (var word = WordprocessingDocument.Open(ms, false))
                    {
                        ExtractMetadataAndEmbedded(word, word.MainDocumentPart, result);
                        coreMime = word.CoreFilePropertiesPart?.ContentType;
                        opened = true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"error: {e.Message}");
                    // not a WordprocessingDocument
                }

                if (!opened)
                {
                    try
                    {
                        Console.WriteLine("tring Excel fallback");
                        using (var ms = new MemoryStream(fileBytes))
                        using (var excel = SpreadsheetDocument.Open(ms, false))
                        {
                            ExtractMetadataAndEmbedded(excel, excel.WorkbookPart, result);
                            coreMime = excel.CoreFilePropertiesPart?.ContentType;
                            opened = true;
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"error: {e.Message}");
                        // not a SpreadsheetDocument
                    }
                }

                if (!opened)
                {
                    try
                    {
                        Console.WriteLine("tring PowerPoint fallback");
                        using (var ms = new MemoryStream(fileBytes))
                        using (var pres = PresentationDocument.Open(ms, false))
                        {
                            ExtractMetadataAndEmbedded(pres, pres.PresentationPart, result);
                            coreMime = pres.CoreFilePropertiesPart?.ContentType;
                            opened = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"error: {e.Message}");
                        // not a PresentationDocument
                    }
                }
            }
         * */
    }
}
