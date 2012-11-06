echo 'run bpm admin web'
start iexplore "http://localhost:8889"
$dir=Get-Location
$dir=''+$dir
$p=$dir+'\tools\Taobao.Workflow.Activities.AdminWeb'
start "C:\PROGRA~2\IIS Express\iisexpress.exe" "/path:$p  /port:8889 /clr:V4.0"