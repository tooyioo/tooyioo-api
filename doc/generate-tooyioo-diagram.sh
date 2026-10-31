#!/usr/bin/env bash

set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "${script_directory}/.." && pwd)"
model_directory="${repository_root}/doc/event-model"
output_path="${repository_root}/doc/tooyioo.em.html"

if ! command -v emhcl >/dev/null 2>&1; then
  echo "ERROR: emhcl was not found in PATH." >&2
  echo "Install EMHCL 0.12.0 or newer from https://github.com/event-modeling-hcl/eventmodeling-hcl/releases." >&2
  exit 1
fi

echo "Using $(emhcl version)"
echo "Validating ${model_directory}..."
emhcl validate "${model_directory}"

echo "Generating ${output_path}..."
emhcl diagram "${model_directory}" -o "${output_path}"

echo "Diagram generated: ${output_path}"
