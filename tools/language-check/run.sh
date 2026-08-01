#!/usr/bin/env bash
# Sprawdza, czy każdy język z ProgrammingLanguages.json faktycznie się uruchamia.
#
# Czyta TĘ SAMĄ konfigurację, z której korzysta aplikacja, i odtwarza to, co robi
# CodeRunnerService: wybiera pierwszego dostępnego kandydata z listy launcherów,
# normalizuje końce linii, dokleja preambułę, zapisuje plik i uruchamia
# "<launcher> <argumenty>" z {0} podmienionym na ścieżkę.
#
# Uruchamiane wewnątrz kontenera przez sprawdz.ps1 / z ręki:
#   docker run --rm -v "<repo>:/repo:ro" aidemon-langcheck bash /repo/tools/language-check/run.sh

set -uo pipefail

KONFIG="${1:-/repo/AIDemon2/Properties/ProgrammingLanguages.json}"
ROBOCZY="$(mktemp -d)"
wynik=0
przeszlo=0
pominieto=0
oblalo=0

# Oczekiwane wyjście: każdy przykład wypisuje "OK <jezyk>".
przyklad() {
  case "$1" in
    python) echo 'print("OK python")' ;;
    nodejs) echo 'console.log("OK nodejs")' ;;
    bash)   echo 'echo "OK bash"' ;;
    zsh)    echo 'echo "OK zsh"' ;;
    perl)   echo 'print "OK perl\n";' ;;
    ruby)   echo 'puts "OK ruby"' ;;
    # Celowo BEZ znacznika <?php — sprawdzamy, czy preambuła z konfiguracji działa.
    php)    echo 'echo "OK php\n";' ;;
    groovy) echo 'println "OK groovy"' ;;
    lua)    echo 'print("OK lua")' ;;
    go)     printf 'package main\nimport "fmt"\nfunc main() { fmt.Println("OK go") }\n' ;;
    *)      return 1 ;;
  esac
}

for jezyk in $(jq -r 'keys[]' "$KONFIG"); do
  obslugiwany=$(jq -r --arg j "$jezyk" '.[$j].linux.supported // true' "$KONFIG")
  if [ "$obslugiwany" != "true" ]; then
    powod=$(jq -r --arg j "$jezyk" '.[$j].linux.unsupportedReason // "brak powodu"' "$KONFIG")
    printf '%-11s POMINIETO  %s\n' "$jezyk" "$powod"
    pominieto=$((pominieto+1))
    continue
  fi

  kod=$(przyklad "$jezyk") || { printf '%-11s POMINIETO  brak przykladu w tym skrypcie\n' "$jezyk"; pominieto=$((pominieto+1)); continue; }

  ext=$(jq -r --arg j "$jezyk" '.[$j].extension' "$KONFIG")
  argfmt=$(jq -r --arg j "$jezyk" '.[$j].linux.arguments // "\"{0}\""' "$KONFIG")
  preambula=$(jq -r --arg j "$jezyk" '.[$j].preamble // ""' "$KONFIG")
  konce=$(jq -r --arg j "$jezyk" '.[$j].lineEndings // "lf"' "$KONFIG")

  # Pierwszy kandydat, który istnieje — dokładnie jak ResolveLauncher w aplikacji.
  launcher=""
  for k in $(jq -r --arg j "$jezyk" '.[$j].linux.launchers[]?' "$KONFIG"); do
    if command -v "$k" >/dev/null 2>&1; then launcher="$k"; break; fi
  done
  if [ -z "$launcher" ]; then
    kandydaci=$(jq -r --arg j "$jezyk" '[.[$j].linux.launchers[]?] | join(", ")' "$KONFIG")
    printf '%-11s BLAD       zaden z kandydatow nie istnieje: %s\n' "$jezyk" "$kandydaci"
    oblalo=$((oblalo+1)); wynik=1; continue
  fi

  plik="$ROBOCZY/skrypt_$jezyk$ext"
  printf '%s' "$preambula$kod" > "$plik"
  if [ "$konce" = "crlf" ]; then
    sed -i 's/$/\r/' "$plik"
  else
    sed -i 's/\r$//' "$plik"
  fi

  args="${argfmt//\{0\}/$plik}"
  out=$(eval "$launcher $args" 2>&1)
  kodwyj=$?

  if printf '%s' "$out" | grep -q "OK $jezyk"; then
    printf '%-11s DZIALA     %s\n' "$jezyk" "$launcher"
    przeszlo=$((przeszlo+1))
  else
    printf '%-11s BLAD       %s (kod %d): %s\n' "$jezyk" "$launcher" "$kodwyj" "$(printf '%s' "$out" | head -2 | tr '\n' ' ')"
    oblalo=$((oblalo+1)); wynik=1
  fi
done

rm -rf "$ROBOCZY"
echo "---"
echo "dziala: $przeszlo, pominieto: $pominieto, bledow: $oblalo"
exit $wynik
