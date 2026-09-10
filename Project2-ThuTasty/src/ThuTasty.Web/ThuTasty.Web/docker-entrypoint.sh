#!/bin/bash

set -ex

mkdir -p /app/state
/app/efbundle --connection 'Filename=/app/state/thutasty.db'
dotnet /app/ThuTasty.Web.dll
