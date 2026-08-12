# Note: CTA "Get Started Free" text disappearing on hover — RESOLVED

**Status:** ✅ Resolved (commit `243ce3d`, pushed to `origin/master`)

## Issue

On the landing page (`index.html`), the "Get Started Free" CTA button displayed
correctly in its default state, but on hover the button remained visible while
the text disappeared.

## Root Cause

In `edtech-web/frontend/css/design-system.css`:

```css
a:hover {
  color: var(--accent-dark);
  text-decoration: underline;
}
```

The `a:hover` selector has specificity `0,1,1`, which beats `.btn-primary`'s
white text color (`0,1,0`). On hover the text color changed to `--accent-dark`
(dark red) — the same color as the button's hover background — making the text
invisible. It also added an unwanted underline to the button label.

## Fix

Changed the selector to exclude buttons:

```css
a:not(.btn):hover {
  color: var(--accent-dark);
  text-decoration: underline;
}
```

Normal links keep their hover styling; all `.btn` anchors now keep their own
label colors (covers every button variant: primary, secondary, ghost,
outline, etc.).

Also bumped the CSS cache-busting query `css/main.css?v=2` → `?v=3` across all
37 pages so the fixed stylesheet is fetched by returning visitors.

## Verification

Verified in headless Chrome (pixel-level check of the button label):

- Before: white text pixels 568 (default) → 0 (hover) — text invisible.
- After: 568 (default) → 532 (hover) — text stays visible; hover still applies
  background darkening, scale, and shadow as intended.
