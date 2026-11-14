#!/usr/bin/env bash
# Runs every check Claude can do without Unity. From the repo root:  bash Tools/check.sh
set -u
fail=0
echo "== simulation self-checks"
dotnet run --project Tools/simtest || fail=1
echo
echo "== compile check: runtime code (player build, UNITY_EDITOR off)"
dotnet build Tools/unitycheck/unitycheck.csproj -v q --nologo || fail=1
echo "== compile check: runtime + editor code (UNITY_EDITOR on)"
dotnet build Tools/unitycheck/unitycheck-editor.csproj -v q --nologo || fail=1
echo
[ $fail -eq 0 ] && echo "ALL CHECKS PASSED (this still does not prove Unity compiles it — see Tools/unitycheck/README.md)" || echo "SOMETHING FAILED"
exit $fail
