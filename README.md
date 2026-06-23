# AFTERIMAGE

**Pembuatan Game 3D Top-Down Fast-Paced Action "AFTERIMAGE" Pada Platform PC**

---

## Informasi Umum

| | |
|---|---|
| **Judul Skripsi** | Pembuatan Game 3D Top-Down Fast-Paced Action "AFTERIMAGE" Pada Platform PC |
| **Penulis** | Ivander (535220020) |
| **Universitas** | Universitas Tarumanagara |
| **Fakultas** | Fakultas Teknologi Informasi |
| **Program Studi** | Teknik Informatika |
| **Dosen Pembimbing** | Bapak Darius Andana Haris, S.Kom., M.TI. (Pembimbing I) |
| | Ibu Ir. Jeanny Pragantha, M.Eng. (Pembimbing II) |

---

## Deskripsi

AFTERIMAGE adalah game 3D top-down fast-paced action yang dibangun menggunakan Unity 6 (URP) dengan bahasa pemrograman C#. Pemain mengendalikan seorang samurai cyber yang harus membersihkan musuh dari berbagai ruangan tertutup menggunakan mekanik dash attack, dodge, dan Iaijutsu Break. Game ini menggabungkan elemen action combat dengan sistem loadout, scoring, dan narratif yang berlatar belakang distopia futuristik Jepang.

### Mekanik Utama

- **Dash Attack** — Serangan utama berbasis dash ke arah kursor musuh. Jika meleset, waktu melambat sebagai penalti.
- **Dodge Roll** — Menghindari serangan tanpa damage output. Cooldown 1.5 detik.
- **Iaijutsu Break** — Ultimate satu kali per level: membekukan semua musuh lalu membunuh semuanya secara berurutan.
- **Sistem Loadout** — Pemilihan utility item antar level dengan slot cost system.
- **Sistem Skor** — Skor berbasis chain kill, multi-kill, waktu, dan penggunaan utility.

### Musuh

Terdapat 9 tipe musuh dengan perilaku unik, mulai dari melee grunt, rusher berbasis dash, spawner (Prism), support (Weaver), hingga boss final (Echo) yang merupakan klon pemain.

### Arsitektur Ruangan

Sistem ruangan tertutup dengan pintu yang bisa dihancurkan via dash. Setiap ruangan terkunci saat musuh aktif dan terbuka setelah semua musuh dikalahkan. Ruangan final terbuka hanya setelah semua ruangan lain selesai.

---

## Lingkungan Pengembangan

| Komponen | Versi |
|----------|-------|
| **Engine** | Unity 6 (6000.3.7f1) |
| **Render Pipeline** | Universal Render Pipeline (URP) 17.3.0 |
| **Bahasa Pemrograman** | C# (.NET Standard 2.1, LangVersion 9.0) |
| **Input System** | Unity Input System 1.18.0 |
| **Target Platform** | StandaloneWindows64 (PC) |
| **Camera** | Cinemachine 3.1.5 |

---

## Aset Eksternal

| Aset | Fungsi |
|------|--------|
| Polygon Cyber City (Synty) | Model lingkungan |
| Polygon Sci-Fi Space (Synty) | Model lingkungan |
| Polygon Sci-Fi Worlds (Synty) | Model lingkungan |
| Polygon Samurai (Synty) | Model karakter |
| Gabriel Aguiar Productions | Efek visual (proyektil, laser, kemampuan) |
| HIVEMIND Realistic Blood VFX | Efek darah dan decal |
| TextMesh Pro | Rendering teks UI |

---

## Struktur Proyek

```
Assets/
├── Scripts/
│   ├── Audio/          — AudioService, AmbientAudioController, FootstepAudio
│   ├── Enemies/        — EnemyBase + 8 tipe musuh
│   ├── Loadout/        — LoadoutManager, LoadoutData, UtilityDefinition
│   ├── Projectiles/    — BaseProjectile + 5 tipe proyektil
│   ├── Rooms/          — Room, RoomManager, Door, DoorDashZone
│   ├── UI/             — MainMenu, PreGamePanel, DeathPanel, PausePanel, dll.
│   ├── Utilities/      — BaseUtility + 4 tipe utility
│   └── (root)          — PlayerMovement, PlayerDash, PlayerHealth, ScoreManager, dll.
├── Scenes/             — MainMenu, LoadoutScene, Level0–Level6
├── Animations/         — Controller animasi karakter
├── Audios/             — SFX dan musik
└── Shaders/            — Shader kustom (Outline, SpriteAlwaysOnTop)
```

---

## Alur Level

```
MainMenu → LoadoutScene → Level0 → Level1 → Level2 → Level3 → Level4 → Level5 → Level6
```

Progress disimpan melalui `PlayerPrefs` dan dikelola oleh `GameProgressManager`.

---

## Instalasi dan Menjalankan Game

### Prasyarat

- [Unity 6](https://unity.com/releases/editor/archive) (6000.3.x atau lebih baru)
- Git

### Menjalankan dari Editor

1. Clone repository ini:
   ```bash
   git clone https://github.com/username/AFTERIMAGE.git
   ```
2. Buka project di Unity Hub.
3. Buka scene `MainMenu` dari folder `Assets/Scenes/`.
4. Tekan **Play**.

### Build Eksekusi

Build game yang sudah siap pakai dan Manual Book tersedia di Google Drive:

[**Google Drive — Build & Manual Book**](https://drive.google.com/drive/folders/1AOyXiYkZ8LjAbBZ5sWc2n_SPw6rTLiBb?usp=sharing)

---

## Fitur Debug

Akses **Debug HUD** (tekan tombol yang ditentukan) untuk mengaktifkan:

- **God Mode** — Tidak bisa mati.
- **Skip to Boss** — Langsung ke ruangan boss.
- **Skip Level** — Melompati level saat ini.

---

## Lisensi

Proyek skripsi ini dikembangkan untuk keperluan akademis di Universitas Tarumanagara. Aset pihak ketiga (Synty, Gabriel Aguiar, HIVEMIND) tunduk pada lisensi masing-masing dari Unity Asset Store.

---

*Ivander — Universitas Tarumanagara, Fakultas Teknologi Informasi, 2026*
