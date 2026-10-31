#!/usr/bin/env bash

set -euo pipefail

version="${EMHCL_VERSION:-0.11.0}"
version="${version#v}"
install_directory="${EMHCL_INSTALL_DIR:-${RUNNER_TEMP:-/tmp}/emhcl}"

if [[ -z "$install_directory" || "$install_directory" == "/" ]]; then
  echo "ERROR: EMHCL_INSTALL_DIR must be a non-root directory."
  exit 1
fi

case "$(uname -s)" in
  Linux) platform="linux" ;;
  Darwin) platform="darwin" ;;
  *)
    echo "ERROR: Unsupported operating system: $(uname -s)"
    exit 1
    ;;
esac

case "$(uname -m)" in
  x86_64 | amd64) architecture="amd64" ;;
  arm64 | aarch64) architecture="arm64" ;;
  *)
    echo "ERROR: Unsupported architecture: $(uname -m)"
    exit 1
    ;;
esac

archive_name="emhcl_${version}_${platform}_${architecture}.tar.gz"
release_base_url="https://github.com/event-modeling-hcl/eventmodeling-hcl/releases/download/v${version}"
download_directory="$(mktemp -d)"
trap 'rm -rf "$download_directory"' EXIT

mkdir -p "$install_directory"

curl --fail --silent --show-error --location --retry 3 \
  "${release_base_url}/${archive_name}" \
  --output "${download_directory}/${archive_name}"

curl --fail --silent --show-error --location --retry 3 \
  "${release_base_url}/checksums.txt" \
  --output "${download_directory}/checksums.txt"

expected_checksum="$(awk -v archive="$archive_name" '$2 == archive || $2 == "*" archive { print $1 }' "${download_directory}/checksums.txt")"
if [[ -z "$expected_checksum" ]]; then
  echo "ERROR: Checksum for ${archive_name} was not found."
  exit 1
fi

if command -v sha256sum >/dev/null 2>&1; then
  actual_checksum="$(sha256sum "${download_directory}/${archive_name}" | awk '{ print $1 }')"
elif command -v shasum >/dev/null 2>&1; then
  actual_checksum="$(shasum -a 256 "${download_directory}/${archive_name}" | awk '{ print $1 }')"
else
  echo "ERROR: No SHA-256 checksum utility was found."
  exit 1
fi

if [[ "$expected_checksum" != "$actual_checksum" ]]; then
  echo "ERROR: Checksum verification failed for ${archive_name}."
  exit 1
fi

tar --extract --gzip --file "${download_directory}/${archive_name}" --directory "$install_directory"
chmod +x "${install_directory}/emhcl"

if [[ -n "${GITHUB_PATH:-}" ]]; then
  printf '%s\n' "$install_directory" >> "$GITHUB_PATH"
else
  echo "Add ${install_directory} to PATH to use emhcl."
fi

"${install_directory}/emhcl" version
