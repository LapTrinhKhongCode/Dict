using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    /// <summary>
    /// Stores structured table data extracted from documents (Azure DI / xlsx / etc.).
    /// One row per table per document page. Cells stored as JSON for full fidelity lookup.
    /// </summary>
    public class DocumentTable
    {
        public int Id { get; set; }

        public int OcrJobId { get; set; }
        [ForeignKey(nameof(OcrJobId))]
        public virtual OcrJob OcrJob { get; set; }

        public int PageNumber { get; set; }

        /// <summary>Zero-based ordinal of the table on the page.</summary>
        public int TableIndex { get; set; }

        /// <summary>Heading/section title extracted from paragraph nearest to the table.</summary>
        [MaxLength(512)]
        public string? SectionTitle { get; set; }

        /// <summary>Table caption from Azure DI, if present.</summary>
        [MaxLength(512)]
        public string? Caption { get; set; }

        public int RowCount { get; set; }
        public int ColumnCount { get; set; }

        /// <summary>JSON array of header strings (first row / column header cells).</summary>
        public string? HeadersJson { get; set; }

        /// <summary>
        /// JSON: [ { "row": 0, "col": 0, "content": "...", "rowSpan": 1, "colSpan": 1, "kind": "columnHeader|rowHeader|content" } ]
        /// </summary>
        public string CellsJson { get; set; } = "[]";

        /// <summary>LLM-generated semantic description used for vector embedding.</summary>
        public string? SummaryForEmbedding { get; set; }

        /// <summary>SHA-256 of CellsJson for dedup.</summary>
        [MaxLength(64)]
        public string? ContentHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
