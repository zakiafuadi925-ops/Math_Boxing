# Game Design Document (GDD): Math Boxing Online (Kids Edition)

**Project Name:** Math Boxing Online
**Target Platform:** WebGL (Mobile Browser Optimized - iOS & Android)
**Development Engine:** Unity 2022.3 LTS (C#)
**Backend & Database:** Supabase (Auth, Database, & Realtime Websockets)

---

## 1. High Concept & Target Market

- **Konsep Utama:** Game edukasi matematika kasual multiplayer _real-time_ bertema tinju. Dua pemain menggunakan HP masing-masing untuk adu cepat menyelesaikan soal matematika dasar yang sama.
- **Target Audiens:** Anak-anak usia Sekolah Dasar (SD).
- **Tujuan Edukasi:** Melatih refleks hitung cepat untuk Penjumlahan (+), Pengurangan (-), perkalian (\*), pembagian (/), akar pangkat 2, akar pangkat 3, kecepatan, jarak, waktu, panjang, luas permukaan benda, volume, menghitung jumlah buah, aljabar.

---

## 2. Core Gameplay & Spesifikasi Klien (4 Poin Utama)

### Poin 1: Animasi & Visual Utama (Tinju)

- Karakter utama di layar adalah dua petinju 2D (Player 1 di kiri, Player 2 di kanan).
- Visualisasi aksi berbasis respons instan: Ketika seorang pemain menjawab benar, petinjunya akan melakukan gerakan memukul (_Punch_) secara _real-time_ di kedua layar HP.

### Poin 2: Mode Bermain (Multiplayer 2 HP)

- Game dimainkan secara _online_ menggunakan dua perangkat HP terpisah via browser (WebGL).
- Kedua HP terhubung ke _Room ID_ yang sama di server dan menerima soal matematika yang sama secara serentak (_Shared Question Pool_).

### Poin 3: Sistem Identifikasi (Google Login)

- Autentifikasi pemain wajib menggunakan **Akun Google**.
- Proses login ditangani oleh _Supabase Auth (Google Provider)_ pada halaman web pembungkus sebelum game Unity WebGL dimuat untuk menjamin keamanan token.

### Poin 4: Penentu Akhir Game (Durasi 60 Detik)

- Pertarungan berjalan dalam mode _Sudden Death_ selama **60 detik (1 menit)**.
- Tujuan permainan adalah mengumpulkan skor sebanyak-banyaknya dalam batas waktu tersebut. Pemain dengan skor tertinggi saat waktu habis dinyatakan sebagai pemenang.

---

### Poin 5: Ruang Lingkup Materi & Generator Soal (Variasi Maksimal)

Sistem `MathGenerator.cs` akan menghasilkan soal secara dinamis dengan aturan hasil akhir **wajib bilangan bulat (Integer)**. Materi meliputi:

1.  **Counting:** Menghitung jumlah buah/objek yang muncul di layar (Visual-based).
2.  **Arithmetics:** $+$, $-$, $*$, $/$ dengan rentang angka dinamis (bisa ratusan).
3.  **Algebra:** Mencari nilai $x$ (Contoh: `2x - 4 = 10`).
4.  **Roots:** Akar Pangkat 2 ($\sqrt{64}$) dan Akar Pangkat 3 ($\sqrt[3]{27}$).
5.  **Physics Dasar:** Menghitung Jarak ($s$), Kecepatan ($v$), atau Waktu ($t$).
6.  **Geometry:** Menghitung Luas Permukaan & Volume Kubus/Balok/Tabung dengan angka yang sudah disederhanakan.

---

## 3. Game Loop & Sistem Penalti Anak-Anak Input Kalkulator (Numpad)

Karena materi sangat bervariasi dan tidak terbatas pada angka 1-20, sistem Pilihan Ganda dihapus secara mutlak dan digantikan dengan **Numpad Statis (0-9, -, CLR, ENTER)**.

[Kedua HP Sync di Room] -> Soal Muncul di Layar Atas: "Cari nilai x: 3x = 15"
|
v
[Tampilan Layar Bawah]: Numpad Statis [0-9], Tombol [-], [CLR], dan [ENTER]
|
+--------------------+--------------------+
| |
v v
[Player 1 Ketik Jawaban] [Player 2 Ketik Jawaban]
(Tekan: [5] -> [ENTER]) (Tekan: [3] -> [ENTER])
| |
+----+----+ +----+----+
| | | |
v v v v
[BENAR] [SALAH] [BENAR] [SALAH]

    Skor +10 - Input Reset                   - Skor +10 - Input Reset

    Pukul    - Numpad Lock 1s                - Pukul    - Numpad Lock 1s

    Soal Baru di-Sync                        - Soal Baru di-Sync

---

## 4. Spesifikasi Arsitektur Database & Realtime

### Kebijakan Replikasi Realtime (Websockets)

- Tabel yang direplikasi secara _realtime_ hanya `live_matches`.
- Klien Unity hanya mendengarkan (_listen_) perubahan pada baris (`row`) dengan `match_id` yang sesuai dengan kamar mereka untuk meminimalkan latensi jaringan.

### Aturan Validasi Input (Anti-Latency Rule)

1. Komparasi jawaban dilakukan di sisi klien murni (Local Verification) demi kecepatan respons animasi tinju.
2. Server Supabase bertindak sebagai pencatat skor resmi, penyedia token Google Auth, dan penyiar status `question_version` untuk pergantian soal secara serentak di kedua HP.

### Tabel `live_matches` (Sinkronisasi Realtime)

---

| Nama Kolom         | Tipe Data   | Deskripsi                                                                                                                                                                                |
| :----------------- | :---------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `match_id`         | `uuid` (PK) | ID unik untuk sesi pertandingan 2 HP                                                                                                                                                     |
| `p1_id`            | `uuid` (FK) | ID Player 1 (dari Supabase Auth)                                                                                                                                                         |
| `p2_id`            | `uuid` (FK) | ID Player 2 (dari Supabase Auth)                                                                                                                                                         |
| `current_question` | `varchar`   | Soal aktif saat ini, cth: "7 + 5"                                                                                                                                                        |
| `current_answer`   | `int4`      | Jawaban benar dari soal aktif. Digunakan oleh C# Unity untuk validasi lokal tombol `ENTER` kalkulator pemain.                                                                            |
| `question_version` | `int4`      | _Flag Trigger Animasi_. Angka ini naik `+1` setiap kali ada pemain yang menjawab benar agar HP lawan tahu kapan harus memicu animasi petinju dipukul dan mengganti soal secara serentak. |
| `p1_score`         | `int4`      | Skor akumulasi Player 1                                                                                                                                                                  |
| `p2_score`         | `int4`      | Skor akumulasi Player 2                                                                                                                                                                  |
| `time_remaining`   | `int4`      | Hitung mundur waktu pertandingan (Default: 60)                                                                                                                                           |
| `status`           | `varchar`   | Status game: `waiting`, `playing`, `finished`                                                                                                                                            |

---

---

## 5. Cetak Biru Kode C# Unity (Scripts)

1.  **`SupabaseAuthController.cs`**
    - Mengambil data profil (`display_name`, `avatar_url`) dari token Supabase Auth setelah user sukses login via Google.
2.  **`MatchmakingManager.cs`**
    - Mencari ketersediaan kamar di tabel `live_matches` dengan status `waiting` atau membuat kamar baru.
3.  **`MathGameManager.cs`**
    - Berjalan di sisi klien untuk memvalidasi input jawaban anak.
    - Mengirim perubahan skor ke Supabase jika jawaban benar.
4.  **`SupabaseRealtimeListener.cs`**
    - Menggunakan _Websockets_ bawaan Supabase untuk mendengarkan perubahan data skor dan soal pada tabel `live_matches`.
    - Memicu fungsi `Punch()` pada komponen petinju yang skornya bertambah.
5.  **`GameTimer.cs`**
    - Menjalankan hitung mundur 60 detik lokal yang secara periodik divalidasi oleh server waktu pertandingan.
