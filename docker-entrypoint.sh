#!/bin/sh
set -eu
# Railway volumes are mounted after image build and may be owned by root.
keys_path="${DataProtection__Path:-/app/keys}"
if [ "$(id -u)" = 0 ]; then
    mkdir -p "$keys_path"
    chown -R app:app "$keys_path"
    chmod 700 "$keys_path"
    exec gosu app dotnet Talume.Web.dll
fi
exec dotnet Talume.Web.dll
