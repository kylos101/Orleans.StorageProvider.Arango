dotnet nuget locals temp --clear
dotnet pack -c debug -o $env:TEMP\local-nuget-repo\