namespace LittleQuranTales.Data;

public class AyahData
{
    public string Arabic { get; set; }
    public string TransKey { get; set; }
}

public class SurahData
{
    public string Id { get; set; }
    public string TitleKey { get; set; }
    public string SubtitleKey { get; set; }
    public AyahData[] Ayahs { get; set; }
}

public static class QuranSurahs
{
    public static SurahData[] All { get; } =
    {
        new SurahData
        {
            Id = "al-fil",
            TitleKey = "chapter_1_title",
            SubtitleKey = "chapter_1_subtitle",
            Ayahs = new[]
            {
                new AyahData { Arabic = "أَلَمْ تَرَ كَيْفَ فَعَلَ رَبُّكَ بِأَصْحَابِ الْفِيلِ", TransKey = "ayah_1_trans" },
                new AyahData { Arabic = "أَلَمْ يَجْعَلْ كَيْدَهُمْ فِي تَضْلِيلٍ", TransKey = "ayah_2_trans" },
                new AyahData { Arabic = "وَأَرْسَلَ عَلَيْهِمْ طَيْرًا أَبَابِيلَ", TransKey = "ayah_3_trans" },
                new AyahData { Arabic = "تَرْمِيهِمْ بِحِجَارَةٍ مِنْ سِجِّيلٍ", TransKey = "ayah_4_trans" },
                new AyahData { Arabic = "فَجَعَلَهُمْ كَعَصْفٍ مَأْكُولٍ", TransKey = "ayah_5_trans" },
            }
        },
    };

    public static SurahData Get(string id)
    {
        foreach (var s in All)
            if (s.Id == id) return s;
        return null;
    }
}
