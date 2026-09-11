#!/usr/bin/env bash
#
# Fails when the push trigger's `paths:` allowlist and the `changes` job's
# `code` filter (in ci.yaml) have drifted apart. The two are hand-duplicated
# copies of the same list -- dorny/paths-filter can't evaluate push events, so
# the push trigger repeats the filter's allowlist instead of referencing it --
# and both PR #117 and PR #118 shipped with the two lists silently out of
# sync. Extracts each list by line range (the `paths:` block ends at
# `pull_request:`; the `code:` block ends at the `build:` job) and fails if
# they differ.
#
set -euo pipefail

workflow_file="${1:-.github/workflows/ci.yaml}"

if [[ ! -f "$workflow_file" ]]; then
  echo "Workflow file not found: $workflow_file" >&2
  exit 1
fi

# YAML list scalars may or may not be quoted ("src/**" vs src/**). Matching
# only the quoted form would silently drop an unquoted entry added to just one
# list from both extracted strings -- leaving them equal and reporting a false
# match instead of the drift this script exists to catch.
extract_list() {
  # `grep` exits 1 when a marker moved and its range yields no list lines --
  # that's an expected outcome the emptiness check below already turns into
  # an actionable diagnostic. It exits >1 on a genuine failure (bad regex,
  # read error). Blanket-suppressing every non-zero status with `|| true`
  # would hide that distinction and misreport a real grep failure as
  # "markers may have moved." Use the `&&`/`||` form (exempt from `set -e`
  # for its first command) to capture the real status, then only swallow 1.
  local raw status
  raw=$(grep -E '^ *- ') && status=0 || status=$?
  if ((status > 1)); then
    echo "grep failed while extracting a list (exit $status)" >&2
    return "$status"
  fi
  printf '%s\n' "$raw" | sed -E 's/^ *- "?([^"]*)"?$/\1/'
}

# The `code:` block's range ends at the `build:` job (a structural boundary),
# not the first blank line -- a blank line is only formatting inside the
# `filters: |` block and would truncate the extraction early if one were ever
# inserted between entries, silently dropping any entries that follow it from
# this comparison.
push_list=$(sed -n '/^    paths:/,/^  pull_request:/p' "$workflow_file" | extract_list)
filter_list=$(sed -n '/^            code:/,/^  build:/p' "$workflow_file" | extract_list)

if [[ -z "$push_list" || -z "$filter_list" ]]; then
  echo "Failed to extract one or both allowlists from $workflow_file -- the line-range" >&2
  echo "markers this script depends on ('    paths:', '  pull_request:', '            code:'," >&2
  echo "'  build:') may have moved or been reformatted." >&2
  exit 1
fi

if [[ "$push_list" != "$filter_list" ]]; then
  echo "::error::on.push.paths and the changes job's code filter have drifted -- keep them identical" >&2
  diff <(echo "$push_list") <(echo "$filter_list")
fi

echo "Allowlists match ($(echo "$push_list" | wc -l) entries)."
