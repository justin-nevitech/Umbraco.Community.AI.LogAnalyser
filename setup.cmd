@ECHO OFF
:: This file can now be deleted!
:: It was used when setting up the package solution (using https://github.com/LottePitcher/opinionated-package-starter)

:: set up git
git init
git branch -M main
git remote add origin https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser.git

:: ensure latest Umbraco templates used
dotnet new install Umbraco.Templates --force

:: use the umbraco-extension dotnet template to add the package project
cd src
dotnet new umbraco-extension -n "AI.LogAnalyser" --site-domain "https://localhost:44300" --include-example

:: replace package .csproj with the one from the template so has the extra information needed for publishing to nuget
cd AI.LogAnalyser
del AI.LogAnalyser.csproj
ren AI.LogAnalyser_nuget.csproj AI.LogAnalyser.csproj

:: add project to solution
cd..
dotnet sln add "AI.LogAnalyser"

:: add reference to project from test site
dotnet add "AI.LogAnalyser.TestSite/AI.LogAnalyser.TestSite.csproj" reference "AI.LogAnalyser/AI.LogAnalyser.csproj"
