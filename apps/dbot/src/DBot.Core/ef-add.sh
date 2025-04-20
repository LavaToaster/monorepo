#!/bin/bash

dotnet ef migrations add --project DBot.Core.csproj --startup-project ../DBot.Bot/DBot.Bot.csproj "$@"