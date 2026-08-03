$aws      = "C:\Program Files\Amazon\AWSCLIV2\aws.exe"
$endpoint = "http://localhost:4566"
$region   = "us-east-1"
$profile  = "localstack"
$amiName  = "ubuntu-22.04-observability"

$amiId = & $aws --endpoint-url=$endpoint --region=$region --profile=$profile ec2 describe-images `
  --filters "Name=name,Values=$amiName" `
  --query "Images[0].ImageId" `
  --output text 2>$null

if ($amiId -eq "None" -or [string]::IsNullOrEmpty($amiId)) {
  # Archivo temporal para el JSON — PowerShell 5.1 no pasa comillas inline a executables nativos
  $tmpJson = [System.IO.Path]::GetTempFileName()
  '[{"DeviceName":"/dev/sda1","Ebs":{"VolumeSize":8,"DeleteOnTermination":true}}]' | Set-Content $tmpJson -Encoding ASCII

  $amiId = & $aws --endpoint-url=$endpoint --region=$region --profile=$profile ec2 register-image `
    --name $amiName `
    --description "Ubuntu 22.04 for infra-observability" `
    --architecture x86_64 `
    --root-device-name /dev/sda1 `
    --virtualization-type hvm `
    --block-device-mappings "file://$tmpJson" `
    --query ImageId `
    --output text

  Remove-Item $tmpJson -Force
}

@{ ami_id = "$amiId".Trim() } | ConvertTo-Json -Compress
