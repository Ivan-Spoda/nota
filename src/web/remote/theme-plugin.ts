// SPDX-License-Identifier: AGPL-3.0-only
// The phone's first-paint colours, built from the theme files (spec 014): what the page
// wears before Nota has told it a theme and before it has one stored — Studio Dark, Nota's
// default, with Studio Light under [data-theme='light'] for an older Nota that only says
// "light". The phone sources hold no colour value; `import 'virtual:nota-theme.css'` gets
// these. The derived inks come from Studio Dark's TextOnAccent and ShadeUp, the two inks
// NotaPalette.ReadableInkOn picks between; Nota sends the real pair once connected.

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import type { Plugin } from 'vite';
import { cssVars, type Colors, type Ink } from './src/lib/palette.ts';

const ID = 'virtual:nota-theme.css';
const themeFile = (id: string) => fileURLToPath(new URL(`../../managed/Nota.App/Theme/Themes/${id}.json`, import.meta.url));

// Avalonia writes a translucent colour #AARRGGBB; CSS reads #RRGGBBAA.
function colorsOf(id: string): Colors {
  const raw: Colors = JSON.parse(readFileSync(themeFile(id), 'utf8')).colors;
  return Object.fromEntries(Object.entries(raw).map(([k, v]) => [k, v.length === 9 ? '#' + v.slice(3) + v.slice(1, 3) : v]));
}

const block = (sel: string, vars: Record<string, string>) =>
  `${sel} {\n${Object.entries(vars).map(([k, v]) => `  ${k}: ${v};`).join('\n')}\n}\n`;

export function notaTheme(): Plugin {
  return {
    name: 'nota-theme',
    resolveId: (id) => (id === ID ? '\0' + ID : undefined),
    // The browser-chrome tint before any theme is stored: the fallback's ground.
    transformIndexHtml: (html) => html.replace('%NOTA_BG_APP%', colorsOf('studio-dark').BgApp),
    load(id) {
      if (id !== '\0' + ID) return;
      this.addWatchFile(themeFile('studio-dark'));
      this.addWatchFile(themeFile('studio-light'));
      const dark = colorsOf('studio-dark'), light = colorsOf('studio-light');
      const ink: Ink = [dark.TextOnAccent, dark.ShadeUp];
      return `/* Generated from Theme/Themes/studio-dark.json and studio-light.json by theme-plugin.ts. */\n`
        + block(":root, [data-theme='dark']", cssVars(dark, ink)) + "[data-theme='dark'] { color-scheme: dark; }\n"
        + block("[data-theme='light']", cssVars(light, ink)) + "[data-theme='light'] { color-scheme: light; }\n";
    },
  };
}
