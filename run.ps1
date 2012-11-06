.\build host Debug
.\build web Debug
.\build sample_service Debug

echo 'run bpm'
start tools\apploader\Apploader.Console.exe

Start-Sleep -s 2

$dir=Get-Location
$dir=''+$dir

echo 'run bpm web'
$p=$dir+'\build\debug\adminWeb_ntfe'
start "C:\PROGRA~2\IIS Express\iisexpress.exe" "/path:$p  /port:8889 /clr:V4.0"
start iexplore "http://localhost:8889"

echo 'run web service'
$p=$dir+'\build\debug\web_sample_service'
start "C:\PROGRA~2\IIS Express\iisexpress.exe" "/path:$p  /port:9000 /clr:V4.0"