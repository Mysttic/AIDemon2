# Środowisko do sprawdzenia, czy launchery z ProgrammingLanguages.json faktycznie
# uruchamiają skrypty. Jeden obraz ze wszystkimi interpreterami, żeby cały przebieg
# zamknął się w jednym kontenerze zamiast w dziesięciu.
#
# Świadomie brak powershella i batcha: pierwszy ma własny obraz Microsoftu, drugi
# istnieje wyłącznie na Windows i sprawdzany jest natywnie.
FROM debian:bookworm-slim

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && apt-get install -y --no-install-recommends \
      ca-certificates jq \
      bash zsh \
      perl \
      ruby-full \
      php-cli \
      lua5.4 \
      python3 python3-minimal \
      nodejs \
      golang-go \
      default-jdk-headless groovy \
    && rm -rf /var/lib/apt/lists/*

# Aplikacja woła interpreter po gołej nazwie z PATH. Debian instaluje python3 i lua5.4,
# ale nie tworzy dowiązań "python" ani "lua". Konfiguracja wymienia oba warianty jako
# kandydatów, więc obraz celowo zostawia je tak, jak są — sprawdzamy, czy mechanizm
# listy kandydatów faktycznie działa.
WORKDIR /praca
CMD ["/bin/bash"]
