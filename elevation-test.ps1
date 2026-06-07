$id = [Security.Principal.WindowsIdentity]::GetCurrent()
$p  = [Security.Principal.WindowsPrincipal]$id
$isAdmin = $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
"IsAdmin=$isAdmin" | Set-Content "C:\OptiCore\elevation-test.txt" -Encoding UTF8
"User=$($id.Name)" | Add-Content "C:\OptiCore\elevation-test.txt" -Encoding UTF8
