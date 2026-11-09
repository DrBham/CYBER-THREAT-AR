using System;
using System.Collections.Generic;

public sealed class ThreatQuizQuestion
{
    public string Prompt { get; }
    public IReadOnlyList<string> Options { get; }
    public int CorrectOptionIndex { get; }
    public string Explanation { get; }

    public ThreatQuizQuestion(
        string prompt,
        string optionA,
        string optionB,
        string optionC,
        string optionD,
        int correctOptionIndex,
        string explanation)
    {
        if (correctOptionIndex < 0 || correctOptionIndex > 3)
            throw new ArgumentOutOfRangeException(nameof(correctOptionIndex));

        Prompt = prompt;
        Options = new[] { optionA, optionB, optionC, optionD };
        CorrectOptionIndex = correctOptionIndex;
        Explanation = explanation;
    }
}

public static class ThreatQuizCatalog
{
    private static readonly Dictionary<string, IReadOnlyList<ThreatQuizQuestion>> Questions
        = new Dictionary<string, IReadOnlyList<ThreatQuizQuestion>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Phishing",
                new[]
                {
                    new ThreatQuizQuestion(
                        "Manakah tanda yang paling kuat bahwa sebuah email kemungkinan merupakan phishing?",
                        "Email memakai logo perusahaan yang terlihat resmi.",
                        "Alamat pengirim menyerupai domain resmi dan meminta tindakan sangat mendesak.",
                        "Email memiliki tanda tangan serta nama pegawai.",
                        "Pesan dikirim pada jam kerja.",
                        1,
                        "Pelaku sering memakai domain yang hanya sedikit berbeda dan menciptakan rasa mendesak agar korban tidak memeriksa pesan dengan teliti."),
                    new ThreatQuizQuestion(
                        "Anda menerima tautan untuk memperbarui kata sandi akun. Apa tindakan paling aman?",
                        "Klik tautan lalu periksa apakah tampilannya meyakinkan.",
                        "Balas email untuk meminta pengirim memastikan tautannya.",
                        "Buka aplikasi atau situs resmi secara manual dan periksa notifikasi akun di sana.",
                        "Teruskan tautan kepada teman untuk dicoba.",
                        2,
                        "Masuk melalui aplikasi atau alamat resmi yang diketik sendiri mencegah Anda diarahkan ke situs login palsu."),
                    new ThreatQuizQuestion(
                        "Jika terlanjur memasukkan kata sandi pada situs phishing, apa langkah pertama yang tepat?",
                        "Ganti kata sandi melalui layanan resmi, aktifkan autentikasi dua faktor, dan laporkan kejadian.",
                        "Tunggu sampai akun benar-benar diambil alih.",
                        "Hapus riwayat browser saja.",
                        "Kirim kembali formulir dengan data palsu.",
                        0,
                        "Kredensial harus segera diamankan melalui kanal resmi. Autentikasi dua faktor dan pelaporan membantu membatasi penyalahgunaan lebih lanjut.")
                }
            },
            {
                "Malware",
                new[]
                {
                    new ThreatQuizQuestion(
                        "Apa yang dimaksud dengan malware?",
                        "Perangkat keras yang sudah terlalu lama digunakan.",
                        "Perangkat lunak berbahaya yang dirancang merusak, memata-matai, atau mengambil alih sistem.",
                        "Semua aplikasi yang membutuhkan koneksi internet.",
                        "Pesan promosi yang masuk ke kotak email.",
                        1,
                        "Malware adalah perangkat lunak berbahaya. Bentuknya mencakup virus, trojan, spyware, worm, dan jenis ancaman perangkat lunak lainnya."),
                    new ThreatQuizQuestion(
                        "Anda menerima lampiran program dari pengirim yang tidak dikenal. Apa yang sebaiknya dilakukan?",
                        "Jalankan agar dapat mengetahui isi program.",
                        "Matikan antivirus sebelum membuka lampiran.",
                        "Ubah nama file lalu jalankan kembali.",
                        "Jangan jalankan; verifikasi pengirim dan pindai file dengan perlindungan keamanan yang diperbarui.",
                        3,
                        "Lampiran yang tidak dipercaya dapat menjadi jalur masuk malware. Verifikasi sumber dan pemindaian harus dilakukan sebelum file digunakan."),
                    new ThreatQuizQuestion(
                        "Komputer tiba-tiba sangat lambat dan muncul proses asing. Langkah respons yang paling aman adalah...",
                        "Mengabaikannya selama komputer masih menyala.",
                        "Menyebarkan file yang dicurigai agar orang lain ikut memeriksa.",
                        "Putuskan koneksi jika perlu, jalankan pemeriksaan keamanan, dan laporkan kepada pihak yang bertanggung jawab.",
                        "Menghapus semua file sistem secara acak.",
                        2,
                        "Isolasi dapat membatasi komunikasi malware. Pemeriksaan dengan alat tepercaya dan pelaporan membantu menangani sumber masalah tanpa merusak sistem lebih lanjut.")
                }
            },
            {
                "Ransomware",
                new[]
                {
                    new ThreatQuizQuestion(
                        "Apa tujuan utama ransomware?",
                        "Mengunci atau mengenkripsi data lalu menuntut pembayaran dari korban.",
                        "Mempercepat proses pencadangan data.",
                        "Memperbarui sistem operasi secara otomatis.",
                        "Memblokir iklan pada browser.",
                        0,
                        "Ransomware menghilangkan akses korban terhadap sistem atau data dan menggunakan ancaman tersebut untuk meminta tebusan."),
                    new ThreatQuizQuestion(
                        "Strategi pencadangan mana yang paling membantu menghadapi ransomware?",
                        "Menyimpan satu salinan pada komputer yang selalu terhubung.",
                        "Menerapkan prinsip 3-2-1: beberapa salinan, media berbeda, dan satu salinan terpisah atau offline.",
                        "Mencadangkan hanya setelah terjadi serangan.",
                        "Mengganti nama folder cadangan agar tidak terlihat.",
                        1,
                        "Cadangan yang terpisah atau offline lebih sulit ikut dienkripsi. Prinsip 3-2-1 juga mengurangi risiko kegagalan pada satu lokasi atau media."),
                    new ThreatQuizQuestion(
                        "Apa tindakan awal yang tepat ketika perangkat diduga terkena ransomware?",
                        "Tetap menghubungkan perangkat ke jaringan agar proses tidak terganggu.",
                        "Langsung membayar tebusan tanpa melapor.",
                        "Menghapus bukti serangan dan semua catatan kejadian.",
                        "Isolasi perangkat, laporkan insiden, dan lakukan pemulihan melalui prosedur serta cadangan yang bersih.",
                        3,
                        "Isolasi membantu mencegah penyebaran. Pelaporan dan pemulihan dari cadangan bersih lebih aman daripada membayar, yang tidak menjamin data akan kembali.")
                }
            }
        };

    public static int ThreatCount => Questions.Count;

    public static bool TryGet(string threatId, out IReadOnlyList<ThreatQuizQuestion> questions)
    {
        if (string.IsNullOrWhiteSpace(threatId))
        {
            questions = null;
            return false;
        }

        return Questions.TryGetValue(threatId, out questions);
    }

    public static IReadOnlyList<ThreatQuizQuestion> Get(string threatId)
    {
        IReadOnlyList<ThreatQuizQuestion> questions;
        if (!TryGet(threatId, out questions))
            throw new KeyNotFoundException("No quiz questions for threat: " + threatId);

        return questions;
    }
}
