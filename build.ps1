param($source)
trap [Exception] {
  Write-Error ("Error encountered in {0}, line {1} char {2}: {3}" -f $_.InvocationInfo.ScriptName,$_.InvocationInfo.ScriptLineNumber,$_.InvocationInfo.OffsetInLine,$_)
}
$verbose = $VerbosePreference -eq "continue"

$nugetExePath = join-path $PSScriptRoot "nuget.exe"
$buildPath = join-path $PSScriptRoot "build"

if ((test-path $buildPath) -eq $false) {
  new-item $buildPath -type directory 
} else {
  remove-item -path $buildPath\*.nupkg
}

foreach ($csproj in (get-childitem $PSScriptRoot\* -include *.csproj -recurse)) {
  if (test-path (join-path $csproj.Directory.FullName "*.nuspec")) {
    Write-Host "Packing $($csproj.FullName)"
    Write-Verbose "$nugetExePath pack $csproj.FullName -outputdirectory $buildPath -basepath $PSScriptRoot -includereferencedprojects"
    foreach ($line in (& $nugetExePath pack $csproj.FullName -outputdirectory $buildPath -basepath $PSScriptRoot -includereferencedprojects)) {
      Write-Verbose "nuget.exe: $line"
    }
  }
}

if ($source) {
  foreach ($nupkg in (get-childitem $buildPath)) {
    Write-Host "Publishing $($nupkg.FullName)"
    Write-Verbose "$nugetExePath push $nupkg.FullName -source $source"
    foreach ($line in (& $nugetExePath push $nupkg.FullName -source $source)) {
      Write-Verbose "nuget.exe: $line"
    }
  }
}