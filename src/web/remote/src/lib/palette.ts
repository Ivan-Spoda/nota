// SPDX-License-Identifier: AGPL-3.0-only
// Theme colours as CSS (spec 014). Every token of a theme becomes a custom property named
// after it — BgApp → --bg-app — plus the derived inks for the solid fills the phone draws.
// Pure: the page uses it at run time and the build (theme-plugin.ts) for the first-paint
// fallback, so both name and derive colours the same way.

export type Colors = Record<string, string>;
/** The dark and light ink text on a solid fill is chosen from, as Nota chooses them. */
export type Ink = readonly [string, string];

/** `BgApp` → `--bg-app`, `SystemAccentLight1` → `--system-accent-light1`. */
export const cssName = (token: string) => '--' + token.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

function rgb(hex: string): [number, number, number] | null {
  const m = /^#([0-9a-f]{6})([0-9a-f]{2})?$/i.exec(hex.trim());
  if (!m) return null;
  const n = parseInt(m[1], 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}

function luminance(c: [number, number, number]): number {
  const ch = (v: number) => { const x = v / 255; return x <= 0.03928 ? x / 12.92 : Math.pow((x + 0.055) / 1.055, 2.4); };
  return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2]);
}

const contrast = (a: number, b: number) => (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);

/** The ink that reads better on <fill> (WCAG contrast; a tie goes to the dark one) —
 * NotaPalette.ReadableInkOn. An unreadable fill gets the dark ink. */
export function inkOn(fill: string, ink: Ink): string {
  const f = rgb(fill), d = rgb(ink[0]), l = rgb(ink[1]);
  if (!f || !d || !l) return ink[0];
  const lf = luminance(f);
  return contrast(luminance(d), lf) >= contrast(luminance(l), lf) ? ink[0] : ink[1];
}

/** The solid fills that carry text, by the token they fill with. */
const FILLS: [string, string][] = [
  ['--ink-on-accent', 'Accent'], ['--ink-on-mute', 'Mute'], ['--ink-on-solo', 'Solo'],
  ['--ink-on-record', 'Record'], ['--ink-on-success', 'Success'],
];

/** Every token as a custom property, the ink pair (--ink-dark / --ink-light) and the inks on
 * the solid fills. */
export function cssVars(colors: Colors, ink: Ink): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(colors)) out[cssName(k)] = v;
  out['--ink-dark'] = ink[0];
  out['--ink-light'] = ink[1];
  for (const [name, token] of FILLS) if (colors[token]) out[name] = inkOn(colors[token], ink);
  return out;
}
