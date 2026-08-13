# Serene Cycle

Portföy odaklı, Android hedefli menstrüel döngü takip uygulaması. Faza göre beslenme/hareket rehberliği sunar ve YOLOv8 tabanlı yiyecek tarama içerir.

## Stack

- **Mobil:** Flutter (Riverpod, drift, go_router) — `mobile/`
- **API:** ASP.NET Core Web API (Identity + JWT, EF Core + PostgreSQL) — `backend/`
- **ML servisi:** Python + FastAPI + YOLOv8 — `ml/`
- **Altyapı:** Docker Compose — `infra/`

## Klasör yapısı

```
mobile/    Flutter uygulaması (Faz 0'da başlar)
backend/   ASP.NET Core Web API (Faz 3'te dolacak)
ml/        FastAPI + YOLOv8 mikroservisi (Faz 4'te dolacak)
infra/     docker-compose.yml, .env.example (Faz 3)
docs/      tasarım referansları, mimari notlar
```

## Geliştirme fazları

| Faz | İçerik | Durum |
|---|---|---|
| 0 | Ortam kurulumu, Flutter/Dart temelleri, go_router + alt menü iskeleti | 🚧 |
| 1 | Offline çekirdek: drift, faz hesaplama, onboarding, ana sayfa, günlük kayıt | ⏳ |
| 2 | Beslenme/hareket içerik katmanı | ⏳ |
| 3 | Backend + senkron (ASP.NET Core, Postgres, JWT auth) | ⏳ |
| 4 | YOLOv8 yiyecek tarama servisi | ⏳ |
| 5 | Cila + portföy dokümantasyonu | ⏳ |

Not: Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir.
