using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dict.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //    migrationBuilder.CreateTable(
            //        name: "api_calls",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            Endpoint = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
            //            RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            ResponseStatus = table.Column<int>(type: "int", nullable: true),
            //            ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_api_calls", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "entries",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            EntSeq = table.Column<long>(type: "bigint", nullable: true),
            //            Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            //            Label = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            //            RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_entries", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "import_jobs",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            JobType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            //            StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            Meta = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_import_jobs", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "kanji",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            Character = table.Column<string>(type: "nchar(1)", nullable: false),
            //            StrokeCount = table.Column<int>(type: "int", nullable: true),
            //            Grade = table.Column<int>(type: "int", nullable: true),
            //            JlptLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
            //            Meaning = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Freq = table.Column<int>(type: "int", nullable: true),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_kanji", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "languages",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
            //            Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_languages", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "tags",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            //            Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_tags", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "users",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //            Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_users", x => x.Id);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "kanji_elements",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            Keb = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            KeInf = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            KePri = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_kanji_elements", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_kanji_elements_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "reading_elements",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            Reb = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            ReNoKanji = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            RePri = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_reading_elements", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_reading_elements_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "senses",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            Pos = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            //            Field = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            //            Misc = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            //            SInf = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            Dialect = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            SenseOrder = table.Column<int>(type: "int", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_senses", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_senses_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "words",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            WordText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            Phonetic = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            Romaji = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            ShortMean = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            //            Weight = table.Column<int>(type: "int", nullable: true),
            //            MobileId = table.Column<int>(type: "int", nullable: true),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_words", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_words_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.SetNull);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "decks",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            UserId = table.Column<int>(type: "int", nullable: true),
            //            Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            IsPublic = table.Column<bool>(type: "bit", nullable: true),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_decks", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_decks_users_UserId",
            //                column: x => x.UserId,
            //                principalTable: "users",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "media_store",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            OwnerId = table.Column<int>(type: "int", nullable: true),
            //            FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
            //            MimeType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            SizeBytes = table.Column<long>(type: "bigint", nullable: true),
            //            StorageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
            //            Sha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_media_store", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_media_store_users_OwnerId",
            //                column: x => x.OwnerId,
            //                principalTable: "users",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "examples",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            SenseId = table.Column<int>(type: "int", nullable: true),
            //            ContentJp = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            ContentTranslated = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Transcription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
            //            SourceRef = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_examples", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_examples_senses_SenseId",
            //                column: x => x.SenseId,
            //                principalTable: "senses",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "glosses",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            SenseId = table.Column<int>(type: "int", nullable: true),
            //            LanguageId = table.Column<int>(type: "int", nullable: true),
            //            Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            GType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            //            GGend = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            //            Priority = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_glosses", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_glosses_languages_LanguageId",
            //                column: x => x.LanguageId,
            //                principalTable: "languages",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_glosses_senses_SenseId",
            //                column: x => x.SenseId,
            //                principalTable: "senses",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "synsets",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            SenseId = table.Column<int>(type: "int", nullable: true),
            //            BaseForm = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            Synonyms = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Pos = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_synsets", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_synsets_senses_SenseId",
            //                column: x => x.SenseId,
            //                principalTable: "senses",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "lemmas",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            LemmaText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            Pos = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_lemmas", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_lemmas_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.SetNull);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "media",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
            //            Caption = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            //            MediaType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            Source = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_media", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_media_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //            table.ForeignKey(
            //                name: "FK_media_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "stats_word_freq",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            Occurrences = table.Column<int>(type: "int", nullable: true),
            //            LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_stats_word_freq", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_stats_word_freq_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "synonyms_cache",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_synonyms_cache", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_synonyms_cache_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "translations",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            LanguageId = table.Column<int>(type: "int", nullable: true),
            //            Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            ExamplesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Source = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_translations", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_translations_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //            table.ForeignKey(
            //                name: "FK_translations_languages_LanguageId",
            //                column: x => x.LanguageId,
            //                principalTable: "languages",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_translations_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "word_kanji",
            //        columns: table => new
            //        {
            //            WordId = table.Column<int>(type: "int", nullable: false),
            //            KanjiId = table.Column<int>(type: "int", nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_word_kanji", x => new { x.WordId, x.KanjiId });
            //            table.ForeignKey(
            //                name: "FK_word_kanji_kanji_KanjiId",
            //                column: x => x.KanjiId,
            //                principalTable: "kanji",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //            table.ForeignKey(
            //                name: "FK_word_kanji_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "word_relations",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: false),
            //            RelatedWordId = table.Column<int>(type: "int", nullable: false),
            //            RelationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            //            Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_word_relations", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_word_relations_words_RelatedWordId",
            //                column: x => x.RelatedWordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_word_relations_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "word_tags",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            TagId = table.Column<int>(type: "int", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_word_tags", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_word_tags_tags_TagId",
            //                column: x => x.TagId,
            //                principalTable: "tags",
            //                principalColumn: "Id");
            //            table.ForeignKey(
            //                name: "FK_word_tags_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "WordToEntries",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            EntryId = table.Column<int>(type: "int", nullable: true),
            //            MappingType = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_WordToEntries", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_WordToEntries_entries_EntryId",
            //                column: x => x.EntryId,
            //                principalTable: "entries",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //            table.ForeignKey(
            //                name: "FK_WordToEntries_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "cards",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            DeckId = table.Column<int>(type: "int", nullable: true),
            //            WordId = table.Column<int>(type: "int", nullable: true),
            //            Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            FrontText = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            BackText = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_cards", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_cards_decks_DeckId",
            //                column: x => x.DeckId,
            //                principalTable: "decks",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //            table.ForeignKey(
            //                name: "FK_cards_words_WordId",
            //                column: x => x.WordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.SetNull);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "ocr_jobs",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            UserId = table.Column<int>(type: "int", nullable: true),
            //            MediaId = table.Column<int>(type: "int", nullable: true),
            //            Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
            //            DetectedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_ocr_jobs", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_ocr_jobs_media_store_MediaId",
            //                column: x => x.MediaId,
            //                principalTable: "media_store",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_ocr_jobs_users_UserId",
            //                column: x => x.UserId,
            //                principalTable: "users",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "inflections",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            LemmaId = table.Column<int>(type: "int", nullable: true),
            //            FormText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            FormType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_inflections", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_inflections_lemmas_LemmaId",
            //                column: x => x.LemmaId,
            //                principalTable: "lemmas",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "card_states",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            CardId = table.Column<int>(type: "int", nullable: true),
            //            UserId = table.Column<int>(type: "int", nullable: true),
            //            DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            Interval = table.Column<int>(type: "int", nullable: true),
            //            Ease = table.Column<float>(type: "real", nullable: true),
            //            Reps = table.Column<int>(type: "int", nullable: true),
            //            Lapses = table.Column<int>(type: "int", nullable: true),
            //            DeckPosition = table.Column<int>(type: "int", nullable: true),
            //            LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            Suspended = table.Column<bool>(type: "bit", nullable: true),
            //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_card_states", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_card_states_cards_CardId",
            //                column: x => x.CardId,
            //                principalTable: "cards",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_card_states_users_UserId",
            //                column: x => x.UserId,
            //                principalTable: "users",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "ocr_results",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            OcrJobId = table.Column<int>(type: "int", nullable: true),
            //            PageNumber = table.Column<int>(type: "int", nullable: true),
            //            WordText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //            BoundingBox = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //            Confidence = table.Column<float>(type: "real", nullable: true),
            //            LinkWordId = table.Column<int>(type: "int", nullable: true)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_ocr_results", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_ocr_results_ocr_jobs_OcrJobId",
            //                column: x => x.OcrJobId,
            //                principalTable: "ocr_jobs",
            //                principalColumn: "Id");
            //            table.ForeignKey(
            //                name: "FK_ocr_results_words_LinkWordId",
            //                column: x => x.LinkWordId,
            //                principalTable: "words",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.SetNull);
            //        });

            //    migrationBuilder.CreateTable(
            //        name: "review_logs",
            //        columns: table => new
            //        {
            //            Id = table.Column<int>(type: "int", nullable: false)
            //                .Annotation("SqlServer:Identity", "1, 1"),
            //            CardStateId = table.Column<int>(type: "int", nullable: true),
            //            UserId = table.Column<int>(type: "int", nullable: true),
            //            CardId = table.Column<int>(type: "int", nullable: true),
            //            Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
            //            Quality = table.Column<int>(type: "int", nullable: true),
            //            TimeTakenMs = table.Column<int>(type: "int", nullable: true),
            //            PreviousInterval = table.Column<int>(type: "int", nullable: true),
            //            NewInterval = table.Column<int>(type: "int", nullable: true),
            //            Ease = table.Column<float>(type: "real", nullable: true),
            //            Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //        },
            //        constraints: table =>
            //        {
            //            table.PrimaryKey("PK_review_logs", x => x.Id);
            //            table.ForeignKey(
            //                name: "FK_review_logs_card_states_CardStateId",
            //                column: x => x.CardStateId,
            //                principalTable: "card_states",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_review_logs_cards_CardId",
            //                column: x => x.CardId,
            //                principalTable: "cards",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Restrict);
            //            table.ForeignKey(
            //                name: "FK_review_logs_users_UserId",
            //                column: x => x.UserId,
            //                principalTable: "users",
            //                principalColumn: "Id",
            //                onDelete: ReferentialAction.Cascade);
            //        });

            //    migrationBuilder.CreateIndex(
            //        name: "IX_card_states_CardId",
            //        table: "card_states",
            //        column: "CardId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_card_states_UserId",
            //        table: "card_states",
            //        column: "UserId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_cards_DeckId",
            //        table: "cards",
            //        column: "DeckId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_cards_WordId",
            //        table: "cards",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_decks_UserId",
            //        table: "decks",
            //        column: "UserId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_entries_EntSeq",
            //        table: "entries",
            //        column: "EntSeq",
            //        unique: true,
            //        filter: "[EntSeq] IS NOT NULL");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_examples_SenseId",
            //        table: "examples",
            //        column: "SenseId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_glosses_LanguageId",
            //        table: "glosses",
            //        column: "LanguageId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_glosses_SenseId",
            //        table: "glosses",
            //        column: "SenseId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_inflections_LemmaId",
            //        table: "inflections",
            //        column: "LemmaId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_kanji_Character",
            //        table: "kanji",
            //        column: "Character",
            //        unique: true);

            //    migrationBuilder.CreateIndex(
            //        name: "IX_kanji_elements_EntryId",
            //        table: "kanji_elements",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_languages_Code",
            //        table: "languages",
            //        column: "Code",
            //        unique: true);

            //    migrationBuilder.CreateIndex(
            //        name: "IX_lemmas_WordId",
            //        table: "lemmas",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_media_EntryId",
            //        table: "media",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_media_WordId",
            //        table: "media",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_media_store_OwnerId",
            //        table: "media_store",
            //        column: "OwnerId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_ocr_jobs_MediaId",
            //        table: "ocr_jobs",
            //        column: "MediaId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_ocr_jobs_UserId",
            //        table: "ocr_jobs",
            //        column: "UserId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_ocr_results_LinkWordId",
            //        table: "ocr_results",
            //        column: "LinkWordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_ocr_results_OcrJobId",
            //        table: "ocr_results",
            //        column: "OcrJobId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_reading_elements_EntryId",
            //        table: "reading_elements",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_review_logs_CardId",
            //        table: "review_logs",
            //        column: "CardId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_review_logs_CardStateId",
            //        table: "review_logs",
            //        column: "CardStateId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_review_logs_UserId",
            //        table: "review_logs",
            //        column: "UserId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_senses_EntryId",
            //        table: "senses",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_stats_word_freq_WordId",
            //        table: "stats_word_freq",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_synonyms_cache_WordId",
            //        table: "synonyms_cache",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_synsets_SenseId",
            //        table: "synsets",
            //        column: "SenseId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_tags_Name",
            //        table: "tags",
            //        column: "Name",
            //        unique: true);

            //    migrationBuilder.CreateIndex(
            //        name: "IX_translations_EntryId",
            //        table: "translations",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_translations_LanguageId",
            //        table: "translations",
            //        column: "LanguageId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_translations_WordId",
            //        table: "translations",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_users_Email",
            //        table: "users",
            //        column: "Email",
            //        unique: true);

            //    migrationBuilder.CreateIndex(
            //        name: "IX_users_Username",
            //        table: "users",
            //        column: "Username",
            //        unique: true);

            //    migrationBuilder.CreateIndex(
            //        name: "IX_word_kanji_KanjiId",
            //        table: "word_kanji",
            //        column: "KanjiId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_word_relations_RelatedWordId",
            //        table: "word_relations",
            //        column: "RelatedWordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_word_relations_WordId",
            //        table: "word_relations",
            //        column: "WordId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_word_tags_TagId",
            //        table: "word_tags",
            //        column: "TagId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_word_tags_WordId_TagId",
            //        table: "word_tags",
            //        columns: new[] { "WordId", "TagId" });

            //    migrationBuilder.CreateIndex(
            //        name: "IX_words_EntryId",
            //        table: "words",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_WordToEntries_EntryId",
            //        table: "WordToEntries",
            //        column: "EntryId");

            //    migrationBuilder.CreateIndex(
            //        name: "IX_WordToEntries_WordId",
            //        table: "WordToEntries",
            //        column: "WordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_calls");

            migrationBuilder.DropTable(
                name: "examples");

            migrationBuilder.DropTable(
                name: "glosses");

            migrationBuilder.DropTable(
                name: "import_jobs");

            migrationBuilder.DropTable(
                name: "inflections");

            migrationBuilder.DropTable(
                name: "kanji_elements");

            migrationBuilder.DropTable(
                name: "media");

            migrationBuilder.DropTable(
                name: "ocr_results");

            migrationBuilder.DropTable(
                name: "reading_elements");

            migrationBuilder.DropTable(
                name: "review_logs");

            migrationBuilder.DropTable(
                name: "stats_word_freq");

            migrationBuilder.DropTable(
                name: "synonyms_cache");

            migrationBuilder.DropTable(
                name: "synsets");

            migrationBuilder.DropTable(
                name: "translations");

            migrationBuilder.DropTable(
                name: "word_kanji");

            migrationBuilder.DropTable(
                name: "word_relations");

            migrationBuilder.DropTable(
                name: "word_tags");

            migrationBuilder.DropTable(
                name: "WordToEntries");

            migrationBuilder.DropTable(
                name: "lemmas");

            migrationBuilder.DropTable(
                name: "ocr_jobs");

            migrationBuilder.DropTable(
                name: "card_states");

            migrationBuilder.DropTable(
                name: "senses");

            migrationBuilder.DropTable(
                name: "languages");

            migrationBuilder.DropTable(
                name: "kanji");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "media_store");

            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "decks");

            migrationBuilder.DropTable(
                name: "words");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "entries");
        }
    }
}
