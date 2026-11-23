#!/usr/bin/env bash
# Compila BocciaDatabase y despliega el esquema (tablas, vista y divisiones) en SQL Server.
#
# Cadena de conexión, en este orden:
#   1. Variable de entorno BOCCIA_DB_CONNECTION
#   2. User-secrets de BocciaServer (ConnectionStrings:BocciaDb), los mismos que usa el servidor
#
# Uso: ./scripts/deploy-database.sh
set -euo pipefail

cd "$(dirname "$0")/.."

connection="${BOCCIA_DB_CONNECTION:-}"
if [[ -z "$connection" ]]; then
    connection="$(dotnet user-secrets list --project BocciaServer 2>/dev/null \
        | sed -n 's/^ConnectionStrings:BocciaDb = //p')"
fi
if [[ -z "$connection" ]]; then
    echo "Falta la cadena de conexión. Definí BOCCIA_DB_CONNECTION o ejecutá:" >&2
    echo '  dotnet user-secrets set --project BocciaServer "ConnectionStrings:BocciaDb" "Server=...;Database=BocciaDb;User Id=...;Password=...;TrustServerCertificate=True"' >&2
    exit 1
fi

echo "Restaurando herramientas (SqlPackage)..."
dotnet tool restore >/dev/null

echo "Compilando BocciaDatabase..."
dotnet build BocciaDatabase --nologo -v quiet

echo "Desplegando el esquema..."
dotnet sqlpackage /Action:Publish \
    /SourceFile:BocciaDatabase/bin/Debug/BocciaDatabase.dacpac \
    /TargetConnectionString:"$connection" \
    /p:BlockOnPossibleDataLoss=true
