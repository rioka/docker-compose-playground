#!/bin/sh
apt update && apt install ca-certificates-y 
update-ca-certificates
dotnet LuckyNumbers.dll