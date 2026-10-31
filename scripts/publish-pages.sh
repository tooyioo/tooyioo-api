#!/usr/bin/env bash

set -euo pipefail

require_environment_variable() {
  local variable_name="$1"
  if [[ -z "${!variable_name:-}" ]]; then
    echo "ERROR: ${variable_name} must be set."
    exit 1
  fi
}

for variable_name in TEST_RESULT RUN_ID RUN_NUMBER SHA SERVER_URL REPOSITORY; do
  require_environment_variable "$variable_name"
done

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
test_template_path="${script_directory}/templates/test-report-index.html.template"
landing_template_path="${script_directory}/templates/pages-index.html.template"
report_path="${TEST_REPORT_PATH:-}"
event_model_path="${EVENT_MODEL_PATH:-}"

if [[ -z "$report_path" ]]; then
  require_environment_variable RUNNER_TEMP
  report_path="$(find "$RUNNER_TEMP" -type f -name "merged-report.html" -print -quit)"
fi

if [[ -z "$report_path" || ! -f "$report_path" ]]; then
  echo "ERROR: TUnit merged-report.html was not found."
  exit 1
fi

if [[ -z "$event_model_path" || ! -f "$event_model_path" ]]; then
  echo "ERROR: EVENT_MODEL_PATH must point to the generated event model HTML."
  exit 1
fi

for template_path in "$test_template_path" "$landing_template_path"; do
  if [[ ! -f "$template_path" ]]; then
    echo "ERROR: Pages template was not found at ${template_path}."
    exit 1
  fi
done

echo "TUnit report: ${report_path}"
echo "Event model:  ${event_model_path}"

pages_work_directory="${PAGES_WORK_DIR:-${RUNNER_TEMP:-}/pages}"
if [[ -z "$pages_work_directory" || "$pages_work_directory" == "/pages" ]]; then
  echo "ERROR: PAGES_WORK_DIR or RUNNER_TEMP must be set."
  exit 1
fi

if [[ "${DRY_RUN:-false}" == "true" ]]; then
  mkdir -p "$pages_work_directory"
else
  require_environment_variable GITHUB_TOKEN
  mkdir -p "$pages_work_directory"
  cd "$pages_work_directory"
  git init
  pages_remote_url="https://x-access-token:${GITHUB_TOKEN}@github.com/${REPOSITORY}.git"

  if git remote get-url origin >/dev/null 2>&1; then
    git remote set-url origin "$pages_remote_url"
  else
    git remote add origin "$pages_remote_url"
  fi

  git fetch origin gh-pages
  git checkout -B gh-pages origin/gh-pages
fi

mkdir -p \
  "${pages_work_directory}/runs/${RUN_ID}" \
  "${pages_work_directory}/latest" \
  "${pages_work_directory}/tests" \
  "${pages_work_directory}/event-model"

cp "$report_path" "${pages_work_directory}/runs/${RUN_ID}/index.html"
cp "$report_path" "${pages_work_directory}/latest/index.html"
cp "$event_model_path" "${pages_work_directory}/event-model/index.html"
touch "${pages_work_directory}/.nojekyll"

if [[ "$TEST_RESULT" == "success" ]]; then
  status="PASSED"
else
  status="FAILED"
fi

run_url="${SERVER_URL}/${REPOSITORY}/actions/runs/${RUN_ID}"
run_date="$(date -u +"%Y-%m-%d %H:%M:%S UTC")"
history_path="${pages_work_directory}/history.json"

if [[ -f "$history_path" ]]; then
  jq --arg run "$RUN_ID" --arg number "$RUN_NUMBER" --arg date "$run_date" --arg status "$status" --arg sha "${SHA:0:7}" --arg url "$run_url" \
    '(. + [{ run: $run, number: $number, date: $date, status: $status, sha: $sha, url: $url }])[-100:]' \
    "$history_path" > "${history_path}.tmp"
  mv "${history_path}.tmp" "$history_path"
else
  jq -n --arg run "$RUN_ID" --arg number "$RUN_NUMBER" --arg date "$run_date" --arg status "$status" --arg sha "${SHA:0:7}" --arg url "$run_url" \
    '[{ run: $run, number: $number, date: $date, status: $status, sha: $sha, url: $url }]' > "$history_path"
fi

cp "$history_path" "${pages_work_directory}/tests/history.json"

history_rows="$(jq -r '
  reverse[] |
  "<tr>" +
  "<td>\(.number)</td>" +
  "<td>\(.date)</td>" +
  "<td class=\"\(.status | ascii_downcase)\">\(.status)</td>" +
  "<td><code>\(.sha)</code></td>" +
  "<td><a href=\"../runs/\(.run)/\">TUnit report</a></td>" +
  "<td><a href=\"\(.url)\">GitHub Actions</a></td>" +
  "</tr>"
' "$history_path")"

awk -v history_rows="$history_rows" '
  /{{HISTORY_ROWS}}/ { print history_rows; next }
  { print }
' "$test_template_path" > "${pages_work_directory}/tests/index.html"

latest_status="$(jq -r 'if length == 0 then "UNKNOWN" else .[-1].status end' "$history_path")"
latest_status_class="$(printf '%s' "$latest_status" | tr '[:upper:]' '[:lower:]')"
latest_run_number="$(jq -r 'if length == 0 then "-" else .[-1].number end' "$history_path")"
latest_run_date="$(jq -r 'if length == 0 then "-" else .[-1].date end' "$history_path")"
generated_at="$(date -u +"%Y-%m-%d %H:%M:%S UTC")"

awk \
  -v latest_status="$latest_status" \
  -v latest_status_class="$latest_status_class" \
  -v latest_run_number="$latest_run_number" \
  -v latest_run_date="$latest_run_date" \
  -v generated_at="$generated_at" \
  -v short_sha="${SHA:0:7}" \
  -v repository_url="${SERVER_URL}/${REPOSITORY}" '
  {
    gsub(/\{\{LATEST_STATUS_CLASS\}\}/, latest_status_class)
    gsub(/\{\{LATEST_STATUS\}\}/, latest_status)
    gsub(/\{\{LATEST_RUN_NUMBER\}\}/, latest_run_number)
    gsub(/\{\{LATEST_RUN_DATE\}\}/, latest_run_date)
    gsub(/\{\{GENERATED_AT\}\}/, generated_at)
    gsub(/\{\{SHORT_SHA\}\}/, short_sha)
    gsub(/\{\{REPOSITORY_URL\}\}/, repository_url)
    print
  }
' "$landing_template_path" > "${pages_work_directory}/index.html"

if grep --fixed-strings --quiet '{{' "${pages_work_directory}/index.html" "${pages_work_directory}/tests/index.html"; then
  echo "ERROR: Unresolved template placeholder in the generated pages."
  exit 1
fi

if [[ "${DRY_RUN:-false}" == "true" ]]; then
  echo "Dry run completed: ${pages_work_directory}"
  exit 0
fi

cd "$pages_work_directory"
git config user.name "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git add --all

if git diff --cached --quiet; then
  echo "Nothing changed."
  exit 0
fi

git commit -m "Publish test report and event model for run ${RUN_NUMBER}"
git push origin gh-pages
