using Dict.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dict.Data
{
    // DbContext with DbSets for all tables
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {


            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot config = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();
                string connString = config.GetConnectionString("DefaultConnection");

                if (!string.IsNullOrEmpty(connString))
                {
                    optionsBuilder.UseSqlServer(connString);
                }
                else
                {
                    Console.WriteLine("Warning: No connection string found.");
                }
            }

            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        public DbSet<SearchMiss> SearchMiss { get; set; }
        public DbSet<SiteStatsHistory> SiteStatsHistorys { get; set; }
        // Languages
        public DbSet<Language> Languages { get; set; }

        // Kanji
        public DbSet<Kanji> Kanji { get; set; }
        public DbSet<WordKanji> WordKanji { get; set; }
        public DbSet<KanjiExample> KanjiExamples { get; set; }

        // Lemmas / Inflections
        public DbSet<Lemma> Lemmas { get; set; }
        public DbSet<Inflection> Inflections { get; set; }

        // Core dictionary
        public DbSet<Entry> Entries { get; set; }
        public DbSet<KanjiElement> KanjiElements { get; set; }
        public DbSet<ReadingElement> ReadingElements { get; set; }
        public DbSet<Sense> Senses { get; set; }
        public DbSet<Gloss> Glosses { get; set; }
        public DbSet<Example> Examples { get; set; }
        public DbSet<SynsetEntry> SynsetEntries { get; set; }
        public DbSet<SynonymItem> SynonymItems { get; set; }

        // Words & mappings
        public DbSet<Word> Words { get; set; }
        public DbSet<WordToEntry> WordToEntries { get; set; }

        // Media
        public DbSet<Media> Media { get; set; }

        // Relations, tags, translations
        public DbSet<WordRelation> WordRelations { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<WordTag> WordTags { get; set; }
        public DbSet<Translation> Translations { get; set; }

        // Admin / import / api
        public DbSet<ImportJob> ImportJobs { get; set; }
        public DbSet<ApiCall> ApiCalls { get; set; }

        // Decks & flashcards
        public DbSet<Deck> Decks { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardState> CardStates { get; set; }
        public DbSet<ReviewLog> ReviewLogs { get; set; }

        // Media store / OCR
        public DbSet<MediaStore> MediaStore { get; set; }
        public DbSet<OcrJob> OcrJobs { get; set; }
        public DbSet<OcrResult> OcrResults { get; set; }

        // Stats / caches
        public DbSet<StatsWordFreq> StatsWordFreq { get; set; }
        public DbSet<SynonymsCache> SynonymsCache { get; set; }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectVocabulary> ProjectVocabularies { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
        public DbSet<WorkspaceInvitation> WorkspaceInvitations { get; set; }
        public DbSet<FileComment> FileComments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 2. Cấu hình WorkspaceInvitation: Chặn xóa User kéo theo xóa lời mời
            modelBuilder.Entity<WorkspaceInvitation>()
                .HasOne(wi => wi.Invitee)
                .WithMany()
                .HasForeignKey(wi => wi.InviteeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkspaceInvitation>()
                .HasOne(wi => wi.Inviter)
                .WithMany()
                .HasForeignKey(wi => wi.InviterId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Cấu hình FileComment: Chặn xóa Comment cha kéo theo xóa Comment con
            modelBuilder.Entity<FileComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SearchMiss>(b =>
            {
                b.ToTable("SearchMisses");

                b.HasKey(sm => sm.Id);

                b.Property(sm => sm.SearchTerm)
                    .IsRequired() 
                    .HasMaxLength(255); 

                b.Property(sm => sm.NormalizedTerm)
                    .IsRequired()
                    .HasMaxLength(255);

                b.Property(sm => sm.SearchCount)
                    .IsRequired()
                    .HasColumnType("int")
                    .HasDefaultValue(1);

                b.Property(sm => sm.LastSearchedAt)
                    .IsRequired()
                    .HasColumnType("datetime2");
                b.HasIndex(sm => sm.NormalizedTerm)
                    .IsUnique();
            });

            modelBuilder.Entity<SiteStatsHistory>(b =>
            {
                b.ToTable("SiteStatsHistory");

                b.HasKey(sh => sh.Date);

                b.Property(sh => sh.Date)
                    .IsRequired()
                    .HasColumnType("date");

                b.Property(sh => sh.TotalUsers).IsRequired();
                b.Property(sh => sh.NewUsersToday).IsRequired();
                b.Property(sh => sh.TotalEntries).IsRequired();
                b.Property(sh => sh.TotalReviewsToday).IsRequired();
                b.Property(sh => sh.TotalApiErrorsToday).IsRequired();
            });

            // languages
            modelBuilder.Entity<Language>(b =>
            {
                b.ToTable("languages");
                b.HasKey(x => x.Id);
                b.Property(x => x.Code).IsRequired().HasMaxLength(8);
                b.HasIndex(x => x.Code).IsUnique();
                b.Property(x => x.Name).HasMaxLength(64);
                b.Property(x => x.CreatedAt);
                b.Property(x => x.UpdatedAt);

                b.HasMany(x => x.Glosses).WithOne(g => g.Language).HasForeignKey(g => g.LanguageId).OnDelete(DeleteBehavior.Restrict);
                b.HasMany(x => x.Translations).WithOne(t => t.Language).HasForeignKey(t => t.LanguageId).OnDelete(DeleteBehavior.Restrict);
            });

            // kanji
            modelBuilder.Entity<Kanji>(b =>
            {
                b.ToTable("kanji");
                b.HasKey(x => x.Id);
                b.Property(x => x.Character).IsRequired().HasColumnType("nchar(1)");
                b.HasIndex(x => x.Character).IsUnique();
                b.Property(x => x.StrokeCount);
                b.Property(x => x.Grade);
                b.Property(x => x.JlptLevel).HasMaxLength(8);
                b.Property(x => x.Meaning);
                b.Property(x => x.Freq);
                b.Property(x => x.CreatedAt);
                b.Property(x => x.UpdatedAt);

                b.HasMany(x => x.WordKanji).WithOne(wk => wk.Kanji).HasForeignKey(wk => wk.KanjiId);
                b.HasMany(k => k.KanjiExamples).WithOne(ke => ke.Kanji).HasForeignKey(ke => ke.KanjiId).OnDelete(DeleteBehavior.Cascade);
            });

            // word_kanji (many-to-many manual join)
            modelBuilder.Entity<WordKanji>(b =>
            {
                b.ToTable("word_kanji");
                b.HasKey(x => new { x.WordId, x.KanjiId });

                b.HasOne(x => x.Word).WithMany(w => w.WordKanji).HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Kanji).WithMany(k => k.WordKanji).HasForeignKey(x => x.KanjiId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<KanjiExample>(b =>
            {
                b.ToTable("KanjiExample");
                b.HasKey(x => x.Id);
                b.Property(x => x.ExampleType).IsRequired() .HasColumnType("nvarchar(50)");
                b.Property(x => x.ReadingGroup).HasColumnType("nvarchar(255)"); 
                b.Property(x => x.Word).IsRequired().HasColumnType("nvarchar(255)");
                b.Property(x => x.Meaning).IsRequired().HasColumnType("nvarchar(max)");
                b.Property(x => x.Reading).IsRequired().HasColumnType("nvarchar(255)");
                b.Property(x => x.HanViet).HasColumnType("nvarchar(255)"); 
                b.HasOne(ke => ke.Kanji).WithMany(k => k.KanjiExamples).HasForeignKey(ke => ke.KanjiId).OnDelete(DeleteBehavior.Cascade);
            });


            // lemmas
            modelBuilder.Entity<Lemma>(b =>
            {
                b.ToTable("lemmas");
                b.HasKey(x => x.Id);
                b.Property(x => x.LemmaText).HasMaxLength(255);
                b.Property(x => x.Pos).HasMaxLength(64);
                b.HasOne(x => x.Word).WithMany(w => w.Lemmas).HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.SetNull);
                b.HasMany(x => x.Inflections).WithOne(i => i.Lemma).HasForeignKey(i => i.LemmaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Inflection>(b =>
            {
                b.ToTable("inflections");
                b.HasKey(x => x.Id);
                b.Property(x => x.FormText).HasMaxLength(255);
                b.Property(x => x.FormType).HasMaxLength(64);
            });

            // entries and related
            modelBuilder.Entity<Entry>(b =>
            {
                // --- BƯỚC 1: Cấu hình "Cơ bắp" (Bảng Gầy - entries) ---
                b.Property(x => x.Type).HasMaxLength(32);
                b.Property(x => x.Label).HasMaxLength(450).UseCollation("Japanese_CS_AS_KS_WS"); // Đã hạ xuống 450 để khớp Index
                b.Property(x => x.Phonetic).HasMaxLength(255).UseCollation("Japanese_CS_AS_KS_WS");
                b.Property(x => x.Romaji).HasMaxLength(255);
                b.Property(x => x.EntryCategory).HasMaxLength(50);

                // ShortMean: ĐÃ ĐƯA VỀ BẢNG GẦY
                // Dùng nvarchar để hỗ trợ tiếng Nhật/Việt và giới hạn độ dài để giữ bảng nhẹ
                b.Property(x => x.ShortMean)
                 .HasMaxLength(450)
                 .IsUnicode(true);

                // RawJson & CommentRawJson: Vẫn dùng tuyệt chiêu UTF-8 vì nó ở bảng Béo
                b.Property(x => x.RawJson)
                 .IsUnicode(false)
                 .HasColumnType("varchar(max)")
                 .UseCollation("Latin1_General_100_CI_AS_SC_UTF8");

                b.Property(x => x.CommentRawJson)
                 .IsUnicode(false)
                 .HasColumnType("varchar(max)")
                 .UseCollation("Latin1_General_100_CI_AS_SC_UTF8");

                // --- BƯỚC 2: Cấu hình "Phân nhà" (Entity Splitting) ---
                b.ToTable("entries");

                b.SplitToTable("entry_details", t =>
                {
                    // ShortMean ĐÃ BỊ LOẠI KHỎI ĐÂY để nó nằm ở bảng chính
                    t.Property(x => x.RawJson);
                    t.Property(x => x.CommentRawJson);
                    t.Property(x => x.JsonErrorMessage);
                    t.Property(x => x.JsonProcessingStatus);
                    t.Property(x => x.SynsetProcessingStatus);
                });

                // --- BƯỚC 3: Index & Relationships ---
                b.HasKey(x => x.Id);

                // Index tối ưu cho Autocomplete (Sử dụng Covering Index nếu EF Core hỗ trợ Include)
                // Lưu ý: EF Core 6.0+ hỗ trợ .IncludeProperties()
                b.HasIndex(x => x.Label, "IX_entries_SmartSearch")
                 .IncludeProperties(x => new { x.Phonetic, x.Type, x.Weight, x.ShortMean });

                b.HasIndex(x => new { x.EntSeq, x.Type }).IsUnique();

                // Các quan hệ giữ nguyên
                b.HasMany(x => x.KanjiElements).WithOne(k => k.Entry).HasForeignKey(k => k.EntryId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.ReadingElements).WithOne(r => r.Entry).HasForeignKey(r => r.EntryId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Senses).WithOne(s => s.Entry).HasForeignKey(s => s.EntryId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Words).WithOne(w => w.Entry).HasForeignKey(w => w.EntryId).OnDelete(DeleteBehavior.SetNull);
                b.HasMany(x => x.WordToEntries).WithOne(m => m.Entry).HasForeignKey(m => m.EntryId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Media).WithOne(m => m.Entry).HasForeignKey(m => m.EntryId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Translations).WithOne(t => t.Entry).HasForeignKey(t => t.EntryId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<KanjiElement>(b =>
            {
                b.ToTable("kanji_elements");
                b.HasKey(x => x.Id);
                b.Property(x => x.Keb).HasMaxLength(255);
                b.Property(x => x.KeInf).HasMaxLength(255);
                b.Property(x => x.KePri).HasMaxLength(255);
            });

            modelBuilder.Entity<ReadingElement>(b =>
            {
                b.ToTable("reading_elements");
                b.HasKey(x => x.Id);
                b.Property(x => x.Reb).HasMaxLength(255);
                b.Property(x => x.ReNoKanji).HasMaxLength(255);
                b.Property(x => x.RePri).HasMaxLength(255);
            });

            modelBuilder.Entity<Sense>(b =>
            {
                b.ToTable("senses");
                b.HasKey(x => x.Id);
                b.Property(x => x.Pos).HasMaxLength(128);
                b.Property(x => x.Field).HasMaxLength(128);
                b.Property(x => x.Misc).HasMaxLength(128);
                b.Property(x => x.SInf).HasColumnType("nvarchar(max)");
                b.Property(x => x.Dialect).HasMaxLength(64);
                b.Property(x => x.SenseOrder);

                b.HasMany(x => x.Glosses).WithOne(g => g.Sense).HasForeignKey(g => g.SenseId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Examples).WithOne(e => e.Sense).HasForeignKey(e => e.SenseId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.SynsetEntries).WithOne(s => s.Sense).HasForeignKey(s => s.SenseId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Gloss>(b =>
            {
                b.ToTable("glosses");
                b.HasKey(x => x.Id);
                b.Property(x => x.Text);
                b.Property(x => x.GType).HasMaxLength(32);
                b.Property(x => x.GGend).HasMaxLength(32);
                b.Property(x => x.Priority).HasMaxLength(64);
            });

            modelBuilder.Entity<Example>(b =>
            {
                b.ToTable("examples");
                b.HasKey(x => x.Id);
                b.Property(x => x.ContentJp);
                b.Property(x => x.ContentTranslated);
                b.Property(x => x.Transcription).HasMaxLength(512);
                b.Property(x => x.SourceRef).HasMaxLength(256);
            });

            modelBuilder.Entity<SynsetEntry>(b =>
            {
                b.ToTable("SynsetEntries"); // Sửa tên bảng cho đúng
                b.HasKey(x => x.Id);
                b.Property(x => x.BaseForm).HasMaxLength(255);
                b.Property(x => x.Pos).HasMaxLength(64);
                // b.Property(x => x.DefinitionId); // Bỏ dòng này nếu DefinitionId có thể null

                // SỬA LỖI: Xóa dòng "b.Property(x => x.SynonymItems);" và thay bằng định nghĩa quan hệ
                b.HasMany(se => se.SynonymItems)        // Một SynsetEntry có nhiều SynonymItem
                 .WithOne(si => si.SynsetEntry)         // Mỗi SynonymItem có một SynsetEntry
                 .HasForeignKey(si => si.SynsetEntryId) // Khóa ngoại là SynsetEntryId
                 .OnDelete(DeleteBehavior.Cascade);     // Nếu xóa cha thì xóa luôn con
            });

            modelBuilder.Entity<SynonymItem>(b =>
            {
                b.ToTable("SynonymItems"); // Chỉ định tên bảng
                b.HasKey(si => si.Id);
                b.Property(si => si.Word).IsRequired().HasMaxLength(255);
            });

            // words and mapping
            modelBuilder.Entity<Word>(b =>
            {
                b.ToTable("words");
                b.HasKey(x => x.Id);
                b.Property(x => x.WordText).IsRequired().HasMaxLength(255);
                b.Property(x => x.Phonetic).HasMaxLength(255);
                b.Property(x => x.Romaji).HasMaxLength(255);
                b.Property(x => x.ShortMean).HasColumnType("nvarchar(max)");
                b.Property(x => x.Weight);
                b.Property(x => x.MobileId);
                b.Property(x => x.CreatedAt);
                b.Property(x => x.UpdatedAt);

                b.HasMany(x => x.WordKanji).WithOne(wk => wk.Word).HasForeignKey(wk => wk.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.WordToEntries).WithOne(m => m.Word).HasForeignKey(m => m.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Media).WithOne(m => m.Word).HasForeignKey(m => m.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Relations).WithOne(r => r.Word).HasForeignKey(r => r.WordId).OnDelete(DeleteBehavior.Restrict);
                b.HasMany(x => x.WordTags).WithOne(wt => wt.Word).HasForeignKey(wt => wt.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Translations).WithOne(t => t.Word).HasForeignKey(t => t.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.OcrResults).WithOne(o => o.LinkWord).HasForeignKey(o => o.LinkWordId).OnDelete(DeleteBehavior.SetNull);
                b.HasMany(x => x.Cards).WithOne(c => c.Word).HasForeignKey(c => c.WordId).OnDelete(DeleteBehavior.SetNull);

                b.HasMany<WordToEntry>().WithOne().HasForeignKey(wte => wte.WordId);
            });

            modelBuilder.Entity<WordToEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Word)
                      .WithMany(w => w.WordToEntries)
                      .HasForeignKey(e => e.WordId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Entry)
                      .WithMany(e => e.WordToEntries)
                      .HasForeignKey(e => e.EntryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Media>(b =>
            {
                b.ToTable("media");
                b.HasKey(x => x.Id);
                b.Property(x => x.Url).HasMaxLength(2000);
                b.Property(x => x.Caption).HasMaxLength(1000);
                b.Property(x => x.MediaType).HasMaxLength(64);
                b.Property(x => x.Source).HasMaxLength(255);
                b.Property(x => x.CreatedAt);
            });

            modelBuilder.Entity<WordRelation>(b =>
            {
                b.ToTable("word_relations");
                b.HasKey(x => x.Id);

                b.Property(x => x.RelationType).HasMaxLength(64);
                b.Property(x => x.Note);

                // Mối quan hệ thứ nhất (từ WordId -> Word.Relations) - Dòng này đã đúng
                b.HasOne(x => x.Word)
                 .WithMany(w => w.Relations) // Chỉ đến ICollection<WordRelation> Relations
                 .HasForeignKey(x => x.WordId)
                 .OnDelete(DeleteBehavior.Restrict);

                // SỬA LỖI: Mối quan hệ thứ hai (từ RelatedWordId -> Word.AppearingInRelations)
                b.HasOne(x => x.RelatedWord)
                 .WithMany(w => w.AppearingInRelations) // Sửa WithMany() thành WithMany(w => w.AppearingInRelations)
                 .HasForeignKey(x => x.RelatedWordId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Tag>(b =>
            {
                b.ToTable("tags");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired().HasMaxLength(128);
                b.HasIndex(x => x.Name).IsUnique();
                b.Property(x => x.Description);
            });

            modelBuilder.Entity<WordTag>(b =>
            {
                b.ToTable("word_tags");
                b.HasKey(x => x.Id);
                b.HasIndex(x => new { x.WordId, x.TagId });
            });

            modelBuilder.Entity<Translation>(b =>
            {
                b.ToTable("translations");
                b.HasKey(x => x.Id);
                b.Property(x => x.Definition);
                b.Property(x => x.Kind).HasMaxLength(64);
                b.Property(x => x.ExamplesJson);
                b.Property(x => x.Source).HasMaxLength(255);
                b.Property(x => x.CreatedAt);
                b.Property(x => x.UpdatedAt);

                b.HasOne(x => x.Language).WithMany(l => l.Translations).HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Word).WithMany(w => w.Translations).HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Entry).WithMany(e => e.Translations).HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ImportJob>(b =>
            {
                b.ToTable("import_jobs");
                b.HasKey(x => x.Id);
                b.Property(x => x.JobType).HasMaxLength(64);
                b.Property(x => x.Status).HasMaxLength(32);
                b.Property(x => x.StartedAt);
                b.Property(x => x.FinishedAt);
                b.Property(x => x.Meta);
            });

            modelBuilder.Entity<ApiCall>(b =>
            {
                b.ToTable("api_calls");
                b.HasKey(x => x.Id);
                b.Property(x => x.Endpoint).HasMaxLength(512);
                b.Property(x => x.RequestJson);
                b.Property(x => x.ResponseStatus);
                b.Property(x => x.ResponseTimeMs);
                b.Property(x => x.CreatedAt);
            });
            
            modelBuilder.Entity<Deck>(b =>
            {
                b.ToTable("decks");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(255);
                b.Property(x => x.Description);
                b.Property(x => x.IsPublic);
                b.Property(x => x.CreatedAt);
                b.Property(x => x.UpdatedAt);

                b.HasOne(x => x.User).WithMany(u => u.Decks).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Card>(b =>
            {
                b.ToTable("cards");
                b.HasKey(x => x.Id);
                b.Property(x => x.Template);
                b.Property(x => x.FrontText);
                b.Property(x => x.BackText);
                b.Property(x => x.Tags);
                b.Property(x => x.CreatedAt);
                b.Property(x => x.UpdatedAt);

                b.HasOne(x => x.Deck).WithMany(d => d.Cards).HasForeignKey(x => x.DeckId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Word).WithMany(w => w.Cards).HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CardState>(entity =>
            {
                entity.ToTable("card_states");
                entity.HasKey(cs => cs.Id);

                entity.Property(cs => cs.DueDate);
                entity.Property(cs => cs.Interval);
                entity.Property(cs => cs.Ease);
                entity.Property(cs => cs.Reps);
                entity.Property(cs => cs.Lapses);
                entity.Property(cs => cs.DeckPosition);
                entity.Property(cs => cs.LastReviewedAt);
                entity.Property(cs => cs.Suspended);
                entity.Property(cs => cs.CreatedAt);
                entity.Property(cs => cs.UpdatedAt);

                // CardState - Card
                entity.HasOne(cs => cs.Card)
                      .WithMany(c => c.CardStates)
                      .HasForeignKey(cs => cs.CardId)
                      .OnDelete(DeleteBehavior.Restrict);  // tránh multiple cascade

                // CardState - User
                entity.HasOne(cs => cs.User)
                      .WithMany(u => u.CardStates)
                      .HasForeignKey(cs => cs.UserId)
                      .OnDelete(DeleteBehavior.Restrict);  // cũng tránh multiple cascade
            });


            modelBuilder.Entity<ReviewLog>(entity =>
            {
                entity.ToTable("review_logs");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Timestamp);
                entity.Property(r => r.Quality);
                entity.Property(r => r.TimeTakenMs);
                entity.Property(r => r.PreviousInterval);
                entity.Property(r => r.NewInterval);
                entity.Property(r => r.Ease);
                entity.Property(r => r.Note).IsRequired();

                // ReviewLog - CardState
                entity.HasOne(r => r.CardState)
                      .WithMany(cs => cs.ReviewLogs)
                      .HasForeignKey(r => r.CardStateId)
                      .OnDelete(DeleteBehavior.Restrict); // tránh cascade loop

                // ReviewLog - Card
                entity.HasOne(r => r.Card)
                      .WithMany(c => c.ReviewLogs)
                      .HasForeignKey(r => r.CardId)
                      .OnDelete(DeleteBehavior.Restrict); // tránh cascade loop

                // ReviewLog - User
                entity.HasOne(r => r.User)
                      .WithMany(u => u.ReviewLogs)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // giữ cascade ở đây
            });


            modelBuilder.Entity<MediaStore>(b =>
            {
                b.ToTable("media_store");
                b.HasKey(x => x.Id);
                b.Property(x => x.FileName).HasMaxLength(512);
                b.Property(x => x.MimeType).HasMaxLength(64);
                b.Property(x => x.SizeBytes);
                b.Property(x => x.StorageUrl).HasMaxLength(2000);
                b.Property(x => x.Sha256).HasMaxLength(128);
                b.Property(x => x.CreatedAt);

                // ✅ SỬA LẠI KHÓA NGOẠI CHO CHUẨN:
                b.HasOne(x => x.Owner).WithMany(u => u.MediaStore).HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Workspace).WithMany(w => w.MediaFiles).HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OcrJob>(entity =>
            {
                entity.ToTable("ocr_jobs");
                entity.HasKey(j => j.Id);

                entity.Property(j => j.Status)
                      .IsRequired()
                      .HasMaxLength(32);

                entity.Property(j => j.DetectedText)
                      .IsRequired();

                entity.Property(j => j.CreatedAt);
                entity.Property(j => j.UpdatedAt);

                entity.Property(x => x.PageNumber).HasDefaultValue(1); // Mặc định là trang 1
                entity.Property(x => x.ProjectId); // Nullable

                entity.HasOne(x => x.Project)
                      .WithMany(p => p.OcrJobs)
                      .HasForeignKey(x => x.ProjectId)
                      .OnDelete(DeleteBehavior.SetNull);
                // Relation: OcrJob - User (NO CASCADE to avoid multiple cascade paths)
                entity.HasOne(j => j.User)
                      .WithMany(u => u.OcrJobs)
                      .HasForeignKey(j => j.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation: OcrJob - MediaStore (also NO CASCADE)
                entity.HasOne(j => j.Media)
                      .WithMany(m => m.OcrJobs)
                      .HasForeignKey(j => j.MediaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OcrResult>(b =>
            {
                b.ToTable("ocr_results");
                b.HasKey(x => x.Id);
                b.Property(x => x.PageNumber);
                b.Property(x => x.WordText).HasMaxLength(255);
                b.Property(x => x.BoundingBox);
                b.Property(x => x.Confidence);
                b.HasOne(x => x.LinkWord).WithMany(w => w.OcrResults).HasForeignKey(x => x.LinkWordId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Project>(b =>
            {
                b.ToTable("projects");
                b.HasKey(x => x.Id);

                b.Property(x => x.Name).IsRequired().HasMaxLength(255);
                b.Property(x => x.Description);
                b.Property(x => x.WorkspaceId).IsRequired();
                b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()"); // Lưu giờ chuẩn quốc tế

                // Quan hệ 1-N: 1 User (Owner) có nhiều Projects
                b.HasOne(x => x.Workspace)
                 .WithMany(w => w.Projects) // Để trống nếu class User của bạn không có ICollection<Project>
                 .HasForeignKey(x => x.WorkspaceId)
                 .OnDelete(DeleteBehavior.Cascade); // Xóa user thì xóa luôn project của họ

                b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // 2. Cấu hình bảng project_vocabularies
            modelBuilder.Entity<ProjectVocabulary>(b =>
            {
                b.ToTable("project_vocabularies");
                b.HasKey(x => x.Id);

                b.Property(x => x.WordText).IsRequired().HasMaxLength(255);
                b.Property(x => x.ContextMeaning).IsRequired();
                b.Property(x => x.AddedBy).IsRequired();
                b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Quan hệ 1-N: 1 Project có nhiều Vocabularies
                b.HasOne(x => x.Project)
                 .WithMany(p => p.ProjectVocabularies)
                 .HasForeignKey(x => x.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade); // Xóa Project thì bay luôn từ vựng dự án đó

                // Quan hệ 1-N: User thêm từ vựng nào
                // LƯU Ý CHÍ PHẢI: Dùng Restrict chỗ này để tránh lỗi "Multiple Cascade Paths" kinh điển của SQL Server
                b.HasOne(x => x.UserAdded)
                 .WithMany()
                 .HasForeignKey(x => x.AddedBy)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Workspace>(b =>
            {
                b.ToTable("workspaces");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired().HasMaxLength(255);
                b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // 2. Cấu hình WorkspaceMember (Khóa chính kép)
            modelBuilder.Entity<WorkspaceMember>(b =>
            {
                b.ToTable("workspace_members");
                b.HasKey(x => new { x.WorkspaceId, x.UserId }); // 1 User chỉ join 1 Workspace 1 lần
                b.Property(x => x.Role).IsRequired().HasMaxLength(50);

                b.HasOne(x => x.Workspace).WithMany(w => w.Members).HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<StatsWordFreq>(b =>
            {
                b.ToTable("stats_word_freq");
                b.HasKey(x => x.Id);

                b.HasOne(x => x.Entry)
                    .WithMany(e => e.FreqStats) // Trỏ đến ICollection FreqStats ta vừa tạo
                    .HasForeignKey(x => x.EntryId) // Dùng EntryId làm khóa ngoại
                    .OnDelete(DeleteBehavior.Cascade); // (Nên dùng Cascade: Nếu Entry bị xóa, thống kê của nó cũng bị xóa)

                b.Property(x => x.Occurrences);
                b.Property(x => x.LastSeenAt);
            });

            modelBuilder.Entity<SynonymsCache>(b =>
            {
                b.ToTable("synonyms_cache");
                b.HasKey(x => x.Id);
                b.Property(x => x.Data);
                b.Property(x => x.UpdatedAt);
                b.HasOne(x => x.Word).WithMany(w => w.SynonymsCaches).HasForeignKey(x => x.WordId).OnDelete(DeleteBehavior.Restrict);
            });

            // small conveniences
            modelBuilder.Entity<WordTag>().HasIndex(wt => new { wt.WordId, wt.TagId });
        }
    }
}
