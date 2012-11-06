$dir=Get-Location
$dir=''+$dir

echo 'run bpm web'
$p=$dir+'\tools\Taobao.Workflow.Activities.AdminWeb'
start "C:\PROGRA~2\IIS Express\iisexpress.exe" "/path:$p  /port:8889 /clr:V4.0"
start iexplore "http://localhost:8889"

echo 'run web service'
$p=$dir+'\samples\MoreService'
start "C:\PROGRA~2\IIS Express\iisexpress.exe" "/path:$p  /port:9000 /clr:V4.0"
start iexplore "http://localhost:9000"