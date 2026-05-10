#!/bin/bash
# Installs the Unity editor and (for IL2CPP cells) the mac-il2cpp module.
# Idempotent. Sets UNITY_PATH in GITHUB_ENV so the playmode step picks it up.
# Workflow caller: .github/workflows/test-audience-sample-app.yml (playmode job).
#
# Inputs (env): UNITY_VERSION, UNITY_CHANGESET, BACKEND.

set -uo pipefail

HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"

echo "::group::install editor"
"$HUB" -- --headless install \
  --version "$UNITY_VERSION" --changeset "$UNITY_CHANGESET" --architecture arm64 \
  || echo "(install non-zero, OK if 'Editor already installed in this location')"
echo "::endgroup::"

if [ "$BACKEND" = "IL2CPP" ]; then
  echo "::group::install mac-il2cpp module"
  "$HUB" -- --headless install-modules \
    --version "$UNITY_VERSION" --changeset "$UNITY_CHANGESET" --architecture arm64 \
    --module mac-il2cpp \
    || echo "(install-modules non-zero, OK if 'No modules found to install')"
  echo "::endgroup::"
fi

EDITOR_APP=""
for cand in \
  "/Applications/Unity/Hub/Editor/$UNITY_VERSION-arm64/Unity.app" \
  "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"; do
  if [ -x "$cand/Contents/MacOS/Unity" ]; then EDITOR_APP="$cand"; break; fi
done

IL2CPP_DIR=""
if [ "$BACKEND" = "IL2CPP" ] && [ -n "$EDITOR_APP" ]; then
  for d in \
    "$EDITOR_APP/Contents/PlaybackEngines/MacStandaloneSupport/Variations/macos_arm64_player_nondevelopment_il2cpp" \
    "$EDITOR_APP/Contents/PlaybackEngines/MacStandaloneSupport/Variations/macos_x64_player_nondevelopment_il2cpp"; do
    if [ -d "$d" ]; then IL2CPP_DIR="$d"; break; fi
  done
fi

MISSING=""
[ -z "$EDITOR_APP" ] && MISSING="editor"
[ "$BACKEND" = "IL2CPP" ] && [ -z "$IL2CPP_DIR" ] && MISSING="${MISSING:+$MISSING+}mac-il2cpp"
if [ -n "$MISSING" ]; then
  echo "::error::Unity $UNITY_VERSION missing: $MISSING"
  ls -la /Applications/Unity/Hub/Editor/ 2>&1 || true
  "$HUB" -- --headless editors --installed 2>&1 || true
  exit 1
fi

UNITY_PATH="$EDITOR_APP/Contents/MacOS/Unity"
echo "Found Unity:  $UNITY_PATH"
[ -n "$IL2CPP_DIR" ] && echo "Found IL2CPP: $IL2CPP_DIR"
echo "UNITY_PATH=$UNITY_PATH" >> "$GITHUB_ENV"
