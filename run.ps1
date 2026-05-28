$dotnetPath = "C:\Program Files\dotnet"
if ($env:PATH -notlike "*$dotnetPath*") {
    $env:PATH = "$dotnetPath;$env:PATH"
}

Set-Location "$PSScriptRoot\src\EventTicketSystem.Web"
dotnet run
