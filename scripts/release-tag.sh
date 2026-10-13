#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-only
#
# Creates the local vX.Y.Z release tag on main once the release PR (develop -> main) has
# merged. It runs the same checks as the release workflow's guard first, so a tag that
# CI would reject is never made. It does not push — pushing the tag starts the release
# build, and that stays a human step.
#
# Usage: scripts/release-tag.sh

set -euo pipefail
cd "$(dirname "$0")/.."

fail() { echo "error: $*" >&2; exit 1; }

[ "$(git rev-parse --abbrev-ref HEAD)" = "main" ] || fail "not on main — run: git switch main && git pull"
[ -z "$(git status --porcelain)" ] || fail "working tree is not clean"

git fetch --quiet origin main
[ "$(git rev-parse HEAD)" = "$(git rev-parse origin/main)" ] \
  || fail "local main differs from origin/main — run: git pull --ff-only"

version="$(tr -d '[:space:]' <VERSION)"
tag="v${version}"

git rev-parse -q --verify "refs/tags/${tag}" >/dev/null && fail "tag ${tag} already exists locally"
git ls-remote --exit-code --tags origin "refs/tags/${tag}" >/dev/null 2>&1 \
  && fail "tag ${tag} already exists on origin"
grep -qE "^## \[${version}\]" CHANGELOG.md || fail "CHANGELOG.md has no '## [${version}]' section"
scripts/release-notes.sh "$version" github >/dev/null  # fails if either notes file is missing

git tag -a "$tag" -m "Nota ${tag}"
echo "Created ${tag} on $(git rev-parse --short HEAD). Push it to start the release build:"
echo "  git push origin ${tag}"
