using System;
using System.Collections.Generic;

[Serializable]
public sealed class ThreatEducationContent
{
    public string ThreatId { get; }
    public string DisplayName { get; }
    public string Definition { get; }
    public string RealWorldExample { get; }
    public string Prevention { get; }

    public ThreatEducationContent(
        string threatId,
        string displayName,
        string definition,
        string realWorldExample,
        string prevention)
    {
        ThreatId = threatId;
        DisplayName = displayName;
        Definition = definition;
        RealWorldExample = realWorldExample;
        Prevention = prevention;
    }
}

/// <summary>
/// Single source of educational copy shared by the information panel and quiz flow.
/// </summary>
public static class ThreatEducationCatalog
{
    private static readonly Dictionary<string, ThreatEducationContent> Contents
        = new Dictionary<string, ThreatEducationContent>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Phishing",
                new ThreatEducationContent(
                    "Phishing",
                    "PHISHING",
                    "Phishing adalah upaya penipuan yang menyamar sebagai pihak tepercaya untuk mencuri kata sandi, kode OTP, data pribadi, atau informasi keuangan.",
                    "Korban menerima pesan yang mengatasnamakan bank dan diminta membuka tautan login palsu. Data yang dimasukkan kemudian dikirim kepada penyerang.",
                    "• Periksa alamat pengirim dan domain tautan.\n• Jangan membagikan OTP atau kata sandi.\n• Buka layanan melalui aplikasi atau alamat resmi.\n• Aktifkan autentikasi dua faktor.")
            },
            {
                "Malware",
                new ThreatEducationContent(
                    "Malware",
                    "MALWARE",
                    "Malware adalah perangkat lunak berbahaya yang dirancang untuk merusak sistem, mencuri data, memata-matai pengguna, atau mengambil alih perangkat.",
                    "Aplikasi yang diunduh dari sumber tidak resmi menyisipkan program berbahaya yang membaca data, menampilkan iklan, atau mengendalikan perangkat tanpa izin.",
                    "• Unduh aplikasi hanya dari sumber tepercaya.\n• Perbarui sistem dan aplikasi secara rutin.\n• Hindari lampiran atau tautan mencurigakan.\n• Gunakan perlindungan keamanan dan pindai perangkat.")
            },
            {
                "Ransomware",
                new ThreatEducationContent(
                    "Ransomware",
                    "RANSOMWARE",
                    "Ransomware adalah jenis malware yang mengunci perangkat atau mengenkripsi berkas, kemudian meminta tebusan agar akses dikembalikan.",
                    "Sebuah organisasi membuka lampiran berbahaya. Berkas penting berubah menjadi tidak dapat dibuka dan muncul pesan pembayaran tebusan.",
                    "• Buat cadangan data secara berkala dan terpisah.\n• Perbarui sistem untuk menutup celah keamanan.\n• Jangan membuka lampiran yang tidak dikenal.\n• Batasi hak akses dan segera isolasi perangkat terinfeksi.")
            }
        };

    public static int Count => Contents.Count;

    public static bool TryGet(string threatId, out ThreatEducationContent content)
    {
        if (string.IsNullOrWhiteSpace(threatId))
        {
            content = null;
            return false;
        }

        return Contents.TryGetValue(threatId, out content);
    }

    public static ThreatEducationContent Get(string threatId)
    {
        ThreatEducationContent content;
        return TryGet(threatId, out content) ? content : null;
    }
}
