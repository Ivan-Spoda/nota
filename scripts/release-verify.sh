#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-only
#
# Checks the GitHub release the workflow drafted for vX.Y.Z: all six platform files are
# attached and the body matches, word for word, what scripts/release-notes.sh renders
# from release-notes/. Needs the gh CLI signed in to an account that can see drafts.
#
# Usage: scripts/release-verify.sh [version]   (default: the version in VERSION)

set -euo pipefail
cd "$(dirname "$0")/.."

version="${1:-$(tr -d '[:space:]' <VERSION)}"
tag="v${version}"
repo="nota-daw/nota"

json="$(gh release view "$tag" --repo "$repo" --json isDraft,body,assets,url)" \
  || { echo "error: no GitHub release for ${tag} yet — is the workflow still running?" >&2; exit 1; }

# GitHub may store the body with CRLF and trim trailing space; compare normalised text.
normalise() { tr -d '\r' | sed -e 's/[[:space:]]*$//' | sed -e :a -e '/^\n*$/{$d;N;ba' -e '}'; }
expected="$(scripts/release-notes.sh "$version" github | normalise)"
actual="$(jq -r .body <<<"$json" | normalise)"

status=0
if [ "$expected" = "$actual" ]; then
  echo "ok    body matches release-notes/${version}.{en,ru}.md"
else
  echo "FAIL  body differs from release-notes/${version}.{en,ru}.md (- expected, + published):"
  diff <(printf '%s\n' "$expected") <(printf '%s\n' "$actual") | sed 's/^/      /' || true
  status=1
fi

assets="$(jq -r '.assets[].name' <<<"$json")"
for pattern in "-arm64.dmg" "-x86_64.dmg" "-x64.exe" "-arm64.exe" "-x86_64.AppImage" "-aarch64.AppImage"; do
  if grep -q -- "${version}${pattern}\$" <<<"$assets"; then
    echo "ok    asset *${version}${pattern}"
  else
    echo "FAIL  asset *${version}${pattern} is missing"
    status=1
  fi
done

if [ "$(jq -r .isDraft <<<"$json")" = "true" ]; then
  echo "note  the release is still a draft: $(jq -r .url <<<"$json")"
fi
exit $status
