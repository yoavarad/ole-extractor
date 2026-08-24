using SampleGenerator.Abstractions;
using SampleGenerator.Generators;

namespace SampleGenerator.Fixtures
{
    /// <summary>
    /// docs/specs/dataset-curation.md's "curated real-world-style samples" composition
    /// rule: 2 self-authored samples per format. Scoped down to docx/xlsx/pptx here (a
    /// product-owner decision) - doc/xls/ppt curated authoring is blocked because
    /// DocSampleGenerator/XlsSampleGenerator/PptSampleGenerator are stub
    /// throw-NotSupportedException placeholders pending NPOI-compiled-from-source being
    /// wired into SampleGenerator.csproj (ADR-001; tracked by T-517f89c5, T-c4123b53,
    /// T-a5f1d074, T-bac988f5 - not this task's job to fix).
    ///
    /// Each of the 3 formats gets:
    /// - one NESTED-embedding sample: 2+ genuine nesting levels (outer embeds a real
    ///   inner-generated OOXML package, which itself embeds a PNG), satisfying the
    ///   "2+ levels of nested embedding" half of the curated-sample composition rule.
    /// - one REVISION-HISTORY sample: scripted-but-realistic
    ///   Author/LastModifiedBy/Created/Modified/Revision core properties plus one
    ///   embedded logo/chart PNG. This is a relaxed stand-in for the composition rule's
    ///   "real (not scripted) authorship/revision-history property chain from having
    ///   been edited by multiple real users across multiple real save operations" -
    ///   Office/LibreOffice authoring isn't available yet. See the follow-up task filed
    ///   under S-da936361 alongside T-6f033869 for true human-provenance samples.
    ///
    /// Nesting is built bottom-up: an inner-format ISampleGenerator is invoked directly
    /// (DocxSampleGenerator/XlsxSampleGenerator have no constructor dependencies) to
    /// produce real OOXML package bytes for the inner document. Those bytes are then
    /// wrapped as an EmbeddedContentSpec whose ContentType is the inner format's real
    /// OOXML MIME type (not image/*), so OpenXmlEmbeddingHelper routes it to an
    /// EmbeddedObjectPart rather than an ImagePart.
    ///
    /// Body text/data here is genuine real-world-style prose (an actual invoice layout,
    /// actual meeting-minutes text, actual budget numbers, actual slide titles/bullets)
    /// - none of it reuses MultilingualFixtures' multilingual-stress-test body text or
    /// MultiEmbeddingSampleSpecs/MacroEmbedSampleSpecs' multi-embedding/macro-embed
    /// purpose. No multilingual/emoji text is required here: the
    /// [ydk:req:extraction/unicode-fidelity] requirement is scoped to the synthetic
    /// samples and is already satisfied there.
    /// </summary>
    public static class CuratedSampleSpecs
    {
        private const string DocxMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Single shared 1x1 placeholder PNG for every logo/chart embedding below - these
        // samples exercise embedding structure and body/metadata content, not image
        // rendering fidelity.
        private static readonly byte[] PngPlaceholder = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        // ---------------------------------------------------------------
        // docx / meeting-minutes-boardroom.docx - NESTED (docx -> xlsx -> png)
        // ---------------------------------------------------------------

        public static readonly string[] MeetingMinutesBoardroomLines =
        {
            "Board of Directors Meeting Minutes",
            "Acme Robotics, Inc.",
            "Date: March 12, 2026",
            "Location: Conference Room A, Corporate Headquarters",
            "Attendees: J. Whitfield (Chair), R. Alvarez, S. Kim, T. Brennan, M. Okafor",
            "1. Call to Order: The meeting was called to order at 9:02 AM by Chair J. Whitfield.",
            "2. Approval of Previous Minutes: The minutes of the February 12, 2026 meeting were approved without amendment.",
            "3. Financial Report: The CFO presented Q1 results, noting revenue of $4.2M against a forecast of $4.0M. The attached budget snippet summarizes departmental spend for the quarter.",
            "4. Old Business: None.",
            "5. New Business: The board approved the expansion of the Austin facility, effective Q3 2026.",
            "6. Adjournment: The meeting was adjourned at 10:15 AM. Next meeting: April 9, 2026."
        };

        private static readonly string[] BudgetSnippetLines =
        {
            "Q1 2026 Budget Snippet",
            "Engineering: $1,200,000 budgeted / $1,180,000 actual",
            "Sales: $800,000 budgeted / $835,000 actual",
            "Marketing: $300,000 budgeted / $290,000 actual",
            "Total: $2,300,000 budgeted / $2,305,000 actual"
        };

        public static SampleSpec BuildMeetingMinutesBoardroom()
        {
            var innerXlsxSpec = new SampleSpec
            {
                BodyText = string.Join("\n", BudgetSnippetLines),
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new() { FileName = "budget-chart.png", Content = PngPlaceholder, ContentType = "image/png" }
                }
            };
            var innerXlsx = new XlsxSampleGenerator().Generate(innerXlsxSpec);

            return new SampleSpec
            {
                BodyText = string.Join("\n", MeetingMinutesBoardroomLines),
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new() { FileName = "budget-snippet.xlsx", Content = innerXlsx.Content, ContentType = XlsxMimeType }
                }
            };
        }

        // ---------------------------------------------------------------
        // docx / invoice-acme-corp.docx - REVISION HISTORY
        // ---------------------------------------------------------------

        public static readonly string[] InvoiceAcmeCorpLines =
        {
            "INVOICE",
            "Acme Corp",
            "1400 Industrial Parkway, Springfield, IL 62704",
            "Invoice Number: INV-2026-0347",
            "Invoice Date: March 19, 2026",
            "Due Date: April 18, 2026",
            "Bill To: Northgate Manufacturing LLC, 220 Commerce Drive, Columbus, OH 43215",
            "Industrial Sensor Array Model X200 - Qty 12 - Unit Price $845.00 - Amount $10,140.00",
            "On-site Calibration Service - Qty 1 - Unit Price $1,250.00 - Amount $1,250.00",
            "Extended Warranty (3-year) - Qty 1 - Unit Price $960.00 - Amount $960.00",
            "Subtotal: $12,350.00",
            "Tax (7.25%): $895.38",
            "Total Due: $13,245.38",
            "Payment Terms: Net 30. Please remit payment to Acme Corp, Accounts Receivable."
        };

        public static SampleSpec BuildInvoiceAcmeCorp() => new()
        {
            BodyText = string.Join("\n", InvoiceAcmeCorpLines),
            Metadata = new Dictionary<string, string>
            {
                [MetadataFields.Author] = "Dana Cole",
                [MetadataFields.LastModifiedBy] = "Marcus Webb",
                [MetadataFields.Created] = "2026-02-03T00:00:00Z",
                [MetadataFields.Modified] = "2026-03-19T00:00:00Z",
                [MetadataFields.RevisionNumber] = "14"
            },
            Embeddings = new List<EmbeddedContentSpec>
            {
                new() { FileName = "acme-logo.png", Content = PngPlaceholder, ContentType = "image/png" }
            }
        };

        // ---------------------------------------------------------------
        // xlsx / project-status-report.xlsx - NESTED (xlsx -> docx -> png)
        // ---------------------------------------------------------------

        public static readonly string[] ProjectStatusReportLines =
        {
            "Project Status Report - Q1 2026",
            "Project: Customer Portal Redesign",
            "Project Manager: Priya Nair",
            "Status: On Track",
            "Percent Complete: 68%",
            "Milestone: Requirements Sign-off - Complete - Jan 15, 2026",
            "Milestone: UI Design Approval - Complete - Feb 10, 2026",
            "Milestone: Backend API Complete - In Progress - Due Mar 28, 2026",
            "Milestone: QA Testing - Not Started - Due Apr 15, 2026",
            "Milestone: Production Launch - Not Started - Due May 1, 2026",
            "Risks: Third-party payment gateway integration delayed by vendor; mitigation in progress.",
            "Notes: See attached one-pager memo for executive summary."
        };

        private static readonly string[] OnePagerMemoLines =
        {
            "Customer Portal Redesign - Executive Summary",
            "Prepared by: Priya Nair, Project Manager",
            "Date: March 20, 2026",
            "The Customer Portal Redesign project remains on track for a May 1, 2026 launch. Backend API development is 70% complete. The primary risk is a delay from our third-party payment gateway vendor, currently being mitigated through weekly vendor check-ins. No budget overruns to report."
        };

        public static SampleSpec BuildProjectStatusReport()
        {
            var innerDocxSpec = new SampleSpec
            {
                BodyText = string.Join("\n", OnePagerMemoLines),
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new() { FileName = "status-chart.png", Content = PngPlaceholder, ContentType = "image/png" }
                }
            };
            var innerDocx = new DocxSampleGenerator().Generate(innerDocxSpec);

            return new SampleSpec
            {
                BodyText = string.Join("\n", ProjectStatusReportLines),
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new() { FileName = "status-memo.docx", Content = innerDocx.Content, ContentType = DocxMimeType }
                }
            };
        }

        // ---------------------------------------------------------------
        // xlsx / budget-quarterly-2026.xlsx - REVISION HISTORY
        // ---------------------------------------------------------------

        public static readonly string[] BudgetQuarterly2026Lines =
        {
            "Quarterly Department Budget - FY2026 Q1",
            "Engineering: Budgeted $1,200,000 - Actual $1,180,000 - Variance -$20,000",
            "Sales: Budgeted $800,000 - Actual $835,000 - Variance $35,000",
            "Marketing: Budgeted $300,000 - Actual $290,000 - Variance -$10,000",
            "Operations: Budgeted $450,000 - Actual $440,000 - Variance -$10,000",
            "Total: Budgeted $2,750,000 - Actual $2,745,000 - Variance -$5,000"
        };

        public static SampleSpec BuildBudgetQuarterly2026() => new()
        {
            BodyText = string.Join("\n", BudgetQuarterly2026Lines),
            Metadata = new Dictionary<string, string>
            {
                [MetadataFields.Author] = "Elena Ruiz",
                [MetadataFields.LastModifiedBy] = "Tom Baker",
                [MetadataFields.Created] = "2026-01-10T00:00:00Z",
                [MetadataFields.Modified] = "2026-04-02T00:00:00Z",
                [MetadataFields.RevisionNumber] = "6"
            },
            Embeddings = new List<EmbeddedContentSpec>
            {
                new() { FileName = "department-chart.png", Content = PngPlaceholder, ContentType = "image/png" }
            }
        };

        // ---------------------------------------------------------------
        // pptx / product-launch-deck.pptx - NESTED (pptx -> xlsx -> png)
        // ---------------------------------------------------------------

        public static readonly string[] ProductLaunchDeckLines =
        {
            "Product Launch: Nova X1 Wireless Earbuds",
            "Launch Date: June 3, 2026",
            "Target Market: Active lifestyle consumers, 18-34",
            "Key Features: 30-hour battery life, IPX7 waterproof rating, adaptive noise cancellation",
            "Go-to-Market Channels: Retail partners, direct-to-consumer site, Amazon",
            "Pricing: $129.99 MSRP",
            "See attached data table for regional sales projections."
        };

        private static readonly string[] SalesProjectionsLines =
        {
            "Regional Sales Projections - Nova X1",
            "North America: 85,000 units",
            "Europe: 52,000 units",
            "Asia Pacific: 61,000 units",
            "Latin America: 18,000 units",
            "Total: 216,000 units"
        };

        public static SampleSpec BuildProductLaunchDeck()
        {
            var innerXlsxSpec = new SampleSpec
            {
                BodyText = string.Join("\n", SalesProjectionsLines),
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new() { FileName = "sales-chart.png", Content = PngPlaceholder, ContentType = "image/png" }
                }
            };
            var innerXlsx = new XlsxSampleGenerator().Generate(innerXlsxSpec);

            return new SampleSpec
            {
                BodyText = string.Join("\n", ProductLaunchDeckLines),
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new() { FileName = "sales-projections.xlsx", Content = innerXlsx.Content, ContentType = XlsxMimeType }
                }
            };
        }

        // ---------------------------------------------------------------
        // pptx / training-onboarding-slides.pptx - REVISION HISTORY
        // ---------------------------------------------------------------

        public static readonly string[] TrainingOnboardingSlidesLines =
        {
            "New Employee Onboarding",
            "Welcome to Acme Corp",
            "Week 1 Agenda: HR orientation, IT setup, team introductions",
            "Week 2 Agenda: Role-specific training, mentor assignment",
            "Key Contacts: HR - hr@acmecorp.example, IT Help Desk - ithelp@acmecorp.example",
            "Company Values: Integrity, Collaboration, Innovation, Accountability",
            "Please complete all required compliance training modules by day 30."
        };

        public static SampleSpec BuildTrainingOnboardingSlides() => new()
        {
            BodyText = string.Join("\n", TrainingOnboardingSlidesLines),
            Metadata = new Dictionary<string, string>
            {
                [MetadataFields.Author] = "Sam Okafor",
                [MetadataFields.LastModifiedBy] = "Nina Patel",
                [MetadataFields.Created] = "2026-03-01T00:00:00Z",
                [MetadataFields.Modified] = "2026-05-15T00:00:00Z",
                [MetadataFields.RevisionNumber] = "11"
            },
            Embeddings = new List<EmbeddedContentSpec>
            {
                new() { FileName = "acme-logo.png", Content = PngPlaceholder, ContentType = "image/png" }
            }
        };
    }
}
