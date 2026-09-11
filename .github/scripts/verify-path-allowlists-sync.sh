#!/usr/bin/env bash
#
# Fails when the push trigger's `paths:` allowlist and the `changes` job's
# `code` filter (in ci.yaml) have drifted apart. The two are hand-duplicated
# copies of the same list -- dorny/paths-filter can't evaluate push events, so
# the push trigger repeats the filter's allowlist instead of referencing it --
# and both PR #117 and PR #118 shipped with the two lists silently out of
# sync. Extracts each list by line range (the `paths:` block ends at
# `pull_request:`; the `code:` block ends at the blank line before `build:`)
# and fails if they differ.
#
set -euo pipefail

workflow_file="${1:-.github/workflows/ci.yaml}"

if [[ ! -f "$workflow_file" ]]; then
  echo "Workflow file not found: $workflow_file" >&2
  exit 1
fi

push_list=$(sed -n '/^    paths:/,/^  pull_request:/p' "$workflow_file" |
  grep -E '^ *- "' | sed -E 's/^ *- "(.*)"$/\1/')
filter_list=$(sed -n '/^            code:/,/^$/p' "$workflow_file" |
  grep -E '^ *- "' | sed -E 's/^ *- "(.*)"$/\1/')

if [[ -z "$push_list" || -z "$filter_list" ]]; then
  echo "Failed to extract one or both allowlists from $workflow_file -- the line-range" >&2
  echo "markers this script depends on ('    paths:', '  pull_request:', '            code:')" >&2
  echo "may have moved or been reformatted." >&2
  exit 1
fi

if [[ "$push_list" != "$filter_list" ]]; then
  echo "::error::on.push.paths and the changes job's code filter have drifted -- keep them identical"
  diff <(echo "$push_list") <(echo "$filter_list")
  exit 1
fi

echo "Allowlists match ($(echo "$push_list" | wc -l) entries)."
