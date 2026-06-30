using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;

namespace LittleQuranTales.Data;

public class QuranDbService
{
    private readonly Dictionary<int, SurahInfo> _surahMap = new();
    private readonly List<SurahInfo> _surahs = new();

    public int TotalSurahs => _surahs.Count;
    public IReadOnlyList<SurahInfo> AllSurahs => _surahs;

    public string DatabasePath { get; private set; }

    public void Load(string dbPath)
    {
        _surahMap.Clear();
        _surahs.Clear();
        DatabasePath = dbPath;

        if (!File.Exists(dbPath))
            throw new InvalidOperationException($"Quran database not found: {dbPath}");

        using var conn = GetConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, revelation_place, revelation_order, bismillah_pre, " +
            "name_simple, name_complex, name_arabic, verses_count, " +
            "page_start, page_end, translated_name FROM chapters ORDER BY id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var surah = new SurahInfo
            {
                Number = reader.GetInt32(0),
                RevelationType = reader.IsDBNull(1) ? "" : reader.GetString(1),
                RevelationOrder = reader.GetInt32(2),
                BismillahPre = reader.GetInt32(3) == 1,
                NameSimple = reader.IsDBNull(4) ? "" : reader.GetString(4),
                NameComplex = reader.IsDBNull(5) ? "" : reader.GetString(5),
                NameArabic = reader.IsDBNull(6) ? "" : reader.GetString(6),
                VersesCount = reader.GetInt32(7),
                PageStart = reader.GetInt32(8),
                PageEnd = reader.GetInt32(9),
                TranslatedName = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };

            _surahs.Add(surah);
            _surahMap[surah.Number] = surah;
        }
    }

    public SqliteConnection GetConnection()
    {
        return new SqliteConnection($"Data Source={DatabasePath}");
    }

    public SurahInfo GetSurah(int number)
    {
        _surahMap.TryGetValue(number, out var surah);
        return surah;
    }

    public SurahInfo GetSurahByEnglishName(string englishName)
    {
        foreach (var s in _surahs)
        {
            if (string.Equals(s.NameSimple, englishName, StringComparison.OrdinalIgnoreCase))
                return s;
            if (string.Equals(s.NameComplex, englishName, StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }

    public VerseInfo GetAyah(int surahNumber, int ayahNumber)
    {
        using var conn = GetConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, chapter_id, verse_number, verse_key, verse_index, " +
            "text_uthmani, text_indopak, juz_number, hizb_number, page_number, " +
            "sajdah_type, sajdah_number, translation_id, translation_en, audio_url, transliteration " +
            "FROM verses WHERE chapter_id = $ch AND verse_number = $ay";
        cmd.Parameters.AddWithValue("$ch", surahNumber);
        cmd.Parameters.AddWithValue("$ay", ayahNumber);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return ReadVerse(reader);
        }
        return null;
    }

    public List<VerseInfo> GetAyahs(int surahNumber)
    {
        var result = new List<VerseInfo>();

        using var conn = GetConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, chapter_id, verse_number, verse_key, verse_index, " +
            "text_uthmani, text_indopak, juz_number, hizb_number, page_number, " +
            "sajdah_type, sajdah_number, translation_id, translation_en, audio_url, transliteration " +
            "FROM verses WHERE chapter_id = $ch ORDER BY verse_number";
        cmd.Parameters.AddWithValue("$ch", surahNumber);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadVerse(reader));
        }
        return result;
    }

    public WordInfo[] GetWords(int verseId)
    {
        var result = new List<WordInfo>();

        using var conn = GetConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, verse_id, position, char_type_name, text_uthmani, " +
            "translation_text, transliteration_text, audio_url " +
            "FROM words WHERE verse_id = $vid ORDER BY position";
        cmd.Parameters.AddWithValue("$vid", verseId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new WordInfo
            {
                Id = reader.GetInt32(0),
                VerseId = reader.GetInt32(1),
                Position = reader.GetInt32(2),
                CharTypeName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                TextUthmani = reader.IsDBNull(4) ? "" : reader.GetString(4),
                TranslationText = reader.IsDBNull(5) ? "" : reader.GetString(5),
                TransliterationText = reader.IsDBNull(6) ? "" : reader.GetString(6),
                AudioUrl = reader.IsDBNull(7) ? "" : reader.GetString(7)
            });
        }
        return result.ToArray();
    }

    public string[][] GetAyahWords(int surahNumber)
    {
        var ayahs = GetAyahs(surahNumber);
        if (ayahs == null || ayahs.Count == 0) return null;

        var result = new string[ayahs.Count][];
        for (var i = 0; i < ayahs.Count; i++)
        {
            var words = GetWords(ayahs[i].Id);
            var wordTexts = new List<string>();
            foreach (var w in words)
            {
                if (w.CharTypeName == "word" && !string.IsNullOrEmpty(w.TextUthmani))
                    wordTexts.Add(w.TextUthmani);
            }
            result[i] = wordTexts.ToArray();
        }
        return result;
    }

    private static VerseInfo ReadVerse(SqliteDataReader reader)
    {
        return new VerseInfo
        {
            Id = reader.GetInt32(0),
            ChapterId = reader.GetInt32(1),
            VerseNumber = reader.GetInt32(2),
            VerseKey = reader.IsDBNull(3) ? "" : reader.GetString(3),
            VerseIndex = reader.GetInt32(4),
            TextUthmani = reader.IsDBNull(5) ? "" : reader.GetString(5),
            TextIndopak = reader.IsDBNull(6) ? "" : reader.GetString(6),
            JuzNumber = reader.GetInt32(7),
            HizbNumber = reader.GetInt32(8),
            PageNumber = reader.GetInt32(9),
            SajdahType = reader.IsDBNull(10) ? null : reader.GetString(10),
            SajdahNumber = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11),
            TranslationId = reader.IsDBNull(12) ? "" : reader.GetString(12),
            TranslationEn = reader.IsDBNull(13) ? "" : reader.GetString(13),
            Transliteration = reader.FieldCount > 15 && !reader.IsDBNull(15) ? reader.GetString(15) : "",
            AudioUrl = reader.IsDBNull(14) ? "" : reader.GetString(14)
        };
    }

    public int GetSurahNumberFromId(string id)
    {
        return id switch
        {
            "al-fatiha" => 1,
            "al-baqarah" => 2,
            "ali-imran" => 3,
            "an-nisa" => 4,
            "al-maidah" => 5,
            "al-anam" => 6,
            "al-araf" => 7,
            "al-anfal" => 8,
            "at-tawbah" => 9,
            "yunus" => 10,
            "hud" => 11,
            "yusuf" => 12,
            "ar-rad" => 13,
            "ibrahim" => 14,
            "al-hijr" => 15,
            "an-nahl" => 16,
            "al-isra" => 17,
            "al-kahf" => 18,
            "maryam" => 19,
            "taha" => 20,
            "al-anbiya" => 21,
            "al-hajj" => 22,
            "al-muminun" => 23,
            "an-nur" => 24,
            "al-furqan" => 25,
            "ash-shuara" => 26,
            "an-naml" => 27,
            "al-qasas" => 28,
            "al-ankabut" => 29,
            "ar-rum" => 30,
            "luqman" => 31,
            "as-sajdah" => 32,
            "al-ahzab" => 33,
            "saba" => 34,
            "fatir" => 35,
            "ya-sin" => 36,
            "as-saffat" => 37,
            "sad" => 38,
            "az-zumar" => 39,
            "ghafir" => 40,
            "fussilat" => 41,
            "ash-shura" => 42,
            "az-zukhruf" => 43,
            "ad-dukhan" => 44,
            "al-jathiyah" => 45,
            "al-ahqaf" => 46,
            "muhammad" => 47,
            "al-fath" => 48,
            "al-hujurat" => 49,
            "qaf" => 50,
            "adh-dhariyat" => 51,
            "at-tur" => 52,
            "an-najm" => 53,
            "al-qamar" => 54,
            "ar-rahman" => 55,
            "al-waqiah" => 56,
            "al-hadid" => 57,
            "al-mujadilah" => 58,
            "al-hashr" => 59,
            "al-mumtahanah" => 60,
            "as-saff" => 61,
            "al-jumuah" => 62,
            "al-munafiqun" => 63,
            "at-taghabun" => 64,
            "at-talaq" => 65,
            "at-tahrim" => 66,
            "al-mulk" => 67,
            "al-qalam" => 68,
            "al-haqqah" => 69,
            "al-maarij" => 70,
            "nuh" => 71,
            "al-jinn" => 72,
            "al-muzzammil" => 73,
            "al-muddaththir" => 74,
            "al-qiyamah" => 75,
            "al-insan" => 76,
            "al-mursalat" => 77,
            "an-naba" => 78,
            "an-naziat" => 79,
            "abasa" => 80,
            "at-takwir" => 81,
            "al-infitar" => 82,
            "al-mutaffifin" => 83,
            "al-inshiqaq" => 84,
            "al-buruj" => 85,
            "at-tariq" => 86,
            "al-ala" => 87,
            "al-ghashiyah" => 88,
            "al-fajr" => 89,
            "al-balad" => 90,
            "ash-shams" => 91,
            "al-layl" => 92,
            "ad-duha" => 93,
            "ash-sharh" => 94,
            "at-tin" => 95,
            "al-alaq" => 96,
            "al-qadr" => 97,
            "al-bayyinah" => 98,
            "az-zalzalah" => 99,
            "al-adiyat" => 100,
            "al-qariah" => 101,
            "at-takathur" => 102,
            "al-asr" => 103,
            "al-humazah" => 104,
            "al-fil" => 105,
            "quraysh" => 106,
            "al-maun" => 107,
            "al-kawthar" => 108,
            "al-kafirun" => 109,
            "an-nasr" => 110,
            "al-masad" => 111,
            "al-ikhlas" => 112,
            "al-falaq" => 113,
            "an-nas" => 114,
            _ => -1
        };
    }
}

public class SurahInfo
{
    public int Number { get; set; }
    public string RevelationType { get; set; }
    public int RevelationOrder { get; set; }
    public bool BismillahPre { get; set; }
    public string NameSimple { get; set; }
    public string NameComplex { get; set; }
    public string NameArabic { get; set; }
    public int VersesCount { get; set; }
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string TranslatedName { get; set; }

    public string EnglishName => NameSimple;
    public string Name => NameArabic;
    public int AyahCount => VersesCount;
    public string EnglishNameTranslation => TranslatedName;
}

public class VerseInfo
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public int VerseNumber { get; set; }
    public string VerseKey { get; set; }
    public int VerseIndex { get; set; }
    public string TextUthmani { get; set; }
    public string TextIndopak { get; set; }
    public int JuzNumber { get; set; }
    public int HizbNumber { get; set; }
    public int PageNumber { get; set; }
    public string SajdahType { get; set; }
    public int? SajdahNumber { get; set; }
    public string TranslationId { get; set; }
    public string TranslationEn { get; set; }
    public string Transliteration { get; set; }
    public string AudioUrl { get; set; }

    public string Arabic => TextUthmani;

    public string[] GetWords()
    {
        return TextUthmani?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}

public class WordInfo
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public int Position { get; set; }
    public string CharTypeName { get; set; }
    public string TextUthmani { get; set; }
    public string TranslationText { get; set; }
    public string TransliterationText { get; set; }
    public string AudioUrl { get; set; }
}
