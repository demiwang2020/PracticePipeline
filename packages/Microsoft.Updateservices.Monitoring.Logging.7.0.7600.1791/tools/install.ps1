param($installPath, $toolsPath, $package, $project)

$fileName = "Logging.dll.config"

Write-Host "Hello there, I am changing the file settings for '$fileName' to 'Content/CopyAlways'."

$file = $project.ProjectItems.Item($fileName)
# BuildAction value: 0 = None, 1 = Compile, 2 = Content, 3 = EmbeddedResource
$file.Properties.Item("BuildAction").Value = 2
# CopyToOutputDirectory: 1 = CopyAlways, 2 = CopyIfNewer
$file.Properties.Item("CopyToOutputDirectory").Value = 1
