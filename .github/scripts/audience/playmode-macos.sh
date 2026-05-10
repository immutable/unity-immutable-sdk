#!/bin/bash
# Runs the audience PlayMode tests on macOS. Captures Player.log into artifacts/.
# Surfaces Unity compile errors as ::error:: annotations.
# Workflow caller: .github/workflows/test-audience-sample-app.yml (playmode job).
#
# Inputs (env): UNITY_PATH (set by install-unity-macos.sh), TARGET.

set -uo pipefail

LOG=artifacts/unity.log
RESULTS="$(pwd)/artifacts/test-results.xml"

mkdir -p artifacts

# Tee Unity stdout to artifacts/unity.log so the annotation step has a file
# to scan; pipefail propagates Unity's exit code through tee.
"$UNITY_PATH" \
  -batchmode -nographics \
  -projectPath examples/audience \
  -runTests \
  -testPlatform "$TARGET" \
  -testResults "$RESULTS" \
  -logFile - 2>&1 | tee "$LOG"
test_rc=${PIPESTATUS[0]}

# Player runs as a separate process; copy its Player.log so HTTP traces and
# OnError fires are captured. Glob across companies and products.
src="$HOME/Library/Logs"
if [ -d "$src" ]; then
  find "$src" -name "Player.log" 2>/dev/null | while IFS= read -r f; do
    cp "$f" "artifacts/Player-$(basename "$(dirname "$f")").log" 2>/dev/null || true
  done
fi

# Promote Unity compile errors to ::error:: annotations. Sanitize '::' so log
# lines containing workflow commands cannot terminate the annotation early.
if [ -f "$LOG" ]; then
  grep -E '(error CS[0-9]+:|Compilation failed:)' "$LOG" | sort -u | while IFS= read -r line; do
    trimmed="${line#"${line%%[![:space:]]*}"}"
    sanitized="${trimmed//::/%3A%3A}"
    echo "::error::$sanitized"
  done || true
fi

exit "$test_rc"
