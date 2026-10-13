#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-only
#
# Bumps the app (VERSION) and engine (src/native/nota.engine/VERSION) versions together,
# by the project's rule: a feature release bumps both minors, a fix release bumps both
# patches, and a component that resets starts at 1, never 0 (0.51.2 -> 0.52.1).
#
# Usage: scripts/release-bump.sh <minor|patch> [--dry-run]
# Prints "app OLD -> NEW" and "engine OLD -> NEW"; --dry-run writes nothing.

set -euo pipefail
cd "$(dirname "$0")/.."

kind="${1:?usage: $0 <minor|patch> [--dry-run]}"
dry="${2:-}"

bump() {
  local major minor patch
  IFS=. read -r major minor patch <<<"$1"
  case "$kind" in
    minor) echo "${major}.$((minor + 1)).1" ;;
    patch) echo "${major}.${minor}.$((patch + 1))" ;;
    *) echo "error: unknown bump '$kind' (minor | patch)" >&2; exit 1 ;;
  esac
}

app_file=VERSION
engine_file=src/native/nota.engine/VERSION
app_old="$(tr -d '[:space:]' <"$app_file")"
engine_old="$(tr -d '[:space:]' <"$engine_file")"
app_new="$(bump "$app_old")"
engine_new="$(bump "$engine_old")"

if git rev-parse -q --verify "refs/tags/v${app_new}" >/dev/null; then
  echo "error: tag v${app_new} already exists" >&2
  exit 1
fi

echo "app    ${app_old} -> ${app_new}"
echo "engine ${engine_old} -> ${engine_new}"

if [ "$dry" != "--dry-run" ]; then
  printf '%s' "$app_new" >"$app_file"
  printf '%s' "$engine_new" >"$engine_file"
fi
