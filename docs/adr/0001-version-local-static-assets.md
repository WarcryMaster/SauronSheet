# ADR 0001 — Version local static assets

Use `asp-append-version="true"` on every local CSS, JS, and image asset referenced from Razor views.

## Status

Active

## Context

We observed a production-only visual mismatch in the header/navbar:

- logos appeared misaligned,
- text looked larger than in local,
- local and production rendered the same Razor layout differently.

The most likely cause was stale browser or intermediary cache serving older local assets in production, especially `site.css` and shared logo images.

Without asset versioning, ASP.NET serves the same URL even after the file changes, so clients may continue using an outdated cached copy.

## Decision

For every **local** static asset referenced from Razor:

- use `~/...` paths,
- add `asp-append-version="true"`.

Examples:

```cshtml
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
<script src="~/js/charts.js" asp-append-version="true"></script>
<img src="~/img/sauron-sheet-logo-32x32.png" asp-append-version="true" alt="SauronSheet logo" />
```

This applies to:

- CSS in `wwwroot/css/`
- JS in `wwwroot/js/`
- images in `wwwroot/img/`
- favicons and other local media referenced from Razor

This does **not** apply to external CDN assets such as MDBootstrap, Font Awesome, Chart.js, or Sentry.

## Consequences

### Positive

- Production and local are less likely to drift because updated assets get a new versioned URL.
- Visual fixes in shared layout files become visible immediately after deploy.
- Troubleshooting becomes clearer: if the issue remains after deploy and hard refresh, the next suspect is missing/blocked CDN CSS rather than stale local assets.

### Tradeoff

- Razor markup for local assets must consistently use tag helpers instead of hardcoded `/css/...`, `/js/...`, or `/img/...` paths.

## Verification path

After changing local assets used by shared UI:

1. Deploy the change.
2. Hard refresh production.
3. Confirm versioned asset URLs include `?v=` in the browser.
4. If the UI still differs, inspect CDN CSS/JS loading in DevTools Network.
