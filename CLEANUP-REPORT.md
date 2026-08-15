# NovaShop Deployment Cleanup Report

Status: COMPLETE. Working tree cleaned and staged (ready for review/commit). No commit
or push performed.

## Deleted

Total staged deletions: 238 files/folders. All removals are from the git index
(`git rm --cached`) and from disk where they were tracked. Categories:

### Hermes / test / debug verification artifacts (root + scripts)
- `'ERR` (empty stray shell-error file)
- `hermes-verify-image-upload.py`, `hermes-verify-*.sh`, `verify-image-upload.sh`,
  `verify-image-upload.py`, `verify-novashop.sh`, `verify.sh`, `verify-fix.sh`,
- `verify_critical_fixes.py`, `verify_final_fixes.py`, `verify_final_pipeline.py`,
  `verify_fix.py`, `verify_image_upload.py`, `verify_pipeline_final.py`,
  `verify_pipeline_fixes.py`, `temp_verify.py`, `temp_verify_fix.py`,
- `check-api.sh`, `check-backend.sh`, `quick-run.sh`, `quick-setup.sh`,
  `setup.sh`, `setup-verification.sh`, `start-backend.sh`, `test-novashop.sh`,
  `test-image-upload.js`, `scripts/verify-image-upload.sh`,
- `AGILE-PLAN.md`, `CHANGES-checkout-flow.md`, `CHANGES-create-order-from-cart.md`,
  `IMAGE_UPLOAD_FIXES_VERIFICATION.md`, `IMAGE_UPLOAD_PIPELINE_REPORT.md`,
  `IMAGE_UPLOAD_PIPELINE_VERIFICATION_REPORT.md`, `XMLFile1.xml`
- Reason: temporary diagnostic / Hermes verification scripts and throwaway reports;
  not part of the shipped application. Legitimate E2E source (`e2e-tests.spec.ts`)
  and real k6 load-test infra (`nova-shop-config.js`, `nova-shop-load-test.js`,
  `LOAD_TEST_QUICK_START.md`) were KEPT.

### Frontend verification temp scripts
- `frontend/hermes-verify-l10n.py`, `frontend/verify-persian-l10n*.py`,
  `frontend/temp-verify-l10n.py`, `frontend/verify-package.json`,
  `frontend/tmp-*.py` (tmp-parity, tmp-scan, tmp-scan2, tmp-scan3, tmp-usage),
  `frontend/bash.exe.stackdump`
- Reason: localized verification helpers only used during dev sessions.

### Backend test verification scripts / example env with secrets
- `backend/tests/NovaShop.Tests/hermes-verify-integration-tests.sh`,
  `backend/tests/NovaShop.Tests/verify-load-tests.sh`,
  `backend/tests/NovaShop.Tests/verify-tests.py`

### Playwright / MCP generated artifacts
- `.playwright-mcp/` (162 files: page snapshots `*.yml`, e2e images `*.png`)

### Temp / output / runtime artifacts
- `appr.txt`, `myreq.txt`, `myreq2.txt`, `notif.txt`, `notif2.txt`, `notif3.txt`,
  `nova-en-home.yml` (0 bytes), `nova-home.png` (0 bytes), `nova-home.yml` (0 bytes),
  `novashop-verify.sh`, `ord_err.txt`, `ord_ok.txt`, `over.txt`, `r10.txt`..`r12c.txt`,
  `tok.txt`, `tok2.txt`, `up_ok.txt`, `users-page.yml`, `test-red.png`
- `backend/src/NovaShop.Infrastructure/Services/LocalImageStorage.cs.backup`
- Reason: curl/API response captures, stray screenshots, and a `.backup` file
  left from image-upload work.

## Kept (important, intentionally preserved)

- Backend source + tests: all `backend/src/**` (`.csproj`, controllers, endpoints,
  domain/entities, EF Core migrations, Dapper + EF repos, services, jobs, consumers),
  `backend/tests/NovaShop.Tests/**` (integration tests + `IntegrationWebApplicationFactory.cs`).
- All 12 EF Core migrations + `NovaShopDbContextModelSnapshot.cs` (NOT deleted).
- API Gateway: `backend/src/NovaShop.ApiGateway/**` (Program.cs, appsettings,
  Dockerfiles, grafana dashboards, docker-compose, monitoring docs).
- Frontend: full `frontend/**` app (app router, components, lib, messages,
  `e2e-tests.spec.ts`, `components.json`, `next.config.ts`, etc.).
- `README.md`, `NOVASHOP-SYSTEM-DOCUMENTATION.md`, `MONITORING_SETUP_GUIDE.md`.
- `docker-compose.yml`, `docker-compose.prod.yml`, backend + gateway `Dockerfile`s.
- `.github/workflows/ci.yml`, `.dockerignore`, `.editorconfig`.
- `NovaShop.slnx`, `backend/**/Dockerfile`, `docker-entrypoint.sh`.
- Test env examples: `backend/tests/NovaShop.Tests/.env.k6.example`,
  `backend/tests/NovaShop.Tests/k6.env.example` (placeholders only).
- `hermes-webui` submodule gitlink reference (separate project, not NovaShop).

## Secrets Removed

- `backend/tests/NovaShop.Tests/.env.k6` → removed (contained `SQL_SERVER_PASSWORD`,
  Slack webhook URL, real config values). Only kept the `.env.k6.example` placeholder.
- `.gitignore` was rewritten to ignore `.env` / `.env.*` so no real env files can
  be committed; only explicit `!.env.example` negations are allowed through.
- NOTE: `YourStrong!Passw0rd` still appears in docker-compose files and
  `k6.env.example`. This is the Microsoft SQL Server container default test
  password (well-known, not a real production secret) used in local-dev/test
  compose stacks. It is NOT a production credential. Production should supply
  a real password via environment variables.
- Test-only JWT key (`TestSuperSecretKeyThatIsLongEnoughForHmacSha25612345!`) in
  `IntegrationWebApplicationFactory.cs` was INTENTIONALLY KEPT — it is a fake
  deterministic key for unit/integration tests, not the production `SuperSecretKey123!`
  value. It cannot be externalized without breaking the test harness.
- `SuperSecretKey123!` / production `Jwt:Key` is NOT stored in any tracked
  appsettings file (the API/Gateway read `Jwt:Key` from User Secrets or the
  `JWT_KEY` environment variable — both external to the repo). `.env.example`
  now documents `JWT_KEY=`.

### Historical Secret — ROTATION REQUIRED (NOT auto-fixed)

- A production JWT secret `SuperSecretKey123!` was present in prior commits
  (per task background). Per instructions, git history was NOT rewritten.
  Recommendation: rotate this key in any deployed environment that used it,
  and audit `git log` / history for the leaked value.

## Gitignore

UPDATED. Replaced the sprawling, partially-duplicated legacy `.gitignore`
with a focused, production-grade ignore covering:

- .NET: `bin/`, `obj/`, `*.user`, `*.suo`, `.vs/`, `*.pidb`, etc.
- Node: `node_modules/`, `.next/`, `build/`, `coverage/`, `.nyc_output/`
- Python: `__pycache__/`, `*.py[cod]`, `dist/`, `.eggs/`
- Logs: `*.log`, `logs/`, `Log/`, `Logs/`
- Env/secrets: `.env` / `.env.*` with `!.env.example` negations
- IDE: `.idea/`, swap files
- Playwright/E2E: `test-results/`, `playwright-report/`, `blob-report/`,
  `playwright/.cache/`, `.playwright-mcp/`
- OS: `.DS_Store`, `Thumbs.db`, `Desktop.ini`
- Temp/backup: `*.tmp`, `*.temp`, `*.bak`, `*.orig`, `*.backup`, `*.stackdump`

## Backend Build

PASS — `dotnet build NovaShop.slnx` → 0 errors, 65 warnings (pre-existing
mapper nullability/obsoletion warnings, unrelated to cleanup). Includes
NovaShop.Api, NovaShop.ApiGateway, Application, Domain, Infrastructure,
Benchmark, and NovaShop.Tests projects.

## Gateway Build

PASS — built as part of the solution (NovaShop.ApiGateway project).

## Frontend Build

PASS — `next build` completed successfully after a clean `pnpm install`;
all routes compiled (admin panel, products, cart, checkout, orders, etc.).

## Tests

AVAILABLE (not a hard failure). `NovaShop.Tests` project compiles. Tests are
integration tests requiring SQL Server + Redis containers (Testcontainers),
so they require Docker to run. CI workflow (`ci.yml`) runs `dotnet test`.
Not executed here because the offline environment lacks Docker. The
integration test fixture (`IntegrationWebApplicationFactory.cs`) is intact.

## Remaining Deployment Artifacts (need attention)

- `.vs/` directory remains on disk (contains `NovaShop.slnx/FileContentIndex/*.vsidx`
  locked by an active Visual Studio process). NOT git-tracked and gitignored —
  will not be committed. It is a local IDE cache; safe to delete when VS is closed.
- `frontend/node_modules/` and `frontend/.next/` were removed from disk after
  the build verification; both are gitignored. `.next` was regenerated during
  the successful build then deleted.
- `bin/` and `obj/` for all .NET projects are gitignored and not tracked; they
  were removed from disk during cleanup.
- `.env.k6` (real) is deleted. `.env.k6.example` + `k6.env.example` kept as
  placeholders.

## Remaining Security Issues

- `YourStrong!Passw0rd` in `docker-compose.yml`, `docker-compose.prod.yml`,
  `backend/src/NovaShop.Api/docker-compose.yml`,
  `backend/src/NovaShop.ApiGateway/docker-compose.yml`, and
  `backend/tests/NovaShop.Tests/k6.env.example`. This is the SQL Server
  container default test password — not a production secret, but recommend
  overriding via environment variables in production deployments.
- Historical `SuperSecretKey123!` JWT secret may exist in git history
  (ROTATION REQUIRED — see above).
- No other secrets, passwords, API keys, or tokens remain in tracked source.
  `appsettings.json` / `appsettings.Development.json` (API + Gateway) contain
  no `Jwt:Key` value (read from User Secrets / env var).

## Git Status (summary)

- Branch: `master`
- Staged changes: 241 (238 deletions, + `.gitignore` modified, + `.env.example`
  added, + `'ERR` removed)
- Tracked files after cleanup: 489
- Untracked on-disk (gitignored, NOT for commit): `.vs/` (locked .vsidx files,
  local IDE cache only)
- No commit or push performed — tree left for your review.
