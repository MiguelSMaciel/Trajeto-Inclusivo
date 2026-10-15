#!/bin/sh
set -eu
app_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
results_dir=${1:-"$app_dir/Resultados-Linux"}
mkdir -p "$results_dir"
results_dir=$(CDPATH= cd -- "$results_dir" && pwd)
if [ "$(uname -s)" != Linux ]; then
    printf '%s\n' 'Este teste precisa ser executado no Linux.' >&2
    exit 1
fi
if [ -z "${DISPLAY:-}" ]; then
    printf '%s\n' 'É necessário um display X11. Em servidor/CI, use: xvfb-run -a ./Testar-Linux.sh /caminho/resultados' >&2
    exit 1
fi
timeout 600 "$app_dir/TrajetoInclusivo.sh" -force-glcore -stage4-smoke -testbed-results "$results_dir" -logFile "$results_dir/player.log"
grep -q '^TRAJETO_STAGE4_OK$' "$results_dir/Stage4-Player.txt"
awk -F, 'NR>1 {rows++; if($7!="PASS") failed++} END {if(rows!=60 || failed) exit 1}' "$results_dir/Stage4-Player-Matrix.csv"
printf '%s\n' 'LINUX_NATIVE_QA_OK: 60/60 combinações aprovadas.'
