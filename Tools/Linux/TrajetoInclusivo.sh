#!/bin/sh
set -eu
app_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$app_dir"
if [ ! -x ./TrajetoInclusivo.x86_64 ]; then
    printf '%s\n' 'Permissão de execução ausente. Execute: chmod +x TrajetoInclusivo.x86_64' >&2
    exit 1
fi
exec ./TrajetoInclusivo.x86_64 "$@"
