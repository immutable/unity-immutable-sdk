#!/usr/bin/env bash
#
# Guards the published shape of the com.immutable.audience package.
#
# When the package is installed from a git URL, Unity copies the folder into an
# immutable PackageCache verbatim. Every asset there must have a .meta companion
# or the asset is dropped, which turns into a fatal error on device builds.
# Non-Unity tooling files (csproj, props, sln) have no place in the package and
# trip the same failure because their .meta files are intentionally gitignored.
#
# This check fails the build if either problem is reintroduced.

set -euo pipefail

PKG="src/Packages/Audience"
status=0

# Files that should never ship inside the package.
forbidden=$(git ls-files "$PKG" \
  | grep -E '\.(csproj|sln)$|/Directory\.Build\.(props|targets)$' || true)
if [ -n "$forbidden" ]; then
  echo "ERROR: non-Unity tooling files tracked inside $PKG (move them out of the package):"
  while IFS= read -r f; do
    printf '  %s\n' "$f"
  done <<< "$forbidden"
  status=1
fi

# Every tracked asset must have a tracked .meta companion. Dotfiles and dot
# folders are ignored by Unity, so they are exempt.
missing=""
while IFS= read -r f; do
  case "$f" in
    *.meta) continue ;;
    */.*) continue ;;
    .*) continue ;;
  esac
  if ! git ls-files --error-unmatch "$f.meta" >/dev/null 2>&1; then
    missing="$missing  $f"$'\n'
  fi
done < <(git ls-files "$PKG")

if [ -n "$missing" ]; then
  echo "ERROR: assets in $PKG are missing a committed .meta companion:"
  printf '%s' "$missing"
  status=1
fi

# Every tracked .meta must have a matching asset. The asset is either a tracked
# file or, for Unity folder metas, a directory that holds tracked files. An
# orphan meta left behind by `git rm`-ing an asset keeps a stale GUID around.
orphan=""
while IFS= read -r m; do
  base="${m%.meta}"
  if git ls-files --error-unmatch "$base" >/dev/null 2>&1; then
    continue
  fi
  if [ -n "$(git ls-files "$base/")" ]; then
    continue
  fi
  orphan="$orphan  $m"$'\n'
done < <(git ls-files "$PKG" | grep -E '\.meta$' || true)

if [ -n "$orphan" ]; then
  echo "ERROR: orphan .meta files in $PKG (no matching asset, stale GUID):"
  printf '%s' "$orphan"
  status=1
fi

if [ "$status" -eq 0 ]; then
  echo "OK: $PKG is meta-complete and free of non-Unity tooling files."
fi
exit "$status"
