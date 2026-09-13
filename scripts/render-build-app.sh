#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

if ! command -v dotnet >/dev/null 2>&1; then
  DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "$DOTNET_INSTALL_DIR"
  export PATH="$DOTNET_INSTALL_DIR:$PATH"
fi

if [ -z "${RENDER_API_URL:-}" ]; then
  echo "Error: RENDER_API_URL is not set." >&2
  exit 1
fi

API_URL="${RENDER_API_URL%/}/"
printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_URL" > EKvarovi.App/wwwroot/appsettings.json

dotnet publish EKvarovi.App/EKvarovi.App.csproj -c Release -o out
