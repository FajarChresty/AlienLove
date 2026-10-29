# Alien Love: Matchmaking System

Implementasi sistem matchmaking berdasarkan **Main GDD** dan **Sub-GDD: Matchmaking System & Interview Logic**.
Unity 2021.2 atau lebih baru (C# 9). Semua logika inti adalah C# murni, tanpa UI.

## Cara memasang
1. Salin folder `Assets/AlienLove` ke folder `Assets` project Unity Anda.
2. Tunggu Unity selesai compile. Seharusnya tidak ada error.
3. (Opsional) Buat config: klik kanan di Project > **Create > AlienLove > Matchmaking Config**.
4. Coba demo: buat GameObject kosong di scene, tambahkan **DummyMatchmakingDemo**, tekan Play, lihat Console.
5. Jalankan test: **Window > General > Test Runner > EditMode > Run All**.

## Struktur
```
Assets/AlienLove/
  Scripts/
    Core/                      <- SISTEM UTAMA (asmdef: AlienLove.Core)
      Data/                    enum, 15 trait (TraitCatalog), CharacterRecord
      Config/                  MatchmakingConfig (SEMUA angka balancing) + ScriptableObject
      Compatibility/           CompatibilityEvaluator (rumus bab 11)
      Characters/              validator, repository, CharacterAsset (ScriptableObject)
      Candidates/              CandidateDatabase (registrasi, return rule, ketersediaan)
      Interview/               InterviewContent + InterviewManager
      Session/                 MatchmakingController, state machine, MatchOutcome
      Randomness/              IRandomSource + SeededRandomSource
    Dummy/                     <- DATA DUMMY (asmdef: AlienLove.Dummy), AMAN DIHAPUS
  Tests/EditMode/              44 unit test (hanya bergantung pada Core)
```

## Cara menghapus data dummy
Hapus folder **`Assets/AlienLove/Scripts/Dummy`**. Selesai.
Core dan Tests tidak mereferensikan Dummy, jadi tidak ada error setelah dihapus
(sudah diverifikasi dengan kompilasi tanpa folder itu).

## Rumus (Sub-GDD bab 11)
```
Skor akhir = Trait Preference x 0.60 + Green/Red Flag x 0.40
```
- **Trait:** tiap trait = rata-rata dua arah. Persen cocok = `tabel[|sifat - preferensi|]`, default `100 / 75 / 50 / 25`.
- **Flag:** mulai 100%, kurangi `100% / jumlah RedFlag` per Red Flag terpenuhi, tambah setengah nilai Green Flag
  terpenuhi (`100% / total flag`), batasi 0-100, dua arah dirata-rata. Karakter tanpa flag = 100%.
- **Kategori:** `>=90` Perfect (x3), `>=50` Normal Success (x1), `>=20` Normal Failure (x0), `<20` Very Bad (x-2).
- Tanpa rekomendasi = Normal Failure, tanpa bayaran, tanpa skor.

## Alur memakai controller
```csharp
var cfg   = MatchmakingConfig.CreateDefault();
var repo  = new CharacterRepository();
var db    = new CandidateDatabase(repo, cfg);
var eval  = new CompatibilityEvaluator(cfg);
var ctrl  = new MatchmakingController(repo, db, eval, cfg, new SeededRandomSource(seed));

ctrl.OutcomeResolved += outcome => { /* kirim ke sistem ekonomi / weekly evaluation */ };

ctrl.StartSession("client_id");
var candidates = ctrl.GetAvailableCandidates();
ctrl.SelectCandidate("cand_id");
ctrl.MarkReadyToRecommend();
ctrl.Recommend("cand_id");        // atau ctrl.DeclineMatch(clientAcceptsOptIn)
```
UI hanya boleh memanggil method controller. UI tidak boleh mengubah ketersediaan karakter atau saldo.

## Asumsi sementara (bisa diubah, belum ada di GDD)
| Hal | Asumsi saat ini |
|---|---|
| Flag dicek ke apa | Ciri **asli** lawan (`CharacterRecord.tags`), bukan CV tampil |
| Karakter tanpa flag | Skor sisi itu 100% (jawaban atasan) |
| Severe failure / lawsuit | **Belum ada**. `NoSevereFailureRule` selalu false. Ganti dengan implementasi `ISevereFailureRule` |
| Deal-breaker, importance, flexibility | Datanya disimpan, **belum dipakai** di rumus |
| Gagal biasa, kandidat tidak kembali | Status `Removed` |
| Save/load sesi | Belum ada |
| Uang / saldo | Tidak diubah di sini; hanya `paymentMultiplier` yang dikirim lewat event |

## Perlu diketahui: balancing
Dengan data acak, hampir semua pasangan masuk Normal Success. Lihat `DummyScoreSimulation`
(otomatis berjalan di `DummyMatchmakingDemo`). Semua angka ada di `MatchmakingConfig`, jadi
mudah disetel. Data karakter asli yang dirancang penulis konten akan berbeda dari data acak,
jadi simulasi ulang setelah data asli ada.

## Push ke GitHub
Lakukan dari **project Unity**, bukan dari folder ini, supaya file `.meta` ikut ter-commit
(Unity membuatnya saat pertama kali membuka folder ini).
```bash
cd <folder-project-unity>
git init                       # lewati jika sudah repo
# salin .gitignore dari zip ke root project jika belum ada
git add .
git commit -m "Add matchmaking system with dummy data and tests"
git branch -M main
git remote add origin https://github.com/<user>/<repo>.git
git push -u origin main
```
