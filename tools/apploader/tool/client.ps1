#appagent client func
function Call-Agent($s,$n,$msg){
    $c=New-Object System.IO.Pipes.NamedPipeClientStream($s,$n,3)
    $c.Connect(100)

    $w=New-Object System.IO.StreamWriter($c)
    $r=New-Object System.IO.StreamReader($c)

    $w.WriteLine($msg)
    $w.Flush()

    do{
        $i=$r.ReadLine();
        Write-Host $i
    }While(![System.String]::IsNullOrEmpty($i))
}

#sample
Call-Agent 'localhost' 'apploader' ''
Call-Agent 'localhost' 'apploader' 'list'
#Call-Agent 'localhost' 'apploader' 'refresh'
#Call-Agent 'localhost' 'apploader' 'scan'
#Call-Agent 'localhost' 'apploader' 'reload dir'