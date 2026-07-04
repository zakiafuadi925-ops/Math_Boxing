# Game Design Document (GDD): Math Boxing Casual (Kids Version)

**Target Platform:** WebGL (Mobile Browser Friendly - iOS & Android)
**Language/Engine:** C# / Unity 2022.3 LTS
**Database/Backend:** Supabase via REST API (UnityWebRequest)

---

## 1. High Concept & Target Market

- **Konsep:** Game edukasi matematika kasual bertema tinju/tarik tambang yang disederhanakan untuk anak-anak (usia SD). Game mengutamakan pengenalan pola (_pattern recognition_) dan refleks cepat tanpa penalti yang membuat stres.
- **Tujuan Edukasi:** Melatih kecepatan berhitung dasar (Penjumlahan & Pengurangan angka 1-20).

---

## 2. Core Gameplay Loop (Alur Utama)

1. **Fase Muncul Soal:** Sistem menampilkan soal di atas layar (Contoh: `5 + 3 = ...`).
2. **Fase Tampilan Opsi:** Layar memunculkan 3 tombol besar berisi angka pilihan jawaban (Contoh: `[ 7 ]`, `[ 8 ]`, `[ 9 ]`).
3. **Fase Eksekusi (Input Anak):**
   - **JIKA BENAR (Klik [ 8 ]):** Karakter memukul (Visual Tinju) atau menarik tali (Visual Tarik Tambang). Skor bertambah +10. Soal berganti.
   - **JIKA SALAH:** Muncul efek visual "Oops!" (Karakter menggeleng). Tidak ada pengurangan darah/skor. Tombol yang salah terkunci selama 1 detik (Cooldown), memberi anak kesempatan memilih jawaban lain.
4. **Fase Ronde Selesai:** Game berakhir setelah batas waktu habis (misal 60 detik) atau setelah berhasil menjawab 10 soal dengan benar. Skor akhir dikirim ke database.

---

## 3. Arsitektur Teknis & Database (Supabase Integration)

Karena game dideploy sebagai WebGL, semua komunikasi ke database dilakukan secara asinkronus menggunakan protokol HTTP melalui kelas `UnityWebRequest` di C#.

### Struktur Tabel Database (`scores`) di Supabase:

- `id` (int8, Primary Key, Auto-increment)
- `created_at` (timestamp)
- `player_name` (varchar)
- `score` (int4)

### Skema Komunikasi C# (Unity) ke Supabase:

[Unity WebGL Client] --(HTTP POST + JSON + API Key)--> [Supabase REST Endpoint]
|
v
[Player Wins/Time Up] <--------------------------------- [Google Sheets/Dashboard]
(Optional Integration)

---

## 4. Rencana Roadmap Pengembangan (Milestones)

### Fase 1: Setup Proyek & Koneksi Database (Minggu Ini)

- [ ] Install Unity 2022.3 LTS (Modul WebGL Build Support).
- [ ] Buat skrip `SupabaseManager.cs` untuk menguji pengiriman data skor statis dari Unity ke tabel Supabase.
- [ ] Setup UI Canvas dasar untuk resolusi layar HP _Landscape_.

### Fase 2: Logika Matematika & Generator Soal

- [ ] Buat skrip `MathGenerator.cs` untuk menghasilkan soal acak penjumlahan/pengurangan dengan rentang hasil akhir 1-20.
- [ ] Buat logika pengacakan posisi 3 tombol jawaban agar posisi jawaban benar tidak selalu sama.

### Fase 3: Visual & Game Feel (Ramai & Interaktif)

- [ ] Masukkan aset karakter 2D (Petinju/Anak-anak).
- [ ] Hubungkan logika benar/salah dengan sistem `Animator` Unity untuk memicu animasi memukul/menghindar.
- [ ] Tambahkan efek suara (_SFX_) yang memuaskan saat jawaban benar dan musik latar (_BGM_) yang ceria.

### Fase 4: Deployment & Optimasi WebGL

- [ ] Optimasi ukuran _build_ WebGL agar ringan saat dimuat di browser HP (Gunakan kompresi Gzip/Brotli).
- [ ] Build proyek dan unggah ke platform hosting pilihan untuk demo ke klien.
