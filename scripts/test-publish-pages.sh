#!/usr/bin/env bash

set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
temporary_directory="$(mktemp -d)"
trap 'rm -rf "$temporary_directory"' EXIT

report_path="${temporary_directory}/merged-report.html"
event_model_path="${temporary_directory}/event-model.html"
pages_directory="${temporary_directory}/pages"
printf '<html><body>TUnit fixture</body></html>' > "$report_path"
printf '<html><body>Event model fixture</body></html>' > "$event_model_path"

run_publisher() {
  local pages_override="${4:-$pages_directory}"

  DRY_RUN=true \
    TEST_REPORT_PATH="$report_path" \
    EVENT_MODEL_PATH="$event_model_path" \
    PAGES_WORK_DIR="$pages_override" \
    TEST_RESULT="$1" \
    RUN_ID="$2" \
    RUN_NUMBER="$3" \
    SHA="1234567890abcdef" \
    SERVER_URL="https://github.example" \
    REPOSITORY="tooyioo/api" \
    bash "${script_directory}/publish-pages.sh"
}

run_publisher success 100 1
run_publisher failure 101 2

test -f "${pages_directory}/runs/100/index.html"
test -f "${pages_directory}/runs/101/index.html"
test -f "${pages_directory}/latest/index.html"
test -f "${pages_directory}/tests/index.html"
test -f "${pages_directory}/tests/history.json"
test -f "${pages_directory}/event-model/index.html"
test -f "${pages_directory}/index.html"
test "$(jq 'length' "${pages_directory}/history.json")" = "2"
test "$(jq 'length' "${pages_directory}/tests/history.json")" = "2"
test "$(jq -r '.[0].status' "${pages_directory}/history.json")" = "PASSED"
test "$(jq -r '.[1].status' "${pages_directory}/history.json")" = "FAILED"
grep -q '../runs/101/' "${pages_directory}/tests/index.html"
grep -q 'class="failed">FAILED' "${pages_directory}/tests/index.html"
grep -q 'href="../latest/"' "${pages_directory}/tests/index.html"
grep -q 'href="tests/"' "${pages_directory}/index.html"
grep -q 'href="latest/"' "${pages_directory}/index.html"
grep -q 'href="event-model/"' "${pages_directory}/index.html"
grep -q 'class="failed">FAILED' "${pages_directory}/index.html"
grep -q 'Event model fixture' "${pages_directory}/event-model/index.html"

jq -n '[range(1; 101) | { run: tostring, number: tostring, date: "fixture", status: "PASSED", sha: "fixture", url: "https://github.example" }]' > "${pages_directory}/history.json"
run_publisher success 101 101

test "$(jq 'length' "${pages_directory}/history.json")" = "100"
test "$(jq 'length' "${pages_directory}/tests/history.json")" = "100"
test "$(jq -r '.[0].run' "${pages_directory}/history.json")" = "2"
test "$(jq -r '.[99].run' "${pages_directory}/history.json")" = "101"

legacy_pages_directory="${temporary_directory}/legacy-pages"
mkdir -p "${legacy_pages_directory}/runs/99"
printf '<html><body>Legacy TUnit fixture</body></html>' > "${legacy_pages_directory}/runs/99/index.html"
jq -n '[{ run: "99", number: "99", date: "fixture", status: "PASSED", sha: "fixture", url: "https://github.example" }]' > "${legacy_pages_directory}/history.json"
run_publisher success 200 3 "$legacy_pages_directory"

test -f "${legacy_pages_directory}/runs/99/index.html"
test -f "${legacy_pages_directory}/tests/index.html"
grep -q '../runs/99/' "${legacy_pages_directory}/tests/index.html"
test "$(jq 'length' "${legacy_pages_directory}/history.json")" = "2"
test "$(jq 'length' "${legacy_pages_directory}/tests/history.json")" = "2"

echo "publish-pages dry-run checks passed"
